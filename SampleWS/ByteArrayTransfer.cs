using System;
using System.Collections.Generic;

namespace SampleWS
{
    public class ByteArrayTransfer:IDataTransfer
    {
        
     
        public string InitialPart  { get; }= @"";
        public string FinalPart { get; }= @"";
        public List<string> RepeativePart { get; }= new();
        private readonly int _partitionSize = 100_000;

        private static byte[] _bytes;

        public ByteArrayTransfer(int rows)
        {
            MakeArray(_partitionSize);
            
            var arrStr =String.Join(",", _bytes);
            InitialPart = $"var byteArray = new List<byte>();\n";
            
            for (var partNum = 0; partNum * _partitionSize< rows ; partNum++)
            {
                var part =  $"byteArray.AddRange( new byte[]{{{arrStr}}});\n " ;
                RepeativePart.Add(part);
            }
            
            FinalPart = $"display(byteArray[{rows - 1}]);\n";
        }

        private static void MakeArray(int len){
            _bytes= new byte[len];
            for (var i = 0; i < _bytes.Length; i++)
            {
                _bytes[i] = 10;
            }
        }
    }
    
    
}