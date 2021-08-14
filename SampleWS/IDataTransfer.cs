using System.Collections.Generic;

namespace SampleWS
{
    public interface IDataTransfer
    {
        public List<string> RepeativePart { get; }
        public string InitialPart {get;}
        public string FinalPart {get;}
    }
}