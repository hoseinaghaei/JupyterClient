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
        Task<IEnumerable<FileDetails>> GetDirectoryAsync(string path);
        Task<FileDetails> DownloadFileAsync(string path,string name, ContentFormat? format = ContentFormat.Text);
        Task<Stream> DownloadFileAsStreamAsync(string path, string name,
            ContentFormat? format = ContentFormat.Text);
        Task<bool> ExistFileAsync(string path,string name);
        Task<bool> ExistDirectoryAsync(string path);
        Task CreateDirectoryAsync(string path);
        Task<string> UploadFileAsync(string path,string name,string content, ContentFormat? format = ContentFormat.Text);
        Task RenameFileAsync(string path,string name,string newName);
        Task EditFileAsync(string path,string name,string newContent,ContentFormat? format = ContentFormat.Text);
        Task DeleteFileAsync(string path,string name);
        Task DeleteDirectoryAsync(string path);
    }
}