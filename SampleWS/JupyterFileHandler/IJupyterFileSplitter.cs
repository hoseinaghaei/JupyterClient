using System;
using System.IO;
using System.Threading.Tasks;

namespace SampleWS.JupyterFileHandler
{
    public interface IJupyterFileSplitter
    {
        public int NextPacketNum { get; }
        public long TotalPackets { get; }
        public bool HasNextPacket { get; }

        void Split(Stream stream,ContentFormat format);
        Task<string> GetSplit();
        Task Split(byte[] byteArray);
    }
}