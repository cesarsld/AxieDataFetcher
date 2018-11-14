using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AxieDataFetcher.BattleData;
using AxieDataFetcher.EggsSpawnedData;
using AxieDataFetcher.BlockchainFetcher;

namespace AxieDataFetcher.Core
{
    class LoopHandler
    {
        public static int lastUnixTimeCheck = 0;
        public static async Task UpdateServiceCheckLoop()
        {
            while (true)
            {
                int unixTime = Convert.ToInt32(((DateTimeOffset)(DateTime.UtcNow)).ToUnixTimeSeconds());
                if (lastUnixTimeCheck == 0) UpdateUnixLastCheck();
                if (unixTime - lastUnixTimeCheck >= WinrateCollector.unixTimeBetweenUpdates)
                {

                    lastUnixTimeCheck = unixTime;
                    using (var tw = new StreamWriter("AxieData/LastTimeCheck.txt"))
                    {
                        tw.Write(lastUnixTimeCheck.ToString());
                    }
                    _ = WinrateCollector.GetWrSinceLastChack();
                    _ = EggsSpawnDataFetcher.GetEggsSpawnedFromCheckpoint();
                    _ = AxieDataGetter.FetchLogsSinceLastCheck();
                }

                await Task.Delay(60000);
            }
        }

        public static void UpdateUnixLastCheck()
        {
            using (StreamReader sr = new StreamReader("AxieData/LastTimeCheck.txt", Encoding.UTF8))
            {
                lastUnixTimeCheck = Convert.ToInt32(sr.ReadToEnd());
            }
        }
    }
}
