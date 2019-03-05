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
using AxieDataFetcher.AxieObjects;
using System.Linq;
using System.IO;
using System.Text;

namespace AxieDataFetcher.MarketPlaceDatabase
{
    public class ApiCalls
    {

        public static async Task<List<AxieObjectV2>> GetAxieListFromMarketplace()
        {
            var axieList = new List<AxieObjectV2>();
            bool dataAvailable = true;
            bool setupDone = false;
            int axiePages = 9999;
            int axieIndex = 0;
            int safetyNet = 0;
            int perc = axiePages / 100;
            while (axieIndex < axiePages && dataAvailable)
            {

                Console.WriteLine($"Page {axieIndex} out of {axiePages}");

                string json = null;
                using (System.Net.WebClient wc = new System.Net.WebClient())
                {
                    try
                    {
                        //var uri = new Uri("https://axieinfinity.com/api/addresses/" + address + "/axies?offset=" + (12 * axieIndex).ToString());
                        json = await wc.DownloadStringTaskAsync("https://axieinfinity.com/api/axies?breedable&language=en&offset=" + (12 * axieIndex).ToString() + "&sale=1&sorting=lowest_price");
                        safetyNet = 0;
                    }
                    catch (Exception ex)
                    {
                        safetyNet++;
                        if (safetyNet > 25) axieIndex++;
                        axieIndex--;
                    }
                }
                axieIndex++;
                if (json != null)
                {
                    JObject addressJson = JObject.Parse(json);
                    if (!setupDone)
                    {
                        axiePages = (int)addressJson["totalPages"];
                        setupDone = true;
                    }
                    foreach (var axie in addressJson["axies"])
                    {
                        AxieObjectV2 axieData = new AxieObjectV2();
                        //try
                        //{
                        axieData = axie.ToObject<AxieObjectV2>();
                        //}
                        //catch (Exception e)
                        //{ Console.WriteLine(e.Message); }
                        axieData.jsonData = JObject.Parse(axie.ToString());
                        if (axieData.stage <= 2)
                        {
                            Console.WriteLine("Axie still egg, moving on.");
                            continue;
                        }
                        axieList.Add(axieData);
                    }
                }
            }
            return axieList;
        }

    }
}
