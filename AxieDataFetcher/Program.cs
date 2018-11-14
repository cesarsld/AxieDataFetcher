using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AxieDataFetcher.BlockchainFetcher;

namespace AxieDataFetcher
{
    class Program
    {
        static void Main(string[] args)
        {
            AxieDataGetter.FetchLogsFromRange().GetAwaiter().GetResult();
            Console.WriteLine("Hello World!");
        }
    }
}

