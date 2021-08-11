using System;
using System.Collections.Generic;

namespace SampleWS
{
    public class CodeTransformer
    {
        public string hardCodeInit = @"";
        public string hardCodeFinal = @"";
        public List<string> _hardCodeRepeatPart { get; }= new();
        public int partitionSize = 500;

        public void GenerateHardCode(int rows)
        {
            hardCodeInit = @" public class SampleObject { 
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
    var dataTable = new List<SampleObject>();
     
";
            for (var partNum = 0; partNum < rows / partitionSize; partNum++)
            {
                var part = "";
                for (var i = partNum * partitionSize; i < rows && i < (partNum + 1) * partitionSize; i++)
                {
                    var line =
                        $"dataTable.Add(new SampleObject() {{row ={i} , id = Guid.NewGuid().ToString(), dt = DateTime.Now, _random = new Random()}});\n";
                    part += line;
                }

                _hardCodeRepeatPart.Add(part);
            }

            hardCodeFinal = $"display(dataTable[{rows - 1}]);\n" +
                            $"dataTable[{rows - 1}].row";
        }


        void cod(int rows)
        {
            var dataTable = new List<SampleObject>();
            for (var i = 0; i < rows; i++)
            {
                dataTable.Add(new SampleObject() {row = i, id = Guid.NewGuid().ToString(), dt = DateTime.Now});
            }
        }
    }

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