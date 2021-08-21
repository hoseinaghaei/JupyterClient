using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SampleWS.JupyterFileHandler
{
    public interface IJupyterFileHandler : IAsyncDisposable
    {
        ContentFormat Format { get; }

        Task<List<string>> SendStaticFileAsync(string fileName, string fileFormat, Stream content, ContentFormat format);
        Task<List<string>> SendStaticFileAsync(string fileName, string fileFormat, byte[] content, ContentFormat format);
        Task<List<string>> SendStaticFileAsync(string fileName, string fileFormat, Stream content);
        Task<List<string>> SendStaticFileAsync(string fileName, string fileFormat, byte[] content);

        Task<List<string>> SendDynamicFileAsync(string fileName, string fileFormat, Stream content, ContentFormat format);
        Task<List<string>> SendDynamicFileAsync(string fileName, string fileFormat, byte[] content, ContentFormat format);
        Task<List<string>> SendDynamicFileAsync(string fileName, string fileFormat, Stream content);
        Task<List<string>> SendDynamicFileAsync(string fileName, string fileFormat, byte[] content);
        
        Task<Stream> DownloadOutputFileAsync(string fileName,ContentFormat format);
        Task<Stream> DownloadOutputFileAsync(string fileName);
        Task<byte[]> DownloadOutputFileAsByteArrayAsync(string fileName,ContentFormat format);
        Task<byte[]> DownloadOutputFileAsByteArrayAsync(string fileName);
        Task<Dictionary<string, Stream>> DownloadOutputFilesAsync(ContentFormat format);
        Task<Dictionary<string, Stream>> DownloadOutputFilesAsync();
        Task<Dictionary<string, byte[]>> DownloadOutputFilesAsByteArrayAsync(ContentFormat format);
        Task<Dictionary<string, byte[]>> DownloadOutputFilesAsByteArrayAsync();
        Task DeleteNonStaticFilesAsync(string fileName, string content);
        ValueTask DisposeAsync();
    }
}