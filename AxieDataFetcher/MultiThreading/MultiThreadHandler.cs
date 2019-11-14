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
using AxieDataFetcher.Core;
using AxieDataFetcher.BattleData;

namespace AxieDataFetcher.MultiThreading
{
    public class MatchPlayers
    {
        public string p1;
        public string p2;
        public int time;

        public MatchPlayers(string add1, string add2, int _time)
        {
            p1 = add1;
            p2 = add2;
            time = _time;
        }
    }
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

        private List<MatchPlayers> playerList = new List<MatchPlayers>();

        public void MultiThreadLogFetchAll(int startBattle, int endBattle)
        {
            perc = (endBattle - startBattle) / 100;
            Parallel.For(startBattle + 1, endBattle + 1, new ParallelOptions { MaxDegreeOfParallelism = 4 }, (x, state) =>
            {
                WinrateCollector.GetBattleLogsData(x, UpdateWinrates, UpdatePracticeWinrates, UpdateUniquePlayers);
            });

            var db = DatabaseConnection.GetDb();
            var collection = db.GetCollection<BsonDocument>("AxieWinrate");
            var practiceCollec = db.GetCollection<BsonDocument>("PracticeAxieWinrate");
            Console.WriteLine("DB write phase");
            int countr = winrateList.Count / 100;
            int tick = 0;
            int percc = 0;
            foreach (var wr in winrateList)
            {
                tick++;
                if (tick > countr)
                {
                    tick = 0;
                    percc++;
                    Console.WriteLine($"{percc}%");
                }
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
                    try
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
                    catch (Exception e)
                    {
                        Logger.Log(e.Message + $" Issue at axie {wr.id}");
                    }
                }
            }

            var dauCollec = db.GetCollection<DailyUsers>("DailyBattleDAU");
            var lastEleDau = (dauCollec.FindAsync(u => u.id > 0).GetAwaiter().GetResult()).ToList().OrderBy(a => a.id).Last();
            var lastTime = lastEleDau.id;
            playerList = playerList.OrderBy(d => d.time).ToList();
            int time = lastTime + 86400;
            var list = new List<string>();
            int matchTime = 0;
            var str = JsonConvert.SerializeObject(playerList);
            Logger.StoreData(str);
            foreach (var day in playerList)
            {
                if (day.time > time)
                {
                    dauCollec.InsertOne(new DailyUsers(time, list.Count));
                    time += 86400;
                    Logger.Log($"Data in list after day {list.Count}");
                    list.Clear();
                }
                else
                {
                    if (!list.Contains(day.p1))
                    {
                        list.Add(day.p1);
                    }
                    if (!list.Contains(day.p2))
                    {
                        list.Add(day.p2);
                    }
                    matchTime = day.time;
                }
            }
            if (time - matchTime < 86400 / 4)
            {
                Logger.Log($"Data in list after last day {list.Count}");
                dauCollec.InsertOne(new DailyUsers(time, list.Count));
                time += 86400;
                list.Clear();
            }

            db.GetCollection<DailyBattles>("CumulDailyBattles").InsertOneAsync(new DailyBattles(LoopHandler.lastUnixTimeCheck, endBattle - startBattle)).GetAwaiter().GetResult();
            using (var tw = new StreamWriter("AxieData/LastCheck.txt"))
            {
                tw.Write((endBattle - 1).ToString());
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

        public void UpdateUniquePlayers(string add1, string add2, int time)
        {
            lock (SyncObj)
            {
                playerList.Add(new MatchPlayers(add1, add2, time));
            }
        }

    }
}
