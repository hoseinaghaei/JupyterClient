using System;

namespace SampleWS
{
    public class SampleObject
    {
        public int row;
        public string id;
        public Double x;
        public string xx;
        public DateTime dt;
        public Random _random;


        public SampleObject()
        {
            _random = new Random();
            x = _random.NextDouble();
            xx = x.GetHashCode().ToString();
        }
    }
}