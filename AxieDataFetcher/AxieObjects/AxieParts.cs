using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AxieDataFetcher.AxieObjects
{
    //support classes


    public class AxieParts
    {
        public AxiePart tail;
        public AxiePart back;
        public AxiePart horn;
        public AxiePart ears;
        public AxiePart mouth;
        public AxiePart eyes;
    }
    public class AxiePart
    {
        public string id;
        public string name;
        public string Class;
        public string type;
        public bool mystic;
        public PartMove[] moves;
    }
    public class PartMove
    {
        public string id;
        public string name;
        public string type;
        public int attack;
        public int defense;
        public int accuracy;
        public PartEffect[] effects;

    }
    public class PartEffect
    {
        public string name;
        public string type;
        public string description;

    }
    public class AxieStats
    {
        public int hp;
        public int speed;
        public int skill;
        public int morale;
    }
    public class AxieFigure
    {
        public string atlas;
        public AxieImage images;
        public string model;
    }
    public class AxieAuction
    {
        public string type;
        public BigInteger startingPrice;
        public BigInteger endingPrice;
        public BigInteger buyNowPrice;
        public BigInteger suggestedPrice;
        public long startingTime;
        public long duration;
        public long timeLeft;
    }
    public class AxieImage
    {
        [JsonProperty(PropertyName = "")]
        public string png;
    }

}
