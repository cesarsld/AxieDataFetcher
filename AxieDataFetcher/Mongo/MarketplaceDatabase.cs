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
using AxieDataFetcher.MarketPlaceDatabase;
using AxieDataFetcher.AxieObjects;

namespace AxieDataFetcher.Mongo
{
    public class MarketplaceDatabase
    {
        public static async Task AddNewAxie(int id)
        {
            var collec = DatabaseConnection.GetDb().GetCollection<AxieObjectV2>("MarketplaceDatabase");
            var axie = (await collec.FindAsync(a => a.id == id)).FirstOrDefault();
            if(axie == null)
            await collec.InsertOneAsync(await AxieObjectV2.GetAxieFromApi(id));
        }

        public static async Task RemoveAxie(int id)
        {
            var collec = DatabaseConnection.GetDb().GetCollection<AxieObjectV2>("MarketplaceDatabase");
            var axie = (await collec.FindAsync(a => a.id == id)).FirstOrDefault();
            if (axie != null)
                await collec.DeleteOneAsync(a => a.id == id);
        }

        public static async Task SetupInitialData()
        {
            var list = await ApiCalls.GetAxieListFromMarketplace();
            var collec = DatabaseConnection.GetDb().GetCollection<AxieObjectV2>("MarketplaceDatabase");
            await collec.InsertManyAsync(list);
        }

    }
}
