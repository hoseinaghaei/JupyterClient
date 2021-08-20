using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SampleWS.JupyterFileHandler
{
    public class JupyterFileSplitter : IJupyterFileSplitter
    {
        public int PacketSize { get; }

        public int StreamReaderByteArraySize { get; }

        //packetSize % streamReaderByteArraySize should be 0;
        public int NextPacketNum { get; private set; }
        public long TotalPackets { get; private set; }
        public bool HasNextPacket => NextPacketNum < TotalPackets;
        public ContentFormat Format { get; private set; }

        private int _index;
        private Stream _stream;

        public JupyterFileSplitter(int packetSize, int streamReaderByteArraySize)
        {
            if (streamReaderByteArraySize <= 0)
                throw new ArgumentException(nameof(streamReaderByteArraySize) + "should be greater than 0");
            if (streamReaderByteArraySize > packetSize || packetSize % streamReaderByteArraySize != 0)
                throw new ArgumentException(nameof(packetSize) + " % " + nameof(streamReaderByteArraySize) +
                                            " should be 0");
            PacketSize = packetSize;
            StreamReaderByteArraySize = streamReaderByteArraySize;
        }

        public JupyterFileSplitter()
        {
            PacketSize = 100_000_000;
            StreamReaderByteArraySize = 10000;
        }


        public void Split(Stream stream, ContentFormat format)
        {
            _stream = stream;
            if (_stream.Length == 0)
                NextPacketNum = -1;
            NextPacketNum = 0;
            _index = 0;
            TotalPackets = (_stream.Length / PacketSize) + (_stream.Length % PacketSize == 0 ? 0 : 1);
            Format = format;
        }

        public async Task<string> GetSplit()
        {
            if (!HasNextPacket)
                throw new EndOfStreamException();

            var bytes = new byte[StreamReaderByteArraySize];
            var packet = new byte[PacketSize];
            var destCursor = 0;

            for (var i = _index; i < _stream.Length; i += bytes.Length)
            {
                var len = (int) ((_stream.Length - i) > bytes.Length ? bytes.Length : (_stream.Length - i));

                await _stream.ReadAsync(bytes, 0, (int) len);

                Array.Copy(bytes, 0, packet, destCursor, (int) len);
                destCursor += len;

                if (len < bytes.Length)
                {
                    var splitString =
                        ByteToString(packet, 0, destCursor);
                    NextPacketNum++;
                    _index = i + bytes.Length;
                    return splitString;
                }

                if (destCursor >= packet.Length)
                {
                    var splitString =
                        ByteToString(packet, 0, packet.Length);
                    NextPacketNum++;
                    _index = i + bytes.Length;
                    return splitString;
                }
            }

            if (destCursor != 0)
            {
                var splitString = ByteToString(packet, 0, destCursor);
                NextPacketNum++;
                return splitString;
            }

            throw new Exception("error");
        }


        public Task Split(byte[] byteArray)
        {
            throw new System.NotImplementedException();
        }


        private string ByteToString(byte[] bytes, int index, int count)
        {
            return Format switch
            {
                ContentFormat.Base64 => Convert.ToBase64String(bytes, index, count),
                ContentFormat.Text => Encoding.Default.GetString(bytes, index, count),
                _ => throw new ArgumentOutOfRangeException(nameof(Format))
            };
        }
    }
}