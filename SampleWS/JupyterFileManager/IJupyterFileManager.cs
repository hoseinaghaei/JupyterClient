using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;

namespace SampleWS
{
    public interface IJupyterFileManager
    {
        public string BaseAddress { get; }
        public string XsrfToken { get; }
        public Cookie LoginCookie { get; }
        public Cookie XsrfCookie { get; }

        Task<IEnumerable<FileDetails>> GetDirectoryAsync(string path);
        Task<FileDetails> DownloadFileAsync(string path,string name, bool? isBase64 = false);
        Task<bool> ExistFileAsync(string path,string name);
        Task<bool> ExistDirectoryAsync(string path);
        Task<bool> CreateDirectoryAsync(string path);
        Task<bool> UploadFileAsync(string path,string name,string content, bool? isBase64 = false);
        Task<bool> RenameFileAsync(string path,string name,string newName);
        Task<bool> ChangeContentFileAsync(string path,string name,string newContent, bool? isBase64 = false);
        Task<bool> DeleteFileAsync(string path,string name);
        Task<bool> DeleteDirectoryAsync(string path);
    }
}