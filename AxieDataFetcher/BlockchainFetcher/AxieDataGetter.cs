﻿using System;
using System.Collections.Generic;
using Nethereum.Web3;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RPC.Eth.DTOs;
using System.Threading.Tasks;
using System.Numerics;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Newtonsoft.Json.Linq;
using AxieDataFetcher.AxieObjects;
using Nethereum.Util;
using System.Linq;
using AxieDataFetcher.Core;
using AxieDataFetcher.Mongo;
namespace AxieDataFetcher.BlockchainFetcher
{
    public class AxieDataGetter
    {

        #region ABI & contract declaration
        private static string AxieCoreContractAddress = "0xF4985070Ce32b6B1994329DF787D1aCc9a2dd9e2";
        private static string NftAddress = "0xf5b0a3efb8e8e4c201e2a935f110eaaf3ffecb8d";
        private static string AxieLabContractAddress = "0x99ff9f4257D5b6aF1400C994174EbB56336BB79F";
        private static string AxieExtraDataContract = "0x10e304a53351b272dc415ad049ad06565ebdfe34";
        #endregion
        private static BigInteger lastBlockChecked = 6421665;

        //public static Queue<Task<IUserMessage>> messageQueue = new Queue<Task<IUserMessage>>();

        public static bool IsServiceOn = true;
        public AxieDataGetter()
        {
        }

        public static async Task<AxieExtraData> GetExtraData(int axieId)
        {
            var web3 = new Web3("https://mainnet.infura.io");
            var ownerDataContract = web3.Eth.GetContract(KeyGetter.GetABI("onwerDataABI"), AxieExtraDataContract);
            var getExtraFunction = ownerDataContract.GetFunction("getExtra");
            try
            {
                var result = await getExtraFunction.CallDeserializingToObjectAsync<AxieExtraData>(new BigInteger(axieId));
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.ReadLine();
            }
            return null;
        }

        public static async Task<AxieExtraData> test(int axieId)
        {
            var web3 = new Web3("https://mainnet.infura.io");
            var auctionContract = web3.Eth.GetContract(KeyGetter.GetABI("auctionABI"), AxieCoreContractAddress);
            var getSellerInfoFunction = auctionContract.GetFunction("getAuction");
            try
            {
                var lastBlock = await GetLastBlockCheckpoint(web3);
                var firstBlock = GetInitialBlockCheckpoint(lastBlock.BlockNumber);
                object[] input = new object[2];
                input[0] = NftAddress;
                input[1] = new BigInteger(axieId);
                var result = await getSellerInfoFunction.CallDeserializingToObjectAsync<SellerInfo>(firstBlock, input);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.ReadLine();
            }
            return null;
        }

