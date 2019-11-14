using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
namespace AxieDataFetcher.AxieObjects
{
    class AxieDetailData : AxieObjectV2
    {
        public int bodyType;

        public void GetBodyType()
        {
            var genome = new GeneData(genes);
            genome.GetDataFromGenome();
            bodyType = genome.bodyType;
        }

        public static async Task<AxieDetailData> GetAxieFromApi(int axieId)
        {
            string json = "";
            int downloadTries = 0;
            bool hasFetched = false;
            while (downloadTries < 5 && !hasFetched)
            {
                using (System.Net.WebClient wc = new System.Net.WebClient())
                {
                    try
                    {
                        json = await wc.DownloadStringTaskAsync("https://axieinfinity.com/api/v2/axies/" + axieId.ToString()); //https://axieinfinity.com/api/axies/ || https://api.axieinfinity.com/v1/axies/
                        hasFetched = true;
                    }

                    catch (Exception ex)
                    {
                        if (downloadTries == 5)
                        {
                            return null;
                        }
                        downloadTries++;
                        hasFetched = false;
                        continue;
                    }
                }
            }
            JObject axieJson = JObject.Parse(json);
            AxieDetailData axieData = axieJson.ToObject<AxieDetailData>();
            axieData.SetJson(axieJson);
            return axieData;
        }
    }
}
