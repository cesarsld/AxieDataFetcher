using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Threading.Tasks;

namespace AxieDataFetcher.AxieObjects
{
    public class GeneData
    {
        public string[] genome;
        public string Class;
        public AxieGeneTrait[] TraitData = new AxieGeneTrait[6];

        public GeneData(string[] genes)
        {
            genome = genes;
            for (int i = 0; i < TraitData.Length; i++) TraitData[i] = new AxieGeneTrait();
        }

        public GeneData(string gene)
        {
            genome = GetSubGroups(calcBinary(gene));
            for (int i = 0; i < TraitData.Length; i++) TraitData[i] = new AxieGeneTrait();
        }

        public void GetDataFromGenome()
        {
            Class = GetClassFromBinary(genome[0].Substring(0, 4));
            for (int i = 0; i < TraitData.Length; i++)
            {
                TraitData[i].DominantClass = GetClassFromBinary(genome[i + 2].Substring(2, 4));
                TraitData[i].R1Class = GetClassFromBinary(genome[i + 2].Substring(12, 4));
                TraitData[i].R2Class = GetClassFromBinary(genome[i + 2].Substring(22, 4));
            }
        }

        private string GetClassFromBinary(string binary)
        {
            switch (binary)
            {
                case "0000":
                    return "beast";
                case "0001":
                    return "bug";
                case "0010":
                    return "bird";
                case "0011":
                    return "plant";
                case "0100":
                    return "aquatic";
                case "0101":
                    return "reptile";
                case "1000":
                    return "hidden_1";
                case "1001":
                    return "hidden_2";
                case "1010":
                    return "hidden_3";
            }
            return "";
        }
        public float GetTraitProbability(string desiredClass, int index)
        {
            float probability = 0;
            probability += TraitData[index].DominantClass == desiredClass ? 0.375f : 0; //0.35
            probability += TraitData[index].R1Class == desiredClass ? 0.09375f : 0;      //0.11
            probability += TraitData[index].R2Class == desiredClass ? 0.03125f : 0;      //0.04 from freak  234375f weird from trung
            return probability;
        }
        public string calcBinary(string gene)
        {
            BigInteger gene256 = BigInteger.Parse(gene);
            var bytes = gene256.ToByteArray();
            //var idx = bytes.Length - 1;
            var idx = 31;
            if (bytes.Length < 32) idx = bytes.Length - 1;
            if (bytes.Length >= 33) Console.WriteLine("33 elements in binary byte array!");
            // Create a StringBuilder having appropriate capacity.
            var base2 = new StringBuilder(32 * 8);

            // Convert first byte to binary.
            var binary = Convert.ToString(bytes[idx], 2).PadLeft(8, '0');

            if (bytes.Length < 32)
            {
                for (int i = 0; i < 32 - bytes.Length; i++) base2.Append("00000000");
            }


            // Ensure leading zero exists if value is positive.
            if (binary[0] != '0' && gene256.Sign == 1)
            {
                //base2.Append('0');
            }

            // Append binary string to StringBuilder.
            base2.Append(binary);

            // Convert remaining bytes adding leading zeros.
            for (idx--; idx >= 0; idx--)
            {
                base2.Append(Convert.ToString(bytes[idx], 2).PadLeft(8, '0'));
            }

            return base2.ToString();
        }

        public string[] GetSubGroups(string gene)
        {
            string[] geneArray = new string[8];
            int stringIndex = 0;
            for (int i = 0; i < geneArray.Length; i++)
            {
                StringBuilder subGeneGroup = new StringBuilder();
                for (int j = 0; j < 32; j++)
                {
                    subGeneGroup.Append(gene[stringIndex]);
                    stringIndex++;
                }
                geneArray[i] = subGeneGroup.ToString();
                subGeneGroup.Clear();
            }
            return geneArray;
        }
        //public bool ContainsTrait(TraitMap traitMap)
        //{
        //    var group = genome[traitMap.partGroup];
        //    for (int i = 0; i < 3; i++)
        //    {
        //        if (traitMap.classCode == group.Substring(2 + i * 10, 4) &&
        //            traitMap.traitCode == group.Substring(6 + i * 10, 5))
        //            return true;

        //    }
        //    return false;
        //}

        //public bool ContainsShape(ShapeMap shapeMap)
        //{
        //    var group = genome[shapeMap.partGroup];
        //    for (int i = 0; i < 3; i++)
        //    {
        //        if (shapeMap.shapeCode == group.Substring(2 + i * 6, 6))
        //            return true;
        //    }
        //    return false;
        //}
    }

    public class AxieGeneTrait
    {
        public string DominantClass;
        public string R1Class;
        public string R2Class;
    }

}