        public static async Task GetData()
        {
            IsServiceOn = true;
            //_ = TaskHandler.UpdateServiceCheckLoop();
            var web3 = new Web3("https://mainnet.infura.io");
            //get contracts
            var auctionContract = web3.Eth.GetContract(KeyGetter.GetABI("auctionABI"), AxieCoreContractAddress);
            var getSellerInfoFunction = auctionContract.GetFunction("getAuction");
            var labContract = web3.Eth.GetContract(KeyGetter.GetABI("labABI"), AxieLabContractAddress);
            //get events
            var auctionSuccesfulEvent = auctionContract.GetEvent("AuctionSuccessful");
            var auctionCreatedEvent = auctionContract.GetEvent("AuctionCreated");
            var axieBoughtEvent = labContract.GetEvent("AxieBought");
            var auctionCancelled = auctionContract.GetEvent("AuctionCancelled");

            //set block range search
            var lastBlock = await GetLastBlockCheckpoint(web3);
            var firstBlock = GetInitialBlockCheckpoint(lastBlock.BlockNumber);
            while (IsServiceOn)
            {
                try
                {
                    //prepare filters
                    var auctionFilterAll = auctionSuccesfulEvent.CreateFilterInput(firstBlock, lastBlock);
                    var auctionCancelledFilterAll = auctionCancelled.CreateFilterInput(firstBlock, lastBlock);
                    var auctionCreationFilterAll = auctionCreatedEvent.CreateFilterInput(firstBlock, lastBlock);
                    var labFilterAll = axieBoughtEvent.CreateFilterInput(firstBlock, lastBlock);

                    //get logs from blockchain
                    var auctionLogs = await auctionSuccesfulEvent.GetAllChanges<AuctionSuccessfulEvent>(auctionFilterAll);
                    var auctionCancelledLogs = await auctionSuccesfulEvent.GetAllChanges<AuctionCancelledEvent>(auctionFilterAll);
                    var labLogs = await axieBoughtEvent.GetAllChanges<AxieBoughtEvent>(labFilterAll);
                    var auctionCreationLogs = await auctionCreatedEvent.GetAllChanges<AuctionCreatedEvent>(auctionCreationFilterAll);

                    BigInteger latestLogBlock = 0;
                    //read logs
                    //if (auctionCancelledLogs != null && auctionCancelledLogs.Count > 0) _ = HandleAuctionCancelTriggers(auctionCancelledLogs);

                    if (auctionCreationLogs != null && auctionCreationLogs.Count > 0)
                    {

                        foreach (var log in auctionCreationLogs)
                        {
                            var axie = await AxieObjectV1.GetAxieFromApi(Convert.ToInt32(log.Event.tokenId.ToString()));
                            var price = log.Event.startingPrice;
                            await axie.GetTrueAuctionData();
                        }
                    }

                    if (auctionLogs != null && auctionLogs.Count > 0)
                    {
                        foreach (var log in auctionLogs)
                        {
                            latestLogBlock = log.Log.BlockNumber.Value;
                            int axieId = Convert.ToInt32(log.Event.tokenId.ToString());
                            float priceinEth = Convert.ToSingle(Nethereum.Util.UnitConversion.Convert.FromWei(log.Event.totalPrice).ToString());
                            object[] input = new object[2];
                            input[0] = NftAddress;
                            input[1] = log.Event.tokenId;
                            var sellerInfo = await getSellerInfoFunction.CallDeserializingToObjectAsync<SellerInfo>(
                                new BlockParameter(new HexBigInteger(log.Log.BlockNumber.Value - 1)), input);

                        };
                        Console.WriteLine("End of batch");
                    }
                    if (labLogs != null && labLogs.Count > 0)
                    {
                        foreach (var log in labLogs)
                        {
                            latestLogBlock = log.Log.BlockNumber.Value;
                            float priceinEth = Convert.ToSingle(Nethereum.Util.UnitConversion.Convert.FromWei(log.Event.price).ToString());
                            int amount = log.Event.amount;
                            
                        };
                        Console.WriteLine("End of batch");
                    }
                    await Task.Delay(60000);
                    if (latestLogBlock > lastBlock.BlockNumber.Value) firstBlock = new BlockParameter(new HexBigInteger(latestLogBlock + 1));
                    else firstBlock = new BlockParameter(new HexBigInteger(lastBlock.BlockNumber.Value + 1));
                    lastBlock = await GetLastBlockCheckpoint(web3);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    //Logger.Log(ex.ToString());
                    IsServiceOn = false;
                    break;
                }

            }
        }

        public static async Task FetchLogsFromRange()
        {
            var web3 = new Web3("https://mainnet.infura.io");
            //get contracts
            var auctionContract = web3.Eth.GetContract(KeyGetter.GetABI("auctionABI"), AxieCoreContractAddress);
            var getSellerInfoFunction = auctionContract.GetFunction("getAuction");
            var labContract = web3.Eth.GetContract(KeyGetter.GetABI("labABI"), AxieLabContractAddress);
            //get events
            var auctionSuccesfulEvent = auctionContract.GetEvent("AuctionSuccessful");
            var auctionCreatedEvent = auctionContract.GetEvent("AuctionCreated");
            var axieBoughtEvent = labContract.GetEvent("AxieBought");
            var auctionCancelled = auctionContract.GetEvent("AuctionCancelled");

            //set block range search
            var lastBlock = await GetLastBlockCheckpoint(web3);
            var firstBlock = GetInitialBlockCheckpoint(lastBlock.BlockNumber);

            //prepare filters
            var auctionFilterAll = auctionSuccesfulEvent.CreateFilterInput(firstBlock, lastBlock);
            var auctionCancelledFilterAll = auctionCancelled.CreateFilterInput(firstBlock, lastBlock);
            var auctionCreationFilterAll = auctionCreatedEvent.CreateFilterInput(firstBlock, lastBlock);
            var labFilterAll = axieBoughtEvent.CreateFilterInput(new BlockParameter(new HexBigInteger(lastBlockChecked)), await GetLastBlockCheckpoint(web3));

            //get logs from blockchain
            //var auctionLogs = await auctionSuccesfulEvent.GetAllChanges<AuctionSuccessfulEvent>(auctionFilterAll);
            //var auctionCancelledLogs = await auctionSuccesfulEvent.GetAllChanges<AuctionCancelledEvent>(auctionFilterAll);
            var labLogs = await axieBoughtEvent.GetAllChanges<AxieBoughtEvent>(labFilterAll);
            //var auctionCreationLogs = await auctionCreatedEvent.GetAllChanges<AuctionCreatedEvent>(auctionCreationFilterAll);

            int time = 0;
            int eggCount = 0;

            int perc = 0;
            int count = 0;
            int div = labLogs.Count / 100;
            foreach (var log in labLogs)
            {
                count++;
                if (count > div)
                {
                    perc++;
                    Console.Write($"{perc}%");
                    count = 0;
                }
                var blockParam = new BlockParameter(log.Log.BlockNumber);
                var block = await web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(blockParam);
                var blockTime = Convert.ToInt32(block.Timestamp.Value.ToString());
                if (time == 0) time = blockTime;
                if (blockTime - time > 86400)
                {
                    var collec = DatabaseConnection.GetDb().GetCollection<EggCount>("EggSoldPerDay");
                    await collec.InsertOneAsync(new EggCount(time, eggCount));
                    eggCount = 0;
                    time = blockTime;
                }
                eggCount += log.Event.amount;
            }
        }

