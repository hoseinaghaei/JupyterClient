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
        private byte[] _bytes;

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
            _bytes = null;
        }

        public void Split(byte[] byteArray, ContentFormat format)
        {
            _bytes = byteArray;
            if (byteArray.Length == 0)
                NextPacketNum = -1;
            NextPacketNum = 0;
            _index = 0;
            TotalPackets = (_bytes.Length / PacketSize) + (_bytes.Length % PacketSize == 0 ? 0 : 1);
            Format = format;
            _stream = null;
        }


        public async Task<string> GetSplit()
        {
            if (!HasNextPacket)
                throw new EndOfStreamException();
            if (_stream is null && _bytes is null)
                throw new Exception("byte[] and stream are Null");

            if (_bytes is null)
                return await GetStreamSplitAsync();
            if (_stream is null)
                return await GetByteArraySplitAsync();

            throw new Exception("bug: byte[] and stream are Not Null");
        }

        private async Task<string> GetByteArraySplitAsync()
        {
            var packet = new byte[PacketSize];
            var len = (_bytes.Length - (NextPacketNum * PacketSize) > PacketSize)
                ? PacketSize
                : _bytes.Length - (NextPacketNum * PacketSize);
            Array.Copy(_bytes, NextPacketNum * PacketSize, packet, 0, len);
            NextPacketNum++;
            return ByteToString(packet, 0, len);
        }

        private async Task<string> GetStreamSplitAsync()
        {
            var bytes = new byte[StreamReaderByteArraySize];
            var packet = new byte[PacketSize];
            var destCursor = 0;
            for (var i = _index; i < _stream.Length; i += bytes.Length)
            {
                var len = (int) ((_stream.Length - i) > bytes.Length ? bytes.Length : (_stream.Length - i));
                await _stream.ReadAsync(bytes, 0, len);
                Array.Copy(bytes, 0, packet, destCursor, len);
                destCursor += len;
                if (len < bytes.Length)
                {
                    NextPacketNum++;
                    _index = i + bytes.Length;
                    return ByteToString(packet, 0, destCursor);
                }

                if (destCursor >= packet.Length)
                {
                    NextPacketNum++;
                    _index = i + bytes.Length;
                    return ByteToString(packet, 0, packet.Length);
                }
            }

            if (destCursor != 0)
            {
                NextPacketNum++;
                return ByteToString(packet, 0, destCursor);
            }

            throw new Exception("error");
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