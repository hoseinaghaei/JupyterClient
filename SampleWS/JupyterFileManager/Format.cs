using System;

namespace SampleWS
{
    public class Format
    {
        public enum UploadFormat
        {
            base64,
            text,
            json
        };
        public enum DownloadFormat
        {
            base64,
            text
        };
    }
}