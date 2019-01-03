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
using AxieDataFetcher.Core;

namespace AxieDataFetcher.BattleData
{
    //https://api.axieinfinity.com/v1/battle/teams/?address=0x4ce15b37851a4448a28899062906a02e51dee267&offset=0&count=10

    class WinrateCollector
    {

        public static readonly object SyncObj = new object();

        public static void GetAllData()
        {
            Dictionary<int, Winrate> winrateData = new Dictionary<int, Winrate>();
            List<AxieWinrate> winrateList = new List<AxieWinrate>();
            int battleCount = 29950;
            int axieIndex = 0;
            int safetyNet = 0;
            int perc = battleCount / 100;
            int currentPerc = 0;
            while (axieIndex < battleCount)
            {
                axieIndex++;
                if (axieIndex % perc == 0)
                {
                    currentPerc++;
                    Console.WriteLine(currentPerc.ToString() + "%");
                }
                string json = null;
                using (System.Net.WebClient wc = new System.Net.WebClient())
                {
                    try
                    {
                        json = wc.DownloadString("https://api.axieinfinity.com/v1/battle/history/matches/" + axieIndex.ToString());
                        safetyNet = 0;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        safetyNet++;
                    }
                }
                if (json != null)
                {
                    JObject axieJson = JObject.Parse(json);
                    JObject script = JObject.Parse((string)axieJson["script"]);
                    int[] team1 = new int[3];
                    int[] team2 = new int[3];
                    for (int i = 0; i < 3; i++)
                    {
                        team1[i] = (int)script["metadata"]["fighters"][i]["id"];
                        team2[i] = (int)script["metadata"]["fighters"][i + 3]["id"];
                    }
                    int winningAxie = (int)script["result"]["lastAlive"][0];
                    int[] winningTeam;
                    int[] losingTeam;
                    if (team1.Contains(winningAxie))
                    {
                        winningTeam = team1;
                        losingTeam = team2;
                    }
                    else
                    {
                        losingTeam = team1;
                        winningTeam = team2;
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        var winner = winrateList.FirstOrDefault(a => a.id == winningTeam[i]);
                        if (winner != null)
                        {
                            winner.win++;
                            winner.battleHistory += "1";
                            winner.wonBattles.Add(axieIndex);
                        }
                        else winrateList.Add(new AxieWinrate(winningTeam[i], 1, 0, "0x1", LoopHandler.lastUnixTimeCheck, axieIndex, true));

                        var loser = winrateList.FirstOrDefault(a => a.id == losingTeam[i]);
                        if (loser != null)
                        {
                            loser.loss++;
                            loser.battleHistory += "0";
                            loser.lostBattles.Add(axieIndex);
                        }
                        else winrateList.Add(new AxieWinrate(losingTeam[i], 0, 1, "0x0", LoopHandler.lastUnixTimeCheck, axieIndex, false));
                    }
                }
            }
            Console.WriteLine("Data Fetched. Initialising DB write phase");

            foreach (var axie in winrateList) axie.GetWinrate();
            var db = DatabaseConnection.GetDb();
            var collection = db.GetCollection<BsonDocument>("AxieWinrateTest");
            float percDB = (float)winrateList.Count / 100f;
            int counter = 0;
            int currentperc = 0;
            foreach (var axie in winrateList)
            {
                counter++;
                if (counter > perc)
                {
                    currentperc++;
                    counter = 0;
                    Console.WriteLine($"{currentperc}%");
                }
                collection.InsertOne(axie.ToBsonDocument());
            }
        }

