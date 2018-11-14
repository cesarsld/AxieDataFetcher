using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxieDataFetcher.EggsSpawnedData
{
    class EggCount
    {
        public int id;
        public int Count;

        public EggCount(int _id, int _count)
        {
            id = _id;
            Count = _count;
        }
    }
}
