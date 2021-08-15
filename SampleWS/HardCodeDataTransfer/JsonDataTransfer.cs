using System;
using System.Collections.Generic;
using System.Data;
using Newtonsoft.Json;

namespace SampleWS
{
    public class JsonDataTransfer : IDataTransfer
    {
        public string InitialPart { get; }= @"";
        public string FinalPart { get; }= @"";
        public List<string> RepeativePart { get; }= new();
        
        private readonly int _partitionSize = 10000;

        private List<SampleObject> dataTable = null;

        public JsonDataTransfer(int rows)
        {
            code(rows);
            InitialPart = @"
                                using Newtonsoft.Json;
                                public class SampleObject { 
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

            for (var partNum = 0; partNum < rows ; partNum+=_partitionSize)
            {
                var json = JsonConvert.SerializeObject(dataTable.GetRange(partNum,dataTable.Count-partNum>_partitionSize?_partitionSize:dataTable.Count-partNum));
                json = json.Replace('"', '\'');
                RepeativePart.Add( $"dataTable.AddRange(JsonConvert.DeserializeObject<List<SampleObject>>(\"{json}\"));");
            }
            
            FinalPart = $"display(dataTable[{rows - 1}]);\n" +
                            $"dataTable[{rows - 1}].row";
        }

        private void code(int rows)
        {
            dataTable = new List<SampleObject>();
            for (var i = 0; i < rows; i++)
            {
                dataTable.Add(new SampleObject() {row = i, id = Guid.NewGuid().ToString(), dt = DateTime.Now});
            }
        }

    }
}