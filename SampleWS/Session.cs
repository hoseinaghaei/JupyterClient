namespace SampleWS
{
    public class Session
    {
        public string id { get; set; }
        public string name { get; set; }
        public string path { get; set; }
        public string type { get; set; }
        public Kernel kernel { get; set; }
    }

    public class Kernel
    {
        public string id { get; set; }
        public string name { get; set; }
        public string last_activity { get; set; }
        public int connections { get; set; }
        public string execution_state { get; set; }
    }
}