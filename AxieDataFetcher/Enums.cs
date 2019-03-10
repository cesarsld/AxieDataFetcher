using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxieDataFetcher
{
    public enum PartType
    {
        mouth,
        ears,
        eyes,
        back,
        tail,
        horn,
        shape,
        colour
    }

    public enum AxieTag
    {
        Untagged,
        Origin,
        MEO,
        MEO2,
        Agamogenesis,
    }

    public enum SaleType
    {
        Tag,
        MysticCount,
        BodyType,
        Part
    }
}
