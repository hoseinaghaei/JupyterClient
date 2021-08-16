using System;

namespace SampleWS
{
    public class DirectoryExistException:Exception
    {
    } 
    public class DirectoryNotFoundException:Exception
    {
    }
    public class FileNotFoundException:Exception
    {
    } 
    public class BadRequestException:Exception
    {
        public string message;
        public string reason;
    }
    public class ConflictException:Exception
    {
    }
    public class InternalServerException:Exception
    {
        public string message;
        public string reason;
    }
}