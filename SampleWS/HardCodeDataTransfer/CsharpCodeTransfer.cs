using SampleWS.JupyterFileHandler;

namespace SampleWS
{
    public class CsharpCodeTransfer
    {
        private IJupyterFileHandler _jupyterFileHandler;
        public string Code { get; private set; }

        public CsharpCodeTransfer(IJupyterFileHandler jupyterFileHandler)
        {
            _jupyterFileHandler = jupyterFileHandler;
        }

        // 
        public string Transfer(string code,string data)
        {
            Code = code;
            return Code;
        }
    }
}