using System;

namespace SampleWS
{
    public class BadRequestException:Exception
    {
        public string message;
        public string reason;
    }
}