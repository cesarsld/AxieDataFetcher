using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AxieDataFetcher.Core
{
    class Logger
    {
        private static readonly string logPath = "Logger/Log.txt";
        private static readonly string dataPath = "Logger/Data.txt";
        public static void Log(string error)
        {
            using (StreamWriter sw = new StreamWriter(logPath, true))
            {
                sw.Write(error + "\n");
            }
        }

        public static void StoreData(string data)
        {
            using (StreamWriter sw = new StreamWriter(dataPath, true))
            {
                sw.WriteLine(data);
            }
        }

        public static string GetLog()
        {
            using (StreamReader sr = new StreamReader(logPath))
            {
                return sr.ReadToEnd();
            }
        }
    }
}
