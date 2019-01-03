using System;
using System.Threading;
using MongoDB.Driver;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using MongoDB.Driver.Core;
using System.Data;
using AxieDataFetcher.Mongo;
using AxieDataFetcher.AxieObjects;
using AxieDataFetcher.BattleData;

namespace AxieDataFetcher.MultiThreading
{
    public class MultiThreadHandler
    {
        public static readonly object SyncObj = new object();

        private int battleCount = 0;
        private int perc = 0;
        private int actualPerc = 0;
        private bool fetchRunning;
        private int fetchesToRun;
        private int fetchesCompleted;

        private List<AxieWinrate> winrateList = new List<AxieWinrate>();

        public void MultiThreadLogFetchAll(int length)
        {
            perc = length / 100;
            Parallel.For(1, length, new ParallelOptions { MaxDegreeOfParallelism = /*Environment.ProcessorCount*/ 1 }, (x, state) =>
            {
                WinrateCollector.GetBattleLogsData(x, UpdateWinrates);


            });
        }

        public void UpdateWinrates(List<AxieWinrate> list)
        {
            lock (SyncObj)
            {
                foreach (var wr in list)
                {
                    var match = winrateList.FirstOrDefault(obj => obj.id == wr.id);
                    if (match != null)
                        match.AddLatestResults(wr);
                    else
                        winrateList.Add(wr);
                    
                }
                battleCount++;
                if (battleCount > perc)
                {
                    actualPerc++;
                    Console.WriteLine($"{actualPerc}%");
                }
            }
        }

    }
}
