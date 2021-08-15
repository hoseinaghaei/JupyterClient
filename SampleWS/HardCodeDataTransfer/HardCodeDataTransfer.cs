using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SampleWS
{
    public class HardCodeDataTransfer : IDataTransfer
    {
        public string InitialPart { get; } = @"";
        public string FinalPart { get; } = @"";
        public List<string> RepeativePart { get; } = new();
        private readonly int _partitionSize = 1000;

        public HardCodeDataTransfer(int rows)
        {
            InitialPart = @"public class SampleObject { 
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
                            var dataTable = new List<SampleObject>();";
            for (var partNum = 0; partNum * _partitionSize < rows; partNum++)
            {
                var part = "";
                for (var i = partNum * _partitionSize; i < rows && i < (partNum + 1) * _partitionSize; i++)
                {
                    part +=
                        $"dataTable.Add(new SampleObject() {{row ={i} , id = Guid.NewGuid().ToString(), dt = DateTime.Now, _random = new Random()}});\n";
                }

                RepeativePart.Add(part);
            }

            FinalPart = $"display(dataTable[{rows - 1}]);\n" +
                        $"dataTable[{rows - 1}].row";
        }
    }
}