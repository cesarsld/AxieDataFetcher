using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AxieDataFetcher.BlockchainFetcher;
using AxieDataFetcher.EggsSpawnedData;
using AxieDataFetcher.Core;
using AxieDataFetcher.BattleData;
using AxieDataFetcher.MultiThreading;
using System.Diagnostics;

namespace AxieDataFetcher
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            //WinrateCollector.GetCumulBattleCount().GetAwaiter().GetResult();
            //inrateCollector.GetBattleDataSinceLastCheck().GetAwaiter().GetResult();
            LoopHandler.UpdateServiceCheckLoop().GetAwaiter().GetResult();

            //AxieDataGetter.FetchCumulUniqueBuyers().GetAwaiter().GetResult();
        }
    }
}
