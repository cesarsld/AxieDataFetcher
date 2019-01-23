using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using AxieDataFetcher.Core;
using AxieDataFetcher.AxieObjects;
using AxieDataFetcher.BlockchainFetcher;
namespace AxieDataFetcher.Mongo
{
    public class Utils
    {
        public static async Task PushLandData (int[] data, int time)
        { 
            for (int i = 0; i < data.Length; i++)
            {
                await DatabaseConnection.GetDb()
                .GetCollection<LandCount>("LandChest" + i.ToString())
                .InsertOneAsync(new LandCount(time, data[i]));
            }
        }

        public static async Task UpdateLandHolders (List<string>)
        { }
    }
}
