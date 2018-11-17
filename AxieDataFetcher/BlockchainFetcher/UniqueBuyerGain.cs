using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxieDataFetcher.BlockchainFetcher
{
    class UniqueBuyerGain
    {
        public int id;
        public int Count;

        public UniqueBuyerGain(int _id, int _count)
        {
            id = _id;
            Count = _count;
        }
    }
}
