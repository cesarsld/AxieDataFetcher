using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AxieDataFetcher.AxieObjects;
using System.Numerics;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.IO;

namespace AxieDataFetcher.BlockchainFetcher
{
    public class DaySummary
    {
        //typeCode for mysticCount -1, 1, 2, 3, 4, 5
        //typeCode for tags Untagged = 0, origin = 1, meo = 2, meo2 = 3, Agamo = 4
        //typeCode for bodyType convert binary body in decimal
        //typeCode for parts 0 + use string field for part
        public int id;
        //SaleType type;
        //int typeCode;
        //string partName;
        public DailySaleData floor;
        public DailySaleData ceiling;

        public DaySummary(int time, SaleType _type, int _typeCode, string name = "")
        {

        }

        public DaySummary(int time)
        {
            id = time;
            floor = new DailySaleData();
            ceiling = new DailySaleData();
        }

        public void UpdateData(DailySaleData sale)
        {
            if (sale.GetPrice() > ceiling.GetPrice()) ceiling = sale;
            if (sale.GetPrice() < floor.GetPrice() || floor.GetPrice() == 0) floor = sale;
        }
    }
    public class DailySaleData
    {
        
        private BigInteger price;
        public void SetPriceToString() => priceString = price.ToString();
        public string priceString;
        public int axieId;

        public BigInteger GetPrice() => price;
        public DailySaleData(AxieObjectV2 _axie, BigInteger _price)
        {
            axieId = _axie.id;
            price = _price;
            SetPriceToString();
        }
        public DailySaleData()
        {
            axieId = -1;
            price = 0;
        }
    }

    public class SaleTypeList : List<DaySummary>
    {
        public void AddNewDataPoint(int time) => Add(new DaySummary(time));

        public void UpdateData(DailySaleData data)
        {
            if (Count > 0)
            {
                 this[Count - 1].UpdateData(data);
            }
        }
    }
}
