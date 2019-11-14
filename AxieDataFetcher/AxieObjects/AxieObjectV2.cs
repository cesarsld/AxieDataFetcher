using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace AxieDataFetcher.AxieObjects
{
    public class AxieObjectV2
    {
        public int id;
        public int birthDate;
        public string name;
        public string owner;
        public string genes;
        public string Class;
        public string title;
        public int sireId;
        public int matronId;
        public AxiePart[] parts;

        public int? exp;
        public int level;
        public int stage;
        public AxieStats stats;
        public AxieAuction auction;
        private JObject jsonData;

        public JObject GetJson() => jsonData;
        public JObject SetJson(JObject json) => jsonData = json;
        public int GetPureness()
        {
            int pureness = 0;
            foreach (var part in parts)
                if (part.Class == Class) pureness++;
            return pureness;
        }

        public int GetAbsolutePureness()
        {
            int[] pureness = new int[6];
            for (int i = 0; i < pureness.Length; i++) pureness[i] = 0;
            foreach (var part in parts)
            {
                switch (part.Class)
                {
                    case "bird":
                        pureness[0]++;
                        break;
                    case "plant":
                        pureness[1]++;
                        break;
                    case "aquatic":
                        pureness[2]++;
                        break;
                    case "bug":
                        pureness[3]++;
                        break;
                    case "beast":
                        pureness[4]++;
                        break;
                    case "reptile":
                        pureness[5]++;
                        break;

                }
            }
            return pureness.Max();
        }

        public int GetDPR()
        {
            int dpr = 0;
            foreach (var part in parts)
            {
                if (part.type == "back" || part.type == "mouth" || part.type == "horn" || part.type == "tail")
                    dpr += part.moves[0].attack * part.moves[0].accuracy / 100;
            }
            return dpr;
        }
        public float GetTNK()
        {
            float tnk = stats.hp;
            foreach (var part in parts)
            {
                if (part.type == "back" || part.type == "mouth" || part.type == "horn" || part.type == "tail")
                    tnk += part.moves[0].defense;
            }
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
        public int mysticCount
        {
            get
            {
                int count = 0;
                foreach (var part in parts)
                    if (part.mystic) count++;
                return count;
            }
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
        public static async Task<AxieObjectV2> GetAxieFromApi(int axieId)
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
            AxieObjectV2 axieData = axieJson.ToObject<AxieObjectV2>();
            axieData.jsonData = axieJson;
            return axieData;
        }

    }
}
