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
        Task<FileDetails> DownloadFileAsync(string path,string name, Format.DownloadFormat? format = Format.DownloadFormat.text);
        Task<Stream> DownloadFileAsStreamAsync(string path, string name,
            Format.DownloadFormat? format = Format.DownloadFormat.text);
        Task<bool> ExistFileAsync(string path,string name);
        Task<bool> ExistDirectoryAsync(string path);
        Task<bool> CreateDirectoryAsync(string path);
        Task UploadFileAsync(string path,string name,string content, Format.UploadFormat? format = Format.UploadFormat.text);
        Task RenameFileAsync(string path,string name,string newName);
        Task ChangeContentFileAsync(string path,string name,string newContent,Format.UploadFormat? format = Format.UploadFormat.text);
        Task DeleteFileAsync(string path,string name);
        Task DeleteDirectoryAsync(string path);
    }
}