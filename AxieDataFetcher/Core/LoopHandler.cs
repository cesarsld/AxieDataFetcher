using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AxieDataFetcher.BattleData;

namespace AxieDataFetcher.Core
{
    class LoopHandler
    {

        public static async Task UpdateServiceCheckLoop()
        {
            while (true)
            {
                int unixTime = Convert.ToInt32(((DateTimeOffset)(DateTime.UtcNow)).ToUnixTimeSeconds());
                if (WinrateCollector.lastUnixTimeCheck == 0) WinrateCollector.UpdateUnixLastCheck();
                if (unixTime - WinrateCollector.lastUnixTimeCheck >= WinrateCollector.unixTimeBetweenUpdates) _ = WinrateCollector.GetDataSinceLastChack();

                await Task.Delay(60000);
            }
        }
    }
}
