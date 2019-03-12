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
using AxieDataFetcher.AxieObjects;
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
        private List<AxieWinrate> practiceWinrateList = new List<AxieWinrate>();

        public void MultiThreadLogFetchAll(int startBattle, int endBattle)
        {
            perc = (endBattle - startBattle) / 100;
            Parallel.For(startBattle + 1, endBattle + 1, new ParallelOptions { MaxDegreeOfParallelism = 4 }, (x, state) =>
            {
                WinrateCollector.GetBattleLogsData(x, UpdateWinrates, UpdatePracticeWinrates);
            });

            var db = DatabaseConnection.GetDb();
            var collection = db.GetCollection<BsonDocument>("AxieWinrate");
            var practiceCollec = db.GetCollection<BsonDocument>("PracticeAxieWinrate");

            foreach (var wr in winrateList)
            {
                var filterId = Builders<BsonDocument>.Filter.Eq("_id", wr.id);
                var doc = collection.Find(filterId).FirstOrDefault();
                if (doc != null)
                {
                    var axieData = BsonSerializer.Deserialize<AxieWinrate>(doc);
                    axieData.AddLatestResults(wr);
                    var update = Builders<BsonDocument>.Update
                                                       .Set("win", axieData.win)
                                                       .Set("loss", axieData.loss)
                                                       .Set("winrate", axieData.winrate)
                                                       .Set("battleHistory", axieData.battleHistory)
                                                       .Set("lastBattleDate", axieData.lastBattleDate)
                                                       .Set("wonBattles", axieData.wonBattles)
                                                       .Set("lostBattles", axieData.lostBattles);
                    collection.UpdateOne(filterId, update);
                }
                else
                {
                    var data = AxieObjectV2.GetAxieFromApi(wr.id).GetAwaiter().GetResult();
                    wr.moves = new string[4];
                    var index = 0;
                    foreach (var move in data.parts)
                    {
                        switch (move.type)
                        {
                            case "mouth":
                            case "back":
                            case "horn":
                            case "tail":
                                wr.moves[index] = move.name;
                                index++;
                                break;

                        }
                    }
                    collection.InsertOne(wr.ToBsonDocument());
                }
            }
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
                if (battleCount >= perc)
                {
                    actualPerc++;
                    battleCount = 0;
                    Console.WriteLine($"{actualPerc}%");
                }
            }
        }

        public void UpdatePracticeWinrates(List<AxieWinrate> list)
        {
            lock (SyncObj)
            {
                foreach (var wr in list)
                {
                    var match = practiceWinrateList.FirstOrDefault(obj => obj.id == wr.id);
                    if (match != null)
                        match.AddLatestResults(wr);
                    else
                        practiceWinrateList.Add(wr);

                }
                battleCount++;
                if (battleCount >= perc)
                {
                    actualPerc++;
                    battleCount = 0;
                    Console.WriteLine($"{actualPerc}%");
                }
            }
        }

    }
}
