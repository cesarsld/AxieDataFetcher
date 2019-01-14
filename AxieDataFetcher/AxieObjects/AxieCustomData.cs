using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxieDataFetcher.AxieObjects
{
    //public enum PartType
    //{
    //    mouth,
    //    ears,
    //    eyes,
    //    back,
    //    tail,
    //    horn,
    //    shape,
    //    colour
    //}
    public class AxieDecodedData
    {
        public string _id;
        public int Region; //binary
        public string Tag; //binary
        public AxieDecodedPart[] parts; // 8 in total
        public int breedCount; 
    }

    public class AxieDecodedPart
    {
        public PartType Part;
        public string Skin; //binary
        public AxieDecodedGene Dominant; //binary
        public AxieDecodedGene R1; //binary
        public AxieDecodedGene R2; //binary


    }

    public class AxieDecodedGene
    {
        public string Class;
        public string Name;
    }
}
