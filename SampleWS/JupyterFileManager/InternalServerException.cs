using System;

namespace SampleWS
{
    public class InternalServerException:Exception
    {
        public string message;
        public string reason;
    }
}