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
using MongoDB.Driver.Core;
using System.Data;
using AxieDataFetcher.Core;
using AxieDataFetcher.Mongo;
using AxieDataFetcher.AxieObjects;
namespace AxieDataFetcher.EggsSpawnedData
{
    public class EggsSpawnDataFetcher
    {
        private static async Task<int> GetAxieCount()
        {
            var json = "";
            //https://axieinfinity.com/api/axies
            using (System.Net.WebClient wc = new System.Net.WebClient())
            {
                try
                {
                    json = await wc.DownloadStringTaskAsync("https://axieinfinity.com/api/axies"); //https://axieinfinity.com/api/axies/ || https://api.axieinfinity.com/v1/axies/

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            JObject axieObj = JObject.Parse(json);
            return (int)axieObj["totalAxies"];
        }

        public static async Task GetAllEggsSpawnedData()
        {
            int axieCount = await GetAxieCount();
            int count = 1;

            int time = 0;
            int eggSpawned = 0;
            while (count < axieCount)
            {
                Console.Clear();
                Console.WriteLine(count);
                var axieData = await AxieObjectOld.GetAxieFromApi(count);
                if (time == 0) time = axieData.birthDate;
                if(axieData.birthDate - time > 86400)
                {
                    var collec = DatabaseConnection.GetDb().GetCollection<EggCount>("EggsPerDay");
                    await collec.InsertOneAsync(new EggCount(time, eggSpawned));
                    eggSpawned = 0;
                    time += 86400;
                }
                if(axieData.sireId != 0) eggSpawned++;
                count++;
            }
            KeyGetter.SetLastCheckedAxie(axieCount);
        }

        public static async Task GetEggsSpawnedFromCheckpoint()
        {
            Console.WriteLine("Breeds per day init");
            var collec = DatabaseConnection.GetDb().GetCollection<EggCount>("EggsPerDay");
            var time = LoopHandler.lastUnixTimeCheck;
            int count = KeyGetter.GetLastCheckedAxie();
            int axieCount = await GetAxieCount();
            int eggSpawned = 0;
            var dataPoints = new List<EggCount>();
            while (count < axieCount)
            {
                var axieData = await AxieObjectV1.GetAxieFromApi(count);
                if (axieData.sireId != 0) eggSpawned++;
                count++;
            }

            await collec.InsertOneAsync(new EggCount(Convert.ToInt32(((DateTimeOffset)(DateTime.UtcNow)).ToUnixTimeSeconds()), eggSpawned));

            KeyGetter.SetLastCheckedAxie(axieCount);
        }

    }
}
