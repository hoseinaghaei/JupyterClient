using System;

namespace SampleWS
{
    public struct FileDetails
    {
        public string name;
        public string path;
        public string type;
        public bool? writable;
        public DateTime? created;
        public DateTime? last_modified;
        public int? size;
        public string mimetype;
        public string content;
        public string format;
    }
}