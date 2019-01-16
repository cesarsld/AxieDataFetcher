using System;
using System.Net.Http;
using System.Net;
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
using Nethereum.Web3.Accounts;
using Nethereum.Contracts.CQS;
using Newtonsoft.Json;
//using Nethereum.Contracts.Extensions;
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
        private static BigInteger lastBlockChecked = 5318592;


        public static bool IsServiceOn = true;

        public static async Task<HexBigInteger> GetSafeLowGas()
        {
            var json = "";
            using (System.Net.WebClient wc = new System.Net.WebClient())
            {
                json = await wc.DownloadStringTaskAsync("https://api.axieinfinity.com/v1/gas-price");
            }
            return new HexBigInteger(BigInteger.Parse((string)JObject.Parse(json)["safeLow"]));
        }

        public static async Task<HexBigInteger> GetStandardGas()
        {
            var json = "";
            using (System.Net.WebClient wc = new System.Net.WebClient())
            {
                json = await wc.DownloadStringTaskAsync("https://api.axieinfinity.com/v1/gas-price");
            }
            return new HexBigInteger(BigInteger.Parse((string)JObject.Parse(json)["standard"]));
        }

        public static async Task BuyAxie(HexBigInteger axieId, HexBigInteger price)
        {
            var myPassword = "";
            var account = new Account(myPassword);
            var web3 = new Web3(account, "https://mainnet.infura.io");
            var gasPrice = await web3.Eth.GasPrice.SendRequestAsync();
            object[] input = new object[2];
            input[0] = NftAddress;
            input[1] = new BigInteger(18095);

            var safeLow = await GetSafeLowGas();
            //get contract
            var auctionContract = web3.Eth.GetContract(KeyGetter.GetABI("auctionABI"), AxieCoreContractAddress);

            //get payable function
            var bidFunction = auctionContract.GetFunction("bid");
            var estimateGas = await bidFunction.EstimateGasAsync(account.Address, new HexBigInteger(8000000), price, NftAddress, axieId);
            Console.WriteLine($"Bid ID {input[1]} using {estimateGas.Value} gas");
            try
            {
                var tx = await bidFunction.SendTransactionAsync(account.Address, estimateGas, safeLow, price, input[0], input[1]);
                Console.WriteLine($"TX : {tx}");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }
        }

        public static async Task CheckIfShouldBuy(EventLog<AuctionCreatedEvent> auction)
        {
            //1 eth is 1 000 000 000 000 000 000 wie
            var currentPrice = auction.Event.startingPrice;

            if (currentPrice < BigInteger.Parse("1 000 000 000 000 000 000"))
            { }
        }

        public static async Task FetchAuctionData()
        {
            var web3 = new Web3("https://mainnet.infura.io");
            //get contracts
            var auctionContract = web3.Eth.GetContract(KeyGetter.GetABI("auctionABI"), AxieCoreContractAddress);

            //get events
            var auctionSuccesfulEvent = auctionContract.GetEvent("AuctionSuccessful");
            var auctionCreatedEvent = auctionContract.GetEvent("AuctionCreated");
            var auctionCancelled = auctionContract.GetEvent("AuctionCancelled");

            //set block range
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

                    //get logs from blockchain
                    var auctionLogs = await auctionSuccesfulEvent.GetAllChanges<AuctionSuccessfulEvent>(auctionFilterAll);
                    var auctionCancelledLogs = await auctionSuccesfulEvent.GetAllChanges<AuctionCancelledEvent>(auctionFilterAll);
                    var auctionCreationLogs = await auctionCreatedEvent.GetAllChanges<AuctionCreatedEvent>(auctionCreationFilterAll);

                    if (auctionCancelledLogs != null && auctionCancelledLogs.Count > 0)
                    {
                        foreach (var cancel in auctionCancelledLogs)
                        {
                            //remove from DB
                            await MarketplaceDatabase.RemoveAxie(Convert.ToInt32(cancel.Event.tokenId.ToString()));

                            //remove market trigger
                        }
                    }
                    if (auctionLogs != null && auctionLogs.Count > 0)
                    {
                        foreach (var success in auctionLogs)
                        {
                            //remove from DB
                            await MarketplaceDatabase.RemoveAxie(Convert.ToInt32(success.Event.tokenId.ToString()));

                            //remove market triggers

                        }
                    }

                    if (auctionCreationLogs != null && auctionCreationLogs.Count > 0)
                    {

                        foreach (var log in auctionCreationLogs)
                        {
                            //add to DB
                            await MarketplaceDatabase.AddNewAxie(Convert.ToInt32(log.Event.tokenId.ToString()));
                            //check if should buy now or later
                            await CheckIfShouldBuy(log);
                        }
                    }
                    await Task.Delay(60000);
                    firstBlock = new BlockParameter(new HexBigInteger(lastBlock.BlockNumber.Value + 1));
                    lastBlock = await GetLastBlockCheckpoint(web3);
                }
                catch
                {
                    IsServiceOn = false;
                }
            }
        }


        public static async Task FetchLogsSinceLastCheck()
        {
            Console.WriteLine("Pods per day init");
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
            var firstBlock = new BlockParameter(new HexBigInteger(KeyGetter.GetLastCheckedBlock()));

            //prepare filters 
            var auctionFilterAll = auctionSuccesfulEvent.CreateFilterInput(firstBlock, lastBlock);
            var auctionCancelledFilterAll = auctionCancelled.CreateFilterInput(firstBlock, lastBlock);
            var auctionCreationFilterAll = auctionCreatedEvent.CreateFilterInput(firstBlock, lastBlock);
            var labFilterAll = axieBoughtEvent.CreateFilterInput(firstBlock, lastBlock);

            //get logs from blockchain
            var auctionLogs = await auctionSuccesfulEvent.GetAllChanges<AuctionSuccessfulEvent>(auctionFilterAll);
            //var auctionCancelledLogs = await auctionSuccesfulEvent.GetAllChanges<AuctionCancelledEvent>(auctionFilterAll);
            var labLogs = await axieBoughtEvent.GetAllChanges<AxieBoughtEvent>(labFilterAll);
            //var auctionCreationLogs = await auctionCreatedEvent.GetAllChanges<AuctionCreatedEvent>(auctionCreationFilterAll);

            int eggCount = 0;

            foreach (var log in labLogs)
            {
                eggCount += log.Event.amount;
            }

            var uniqueBuyers = await DbFetch.FetchUniqueBuyers();
            var uniqueGains = 0;
            foreach (var log in auctionLogs)
            {
                if (!uniqueBuyers.Contains(log.Event.winner))
                {
                    uniqueBuyers.Add(log.Event.winner);
                    await DatabaseConnection.GetDb().GetCollection<UniqueBuyer>("UniqueBuyers").InsertOneAsync(new UniqueBuyer(log.Event.winner));
                    uniqueGains++;
                }
            }

            await DatabaseConnection.GetDb().GetCollection<UniqueBuyerGain>("UniqueBuyerGains").InsertOneAsync(new UniqueBuyerGain(LoopHandler.lastUnixTimeCheck, uniqueGains));

            var collec = DatabaseConnection.GetDb().GetCollection<EggCount>("EggSoldPerDay");
            await collec.InsertOneAsync(new EggCount(LoopHandler.lastUnixTimeCheck, eggCount));
            KeyGetter.SetLastCheckedBlock(lastBlock.BlockNumber.Value);
            Console.WriteLine("Pods sync done.");
        }

        public static async Task FetchAllUniqueBuyers()
        {
            var web3 = new Web3("https://mainnet.infura.io");
            var lastBlock = await GetLastBlockCheckpoint(web3);
            var auctionContract = web3.Eth.GetContract(KeyGetter.GetABI("auctionABI"), AxieCoreContractAddress);
            var auctionSuccesfulEvent = auctionContract.GetEvent("AuctionSuccessful");

            var uniqueBuyers = new List<string>();
            var lastBlockvalue = lastBlock.BlockNumber.Value;
            while (lastBlockChecked < lastBlockvalue)
            {
                var latest = lastBlockChecked + 50000;
                if (latest > lastBlockvalue)
                    latest = lastBlockvalue;
                var auctionFilterAll = auctionSuccesfulEvent.CreateFilterInput(new BlockParameter(new HexBigInteger(lastBlockChecked)), new BlockParameter(new HexBigInteger(latest)));
                var auctionLogs = await auctionSuccesfulEvent.GetAllChanges<AuctionSuccessfulEvent>(auctionFilterAll);


                foreach (var logs in auctionLogs)
                {
                    if (!uniqueBuyers.Contains(logs.Event.winner)) uniqueBuyers.Add(logs.Event.winner);
                }
                lastBlockChecked += 50000;
            }
            var collec = DatabaseConnection.GetDb().GetCollection<UniqueBuyer>("UniqueBuyers");
            foreach (var buyers in uniqueBuyers) await collec.InsertOneAsync(new UniqueBuyer(buyers));
        }

        public static async Task FetchCumulUniqueBuyers()
        {
            var web3 = new Web3("https://mainnet.infura.io");
            var lastBlock = await GetLastBlockCheckpoint(web3);
            var auctionContract = web3.Eth.GetContract(KeyGetter.GetABI("auctionABI"), AxieCoreContractAddress);
            var auctionSuccesfulEvent = auctionContract.GetEvent("AuctionSuccessful");

            List<int> list = new List<int>();
            var collec = DatabaseConnection.GetDb().GetCollection<UniqueBuyerGain>("UniqueBuyerGains");
            var uniqueBuyers = new List<string>();
            var uniqueGains = 0;
            var initialTime = await GetBlockTimeStamp(lastBlockChecked, web3);
            var lastBlockvalue = lastBlock.BlockNumber.Value;
            while (lastBlockChecked < lastBlockvalue)
            {
                var latest = lastBlockChecked + 50000;
                if (latest > lastBlockvalue)
                    latest = lastBlockvalue;
                var auctionFilterAll = auctionSuccesfulEvent.CreateFilterInput(new BlockParameter(new HexBigInteger(lastBlockChecked)), new BlockParameter(new HexBigInteger(latest)));
                var auctionLogs = await auctionSuccesfulEvent.GetAllChanges<AuctionSuccessfulEvent>(auctionFilterAll);


                foreach (var logs in auctionLogs)
                {
                    if (!uniqueBuyers.Contains(logs.Event.winner))
                    {
                        uniqueBuyers.Add(logs.Event.winner);
                        uniqueGains++;
                    }
                    var logTime = await GetBlockTimeStamp(logs.Log.BlockNumber.Value, web3);
                    if (logTime - initialTime > 86400)
                    {
                        var diff = logTime - initialTime;
                        var mult = diff / 86400;
                        if (mult > 1)
                        {
                            for (int i = 1; i < mult + 1; i++)
                            {
                                if (i == mult)
                                    await collec.InsertOneAsync(new UniqueBuyerGain(initialTime, uniqueGains));
                                else await collec.InsertOneAsync(new UniqueBuyerGain(initialTime, 0));
                                if (i == mult)
                                    list.Add(uniqueGains);
                                else
                                    list.Add(0);
                                initialTime += 86400;
                            }
                        }
                        else
                        {
                            await collec.InsertOneAsync(new UniqueBuyerGain(initialTime, uniqueGains));
                            list.Add(uniqueGains);
                            initialTime += 86400;
                        }
                        uniqueGains = 0;
                    }
                }
                lastBlockChecked += 50000;
            }
            var sum = list.Sum();

            await collec.InsertOneAsync(new UniqueBuyerGain(initialTime + 86400, uniqueGains));
        }

        private static async Task<int> GetBlockTimeStamp(BigInteger number, Web3 web3)
        {
            var blockParam = new BlockParameter(new HexBigInteger(number));
            var block = await web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(blockParam);
            return Convert.ToInt32(block.Timestamp.Value.ToString());
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

    [Function("bid")]
    public class BidFunction
    {
        [Parameter("address", "_nftAddress", 1)]
        public string address { get; set; }
        [Parameter("uint256", "_tokenId", 1)]
        public BigInteger tokenId { get; set; }
    }
}