        public static void GetBattleLogsData(int battleId, Action<List<AxieWinrate>> updateList)
        {
            Dictionary<int, Winrate> winrateData = new Dictionary<int, Winrate>();
            List<AxieWinrate> winrateList = new List<AxieWinrate>();

            string json = null;
            using (System.Net.WebClient wc = new System.Net.WebClient())
            {
                try
                {
                    json = wc.DownloadString("https://api.axieinfinity.com/v1/battle/history/matches/" + battleId.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            if (json != null)
            {
                JObject axieJson = JObject.Parse(json);
                JObject script = JObject.Parse((string)axieJson["script"]);
                int time = Convert.ToInt32(((string)axieJson["createdAt"]).Remove(((string)axieJson["createdAt"]).Length - 3, 3));

                int[] team1 = new int[3];
                int[] team2 = new int[3];
                for (int i = 0; i < 3; i++)
                {
                    team1[i] = (int)script["metadata"]["fighters"][i]["id"];
                    team2[i] = (int)script["metadata"]["fighters"][i + 3]["id"];
                }
                int winningAxie = (int)script["result"]["lastAlive"][0];
                int[] winningTeam;
                int[] losingTeam;
                if (team1.Contains(winningAxie))
                {
                    winningTeam = team1;
                    losingTeam = team2;
                }
                else
                {
                    losingTeam = team1;
                    winningTeam = team2;
                }
                for (int i = 0; i < 3; i++)
                {
                    var winner = winrateList.FirstOrDefault(a => a.id == winningTeam[i]);
                    if (winner != null)
                    {
                        winner.win++;
                        winner.battleHistory += "1";
                        winner.wonBattles.Add(battleId);
                    }
                    else winrateList.Add(new AxieWinrate(winningTeam[i], 1, 0, "0x1", time, battleId, true));

                    var loser = winrateList.FirstOrDefault(a => a.id == losingTeam[i]);
                    if (loser != null)
                    {
                        loser.loss++;
                        loser.battleHistory += "0";
                        loser.lostBattles.Add(battleId);
                    }
                    else winrateList.Add(new AxieWinrate(losingTeam[i], 0, 1, "0x0", time, battleId, false));
                }
                updateList(winrateList);
            }
        }


        public static void GetInitUniquePlayers()
        {
            Dictionary<int, Winrate> winrateData = new Dictionary<int, Winrate>();
            List<AxieWinrate> winrateList = new List<AxieWinrate>();
            List<string> uniqueUsers = new List<string>();
            int timeCheck = 0;
            int battleCount = 78634;
            int axieIndex = 0;
            int safetyNet = 0;
            int perc = battleCount / 100;
            int currentPerc = 0;
            var db1 = DatabaseConnection.GetDb();
            var collection1 = db1.GetCollection<DailyUsers>("DailyBattleDAUTest");
            while (axieIndex < battleCount)
            {
                axieIndex++;
                if (axieIndex % perc == 0)
                {
                    currentPerc++;
                    Console.WriteLine(currentPerc.ToString() + "%");
                }
                string json = null;
                using (System.Net.WebClient wc = new System.Net.WebClient())
                {
                    try
                    {
                        json = wc.DownloadString("https://api.axieinfinity.com/v1/battle/history/matches/" + axieIndex.ToString());
                        safetyNet = 0;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        safetyNet++;
                    }
                }
                if (json != null)
                {
                    JObject axieJson = JObject.Parse(json);
                    int time = Convert.ToInt32(((string)axieJson["createdAt"]).Remove(((string)axieJson["createdAt"]).Length - 3, 3));
                    if (timeCheck == 0) timeCheck = time;
                    if (time - timeCheck > 86400)
                    {
                        Console.WriteLine("Day passed");
                        var dailyData = new DailyUsers(time, uniqueUsers.Count);
                        collection1.InsertOne(dailyData);
                        timeCheck += 86400;
                        uniqueUsers.Clear();
                    }
                    if (!uniqueUsers.Contains((string)axieJson["winner"])) uniqueUsers.Add((string)axieJson["winner"]);
                    if (!uniqueUsers.Contains((string)axieJson["loser"])) uniqueUsers.Add((string)axieJson["loser"]);
                }
            }

        }

        public static async Task GetCumulBattleCount()
        {
            string dataCountUrl = "https://api.axieinfinity.com/v1/battle/history/matches-count";
            int lastChecked = 0;
            int lastBattle = 0;
            int apiPerc = 0;
            int counter = 0;
            int battleCount = 0;
            int timeCheck = 0;
            using (System.Net.WebClient wc = new System.Net.WebClient())
            {
                lastBattle = Convert.ToInt32((await wc.DownloadStringTaskAsync(dataCountUrl)));
            }
            float perc = (float)lastBattle / 100;
            while (lastChecked < lastBattle)
            {
                lastChecked++;
                counter++;
                if (counter > perc)
                {
                    apiPerc++;
                    counter = 0;
                    Console.WriteLine($"{apiPerc}%");
                }
                string json = null;
                try
                {
                    using (System.Net.WebClient wc = new System.Net.WebClient())
                    {
                        json = wc.DownloadString("https://api.axieinfinity.com/v1/battle/history/matches/" + lastChecked.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                JObject axieJson = JObject.Parse(json);
                battleCount++;
                int time = Convert.ToInt32(((string)axieJson["createdAt"]).Remove(((string)axieJson["createdAt"]).Length - 3, 3));
                if (timeCheck == 0) timeCheck = time;
                if (time - timeCheck > 86400)
                {
                    await DatabaseConnection.GetDb().GetCollection<DailyBattles>("CumulDailyBattles").InsertOneAsync(new DailyBattles(timeCheck, battleCount));
                    battleCount = 0;
                    timeCheck += 86400;
                }
            }
            if (battleCount > 0) await DatabaseConnection.GetDb().GetCollection<DailyBattles>("CumulDailyBattles").InsertOneAsync(new DailyBattles(LoopHandler.lastUnixTimeCheck, battleCount));
        }

        public static async Task GetBattleDataSinceLastCheck()
        {
            Console.WriteLine("WR per day init");
            List<string> uniqueUsers = new List<string>();
            string dataCountUrl = "https://api.axieinfinity.com/v1/battle/history/matches-count";
            string battleNumberPath = "AxieData/LastCheck.txt";
            int lastChecked = 0;
            int lastBattle = 0;
            int apiPerc = 0;
            int counter = 0;
            int battleCount = 0;
            using (System.Net.WebClient wc = new System.Net.WebClient())
            {
                lastBattle = Convert.ToInt32((await wc.DownloadStringTaskAsync(dataCountUrl)));
            }
            using (StreamReader sr = new StreamReader(battleNumberPath, Encoding.UTF8))
            {
                lastChecked = Convert.ToInt32(sr.ReadToEnd());
            }
            List<AxieWinrate> winrateList = new List<AxieWinrate>();
            List<AxieWinrate> practiceWinrateList = new List<AxieWinrate>();
            int total = lastBattle - lastChecked;
            float perc = (float)total / 100;

            while (lastChecked < lastBattle)
            {
                lastChecked++;
                counter++;
                if (counter > perc)
                {
                    apiPerc++;
                    counter = 0;
                    Console.WriteLine($"{apiPerc}%");
                }
                string json = null;
                try
                {
                    using (System.Net.WebClient wc = new System.Net.WebClient())
                    {
                        json = wc.DownloadString("https://api.axieinfinity.com/v1/battle/history/matches/" + lastChecked.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                if (json != null)
                {
                    battleCount++;
                    JObject axieJson = JObject.Parse(json);
                    JObject script = JObject.Parse((string)axieJson["script"]);
                    int[] team1 = new int[3];
                    int[] team2 = new int[3];
                    for (int i = 0; i < 3; i++)
                    {
                        team1[i] = (int)script["metadata"]["fighters"][i]["id"];
                        team2[i] = (int)script["metadata"]["fighters"][i + 3]["id"];
                    }
                    int winningAxie = (int)script["result"]["lastAlive"][0];
                    int[] winningTeam;
                    int[] losingTeam;
                    if (team1.Contains(winningAxie))
                    {
                        winningTeam = team1;
                        losingTeam = team2;
                    }
                    else
                    {
                        losingTeam = team1;
                        winningTeam = team2;
                    }
                    if (axieJson["expUpdates"].Count() > 0)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            var winner = winrateList.FirstOrDefault(a => a.id == winningTeam[i]);
                            if (winner != null)
                            {
                                winner.win++;
                                winner.battleHistory += "1";
                                winner.wonBattles.Add(lastChecked);
                            }
                            else winrateList.Add(new AxieWinrate(winningTeam[i], 1, 0, "0x1", LoopHandler.lastUnixTimeCheck, lastChecked, true));

                            var loser = winrateList.FirstOrDefault(a => a.id == losingTeam[i]);
                            if (loser != null)
                            {
                                loser.loss++;
                                loser.battleHistory += "0";
                                loser.lostBattles.Add(lastChecked);
                            }
                            else winrateList.Add(new AxieWinrate(losingTeam[i], 0, 1, "0x0", LoopHandler.lastUnixTimeCheck, lastChecked, false));
                        }
                    }
                    else
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            var winner = practiceWinrateList.FirstOrDefault(a => a.id == winningTeam[i]);
                            if (winner != null)
                            {
                                winner.win++;
                                winner.battleHistory += "1";
                                winner.wonBattles.Add(lastChecked);
                            }
                            else practiceWinrateList.Add(new AxieWinrate(winningTeam[i], 1, 0, "0x1", LoopHandler.lastUnixTimeCheck, lastChecked, true));

                            var loser = practiceWinrateList.FirstOrDefault(a => a.id == losingTeam[i]);
                            if (loser != null)
                            {
                                loser.loss++;
                                loser.battleHistory += "0";
                                loser.lostBattles.Add(lastChecked);
                            }
                            else practiceWinrateList.Add(new AxieWinrate(losingTeam[i], 0, 1, "0x0", LoopHandler.lastUnixTimeCheck, lastChecked, false));
                        }
                    }
                    if (!uniqueUsers.Contains((string)axieJson["winner"])) uniqueUsers.Add((string)axieJson["winner"]);
                    if (!uniqueUsers.Contains((string)axieJson["loser"])) uniqueUsers.Add((string)axieJson["loser"]);


                }
            }
            foreach (var axie in winrateList) axie.GetWinrate();
            var db = DatabaseConnection.GetDb();
            var collection = db.GetCollection<BsonDocument>("AxieWinrate");
            var practiceCollec = db.GetCollection<BsonDocument>("PracticeAxieWinrate");
            Console.WriteLine("Initialising DB write phase");
            int dbPerc = 0;
            perc = (float)winrateList.Count / 100;
            counter = 0;
            foreach (var axie in winrateList)
            {
                counter++;
                if (counter > perc)
                {
                    dbPerc++;
                    counter = 0;
                    Console.WriteLine($"{dbPerc}%");
                }
                var filterId = Builders<BsonDocument>.Filter.Eq("_id", axie.id);
                var doc = collection.Find(filterId).FirstOrDefault();
                if (doc != null)
                {
                    var axieData = BsonSerializer.Deserialize<AxieWinrate>(doc);
                    axieData.AddLatestResults(axie);
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
                    var data = await AxieObjectOld.GetAxieFromApi(axie.id);
                    axie.moves = new string[4];
                    var index = 0;
                    foreach (var move in data.parts)
                    {
                        switch (move.type)
                        {
                            case "mouth":
                            case "back":
                            case "horn":
                            case "tail":
                                axie.moves[index] = move.name;
                                index++;
                                break;

                        }
                    }
                    collection.InsertOne(axie.ToBsonDocument());
                }
            }
            dbPerc = 0;
            foreach (var axie in practiceWinrateList)
            {
                counter++;
                if (counter > perc)
                {
                    dbPerc++;
                    counter = 0;
                    Console.WriteLine($"{dbPerc}%");
                }
                var filterId = Builders<BsonDocument>.Filter.Eq("_id", axie.id);
                var doc = practiceCollec.Find(filterId).FirstOrDefault();
                if (doc != null)
                {
                    var axieData = BsonSerializer.Deserialize<AxieWinrate>(doc);
                    axieData.AddLatestResults(axie);
                    var update = Builders<BsonDocument>.Update
                                                       .Set("win", axieData.win)
                                                       .Set("loss", axieData.loss)
                                                       .Set("winrate", axieData.winrate)
                                                       .Set("battleHistory", axieData.battleHistory)
                                                       .Set("lastBattleDate", axieData.lastBattleDate)
                                                       .Set("wonBattles", axieData.wonBattles)
                                                       .Set("lostBattles", axieData.lostBattles);
                    practiceCollec.UpdateOne(filterId, update);
                }
                else practiceCollec.InsertOne(axie.ToBsonDocument());
            }


            var collecDau = db.GetCollection<DailyUsers>("DailyBattleDAU");
            var dailyData = new DailyUsers(LoopHandler.lastUnixTimeCheck, uniqueUsers.Count);
            await collecDau.InsertOneAsync(dailyData);
            await db.GetCollection<DailyBattles>("CumulDailyBattles").InsertOneAsync(new DailyBattles(LoopHandler.lastUnixTimeCheck, battleCount));

            using (var tw = new StreamWriter(battleNumberPath))
            {
                tw.Write((lastBattle - 1).ToString());
            }
            Console.WriteLine("Wr sync done.");
            Console.Clear();
        }
    }
}
