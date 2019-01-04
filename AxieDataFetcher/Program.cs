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
            var sw = new Stopwatch();
            Console.WriteLine("Hello World!");
            //WinrateCollector.GetCumulBattleCount().GetAwaiter().GetResult();
            //inrateCollector.GetBattleDataSinceLastCheck().GetAwaiter().GetResult();
            //LoopHandler.UpdateServiceCheckLoop().GetAwaiter().GetResult();
            sw.Start();
            new MultiThreadHandler().MultiThreadLogFetchAll(200);
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);
            Console.ReadLine();
            //AxieDataGetter.FetchCumulUniqueBuyers().GetAwaiter().GetResult();
        }
    }
}
