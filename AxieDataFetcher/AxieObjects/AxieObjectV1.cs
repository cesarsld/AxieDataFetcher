using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace AxieDataFetcher.AxieObjects
{
    public class AxieObjectV1
    {
        public int id;
        public string name;
        public string owner;
        public string genes;
        public string Class;
        public string title;
        public int sireId;
        public int matronId;
        public AxieParts parts;

        public bool hasMystic
        {
            get
            {
                return parts.ears.mystic || parts.mouth.mystic || parts.horn.mystic || parts.tail.mystic || parts.eyes.mystic || parts.back.mystic;
            }
        }
        public int mysticCount
        {
            get
            {
                int count = 0;
                if (parts.ears.mystic) count++;
                if (parts.mouth.mystic) count++;
                if (parts.tail.mystic) count++;
                if (parts.eyes.mystic) count++;
                if (parts.back.mystic) count++;
                if (parts.horn.mystic) count++;
                return count;
            }
        }
        public int exp;
        public int pendingExp;
        public int level;
        public int stage;
        public AxieStats stats;
        public AxieAuction auction;
        public JObject jsonData;
        public JObject oldjsonData;

        public int GetPureness()
        {
            int pureness = 0;
            if (parts.ears.Class == Class) pureness++;
            if (parts.tail.Class == Class) pureness++;
            if (parts.horn.Class == Class) pureness++;
            if (parts.back.Class == Class) pureness++;
            if (parts.eyes.Class == Class) pureness++;
            if (parts.mouth.Class == Class) pureness++;
            return pureness;
        }
        public int GetDPR()
        {
            int dpr = 0;
            dpr += parts.back.moves[0].attack * parts.back.moves[0].accuracy / 100;
            dpr += parts.mouth.moves[0].attack * parts.mouth.moves[0].accuracy / 100;
            dpr += parts.horn.moves[0].attack * parts.horn.moves[0].accuracy / 100;
            dpr += parts.tail.moves[0].attack * parts.tail.moves[0].accuracy / 100;
            return dpr;
        }
        public float GetTNK()
        {
            float tnk = stats.hp;
            tnk += parts.back.moves[0].defense;
            tnk += parts.mouth.moves[0].defense;
            tnk += parts.horn.moves[0].defense;
            tnk += parts.tail.moves[0].defense;
            return tnk;
        }
        public int GetTNKScore()
        {
            float tnk = GetTNK();
            float minTnk = GetMinTNK();
            return (int)Math.Floor((tnk - minTnk) / (GetMaxTNK() - minTnk) * 100);
        }
        public int GetDPRScore()
        {
            int dpr = GetDPR();
            return (int)Math.Floor(GetDPR() / GetMaxDPR() * 100);
        }

        public static float GetMaxDPR() => 91.5f;
        public static float GetMaxTNK() => 129f;
        public static float GetMinTNK() => 33;

        public string GetImageUrl()
        {
            try
            {
                return jsonData["figure"]["static"]["idle"].ToString();
            }
            catch (Exception e)
            {
                return "";
            }
        }
        public static async Task<AxieObjectV1> GetAxieFromApi(int axieId)
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
                        json = await wc.DownloadStringTaskAsync("https://api.axieinfinity.com/v1/axies/" + axieId.ToString()); //https://axieinfinity.com/api/axies/ || https://api.axieinfinity.com/v1/axies/
                        hasFetched = true;
                    }

                    catch (Exception ex)
                    {
                        if (downloadTries == 5)
                        {
                            try
                            {
                                json = await wc.DownloadStringTaskAsync("https://axieinfinity.com/api/axies/" + axieId.ToString());
                                hasFetched = true;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(ex.ToString());
                                return null;
                            }
                        }
                        downloadTries++;
                        hasFetched = false;
                        continue;
                    }
                }
            }
            try
            {
                JObject axieJson = JObject.Parse(json);
                AxieObjectV1 axieData = axieJson.ToObject<AxieObjectV1>();
                axieData.jsonData = axieJson;
                return axieData;
            }
            catch (Exception e)
            {
                return new AxieObjectV1 { id = 0 };
            }
        }

        public async Task GetTrueAuctionData()
        {
            string json = "";
            using (System.Net.WebClient wc = new System.Net.WebClient())
            {
                try
                {
                    json = await wc.DownloadStringTaskAsync("https://axieinfinity.com/api/axies/" + id.ToString()); //https://axieinfinity.com/api/axies/
                }

                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return;
                }
            }
            JObject axieJson = JObject.Parse(json);
            auction = axieJson["auction"].ToObject<AxieAuction>();
        }
        public async Task<int> GetTrueBeedData()
        {
            string json = "";
            using (System.Net.WebClient wc = new System.Net.WebClient())
            {
                try
                {
                    json = await wc.DownloadStringTaskAsync("https://axieinfinity.com/api/axies/" + id.ToString()); //https://axieinfinity.com/api/axies/
                }

                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());

                }
            }
            JObject axieJson = JObject.Parse(json);
            return (int)axieJson["expForBreeding"];
        }
    }

}
