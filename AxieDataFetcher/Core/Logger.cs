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
        public static void Log(string error)
        {
            using (StreamWriter sw = new StreamWriter(logPath, true))
            {
                sw.Write(error + "\n");
            }
        }
    }
}