        private static async Task<BlockParameter> GetLastBlockCheckpoint(Web3 web3)
        {
            var lastBlock = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            var blockNumber = lastBlock.Value - 12;
            return new BlockParameter(new HexBigInteger(blockNumber));
        }

        private static BlockParameter GetInitialBlockCheckpoint(HexBigInteger blockNumber)
        {
            var firstBlock = blockNumber.Value - 10;
            return new BlockParameter(new HexBigInteger(firstBlock));
        }

        static public async Task ScanBlocksExample(Web3 web3, ulong startBlockNumber, ulong endBlockNumber)
        {
            Console.WriteLine("ScanBlocksExample:");

            long txTotalCount = 0;
            for (ulong blockNumber = startBlockNumber; blockNumber <= endBlockNumber; blockNumber++)
            {
                var blockParameter = new Nethereum.RPC.Eth.DTOs.BlockParameter(blockNumber);
                var block = await web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(blockParameter);
                var trans = block.Transactions;
                int txCount = trans.Length;
                txTotalCount += txCount;
                if (blockNumber % 1000 == 0) Console.Write(".");

            }
            Console.WriteLine();
        }
    }



    public class AuctionSuccessfulEvent
    {
        [Parameter("address", "_nftAddress", 1, true)]
        public string nftAddress { get; set; }

        [Parameter("uint256", "_tokenId", 2, true)]
        public BigInteger tokenId { get; set; }

        [Parameter("uint256", "_totalPrice", 3)]
        public BigInteger totalPrice { get; set; }

        [Parameter("address", "_winner", 4)]
        public string winner { get; set; }
    }

    public class AxieBoughtEvent
    {
        [Parameter("address", "_buyer", 1, true)]
        public string buyer { get; set; }

        [Parameter("address", "_referrer", 2, true)]
        public string referrer { get; set; }

        [Parameter("int8", "_amount", 3)]
        public int amount { get; set; }

        [Parameter("uint256", "_price", 4)]
        public BigInteger price { get; set; }

        [Parameter("uint256", "_referralReward", 4)]
        public BigInteger referralReward { get; set; }
    }


    public class AuctionCreatedEvent
    {
        [Parameter("address", "_nftAddress", 1, true)]
        public string nftAddress { get; set; }

        [Parameter("uint256", "_tokenId", 2, true)]
        public BigInteger tokenId { get; set; }

        [Parameter("uint256", "_startingPrice", 3)]
        public BigInteger startingPrice { get; set; }

        [Parameter("uint256", "_endingPrice", 4)]
        public BigInteger endingPrice { get; set; }

        [Parameter("uint256", "_duration", 5)]
        public BigInteger duration { get; set; }

        [Parameter("address", "_seller", 6)]
        public string seller { get; set; }

    }

    public class AuctionCancelledEvent
    {
        [Parameter("address", "_nftAddress", 1, true)]
        public string nftAddress { get; set; }

        [Parameter("uint256", "_tokenId", 2, true)]
        public BigInteger tokenId { get; set; }
    }

    [FunctionOutput]
    public class AxieExtraData
    {
        [Parameter("uint256", "_sireId", 1)]
        public BigInteger sireId { get; set; }

        [Parameter("uint256", "_matronId", 2)]
        public BigInteger matronId { get; set; }

        [Parameter("uint256", "_exp", 3)]
        public BigInteger exp { get; set; }

        [Parameter("uint256", "_numBreeding", 4)]
        public BigInteger numBreeding { get; set; }
    }

    [FunctionOutput]
    public class SellerInfo
    {
        [Parameter("address", "seller", 1)]
        public string seller { get; set; }

        [Parameter("uint256", "startingPrice", 2)]
        public BigInteger startingPrice { get; set; }

        [Parameter("uint256", "endingPrice", 3)]
        public BigInteger endingPrice { get; set; }

        [Parameter("uint256", "duration", 4)]
        public BigInteger duration { get; set; }

        [Parameter("uint256", "startedAt", 5)]
        public BigInteger startedAt { get; set; }
    }

    [Function("getExtra")]
    public class AxieExtraFunction
    {
        [Parameter("uint256", "_axieId", 1)]
        public BigInteger axieId { get; set; }

    }

}