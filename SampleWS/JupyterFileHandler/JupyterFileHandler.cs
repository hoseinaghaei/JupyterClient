using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SampleWS.JupyterFileHandler
{
    //todo myHttpClient
    public class JupyterFileHandler : IJupyterFileHandler
    {
        private readonly IJupyterFileManager _fileManager;
        private readonly IJupyterFileSplitter _fileSplitter;

        private static readonly string JupyterDir = "jupyter_files";

        // private const string StaticSubDir = "static_files";
        private const string StaticSubDir = "output_files";
        private const string DynamicSubDir = "dynamic_files";
        private const string OutPutSubDir = "output_files";
        private readonly string _id;
        public ContentFormat Format { get; }


        private JupyterFileHandler(IJupyterFileManager fileManager, IJupyterFileSplitter fileSplitter, string id,
            ContentFormat format = default)
        {
            _fileSplitter = fileSplitter;
            _fileManager = fileManager;
            if (id is null || id.Equals(""))
                throw new ArgumentNullException(nameof(id));
            _id = id;
            Format = format;
        }

        public static async Task<JupyterFileHandler> CreateAsync(IJupyterFileManager fileManager,
            IJupyterFileSplitter fileSplitter,
            string id, ContentFormat format = default)
        {
            var fileHandler = new JupyterFileHandler(fileManager, fileSplitter, id, format);
            await fileHandler.MakeDirectoriesAsync();
            return fileHandler;
        }

        private async Task MakeDirectoriesAsync()
        {
            try
            {
                await _fileManager.CreateDirectoryAsync($"/{JupyterDir}");
            }
            catch (DirectoryExistException)
            {
            }

            await _fileManager.CreateDirectoryAsync($"/{JupyterDir}/{_id}");
            // await _fileManager.CreateDirectoryAsync($"/{JupyterDir}/{_id}/{StaticSubDir}");
            await _fileManager.CreateDirectoryAsync($"/{JupyterDir}/{_id}/{DynamicSubDir}");
            await _fileManager.CreateDirectoryAsync($"/{JupyterDir}/{_id}/{OutPutSubDir}");
        }

        public async Task<List<string>> SendStaticFileAsync(string fileName, string fileFormat, Stream content,
            ContentFormat format)
        {
            await _fileManager.CreateDirectoryAsync($"/{JupyterDir}/{_id}/{StaticSubDir}/{fileName}");
            var addressList = new List<string>();
            _fileSplitter.Split(content, format);
            while (_fileSplitter.HasNextPacket)
            {
                addressList.Add(await _fileManager.UploadFileAsync($"/{JupyterDir}/{_id}/{StaticSubDir}/{fileName}",
                    $"{_fileSplitter.NextPacketNum}.{fileFormat}", await _fileSplitter.GetSplit(), format));
            }

            return addressList;
        }

        public async Task<List<string>> SendStaticFileAsync(string fileName, string fileFormat, byte[] content,
            ContentFormat format)
        {
            await _fileManager.CreateDirectoryAsync($"/{JupyterDir}/{_id}/{StaticSubDir}/{fileName}");
            var addressList = new List<string>();
            _fileSplitter.Split(content, format);
            while (_fileSplitter.HasNextPacket)
            {
                addressList.Add(await _fileManager.UploadFileAsync($"/{JupyterDir}/{_id}/{StaticSubDir}/{fileName}",
                    $"{_fileSplitter.NextPacketNum}.{fileFormat}", await _fileSplitter.GetSplit(), format));
            }

            return addressList;
        }

        public async Task<List<string>> SendStaticFileAsync(string fileName, string fileFormat, Stream content)
        {
            return await SendStaticFileAsync(fileName, fileFormat, content, Format);
        }

        public async Task<List<string>> SendStaticFileAsync(string fileName, string fileFormat, byte[] content)
        {
            return await SendStaticFileAsync(fileName, fileFormat, content, Format);
        }

        public async Task<List<string>> SendDynamicFileAsync(string fileName, string fileFormat, Stream content,
            ContentFormat format)
        {
            await _fileManager.CreateDirectoryAsync($"/{JupyterDir}/{_id}/{DynamicSubDir}/{fileName}");
            var addressList = new List<string>();
            _fileSplitter.Split(content, format);
            while (_fileSplitter.HasNextPacket)
            {
                addressList.Add(await _fileManager.UploadFileAsync($"/{JupyterDir}/{_id}/{DynamicSubDir}/{fileName}",
                    $"{_fileSplitter.NextPacketNum}.{fileFormat}",
                    await _fileSplitter.GetSplit(), format));
            }

            return addressList;
        }

        public async Task<List<string>> SendDynamicFileAsync(string fileName, string fileFormat, byte[] content,
            ContentFormat format)
        {
            await _fileManager.CreateDirectoryAsync($"/{JupyterDir}/{_id}/{DynamicSubDir}/{fileName}");
            var addressList = new List<string>();
            _fileSplitter.Split(content, format);
            while (_fileSplitter.HasNextPacket)
            {
                addressList.Add(await _fileManager.UploadFileAsync($"/{JupyterDir}/{_id}/{DynamicSubDir}/{fileName}",
                    $"{_fileSplitter.NextPacketNum}.{fileFormat}",
                    await _fileSplitter.GetSplit(), format));
            }

            return addressList;
        }

        public async Task<List<string>> SendDynamicFileAsync(string fileName, string fileFormat, Stream content)
        {
            return await SendDynamicFileAsync(fileName, fileFormat, content, Format);
        }

        public async Task<List<string>> SendDynamicFileAsync(string fileName, string fileFormat, byte[] content)
        {
            return await SendDynamicFileAsync(fileName, fileFormat, content, Format);
        }

        public async Task<Stream> DownloadOutputFileAsync(string fileName,ContentFormat format)
        {
            var fileParts =
                await _fileManager.GetDirectoryAsync($"/{JupyterDir}/{_id}/{OutPutSubDir}/{fileName}");
            fileParts = fileParts.OrderBy(p => p.name);
            
            Stream stream = new MemoryStream();
            var streamWriter = new StreamWriter(stream);
            foreach (var filePart in fileParts)
            {
                var part = await _fileManager.DownloadFileAsync(
                    $"/{JupyterDir}/{_id}/{OutPutSubDir}/{fileName}", filePart.name, format);
                await streamWriter.WriteAsync(part.content);
            }
            await streamWriter.FlushAsync();
            return stream;
        }

        public async Task<Stream> DownloadOutputFileAsync(string fileName)
        {
            return await DownloadOutputFileAsync(fileName, Format);
        }

        public async Task<byte[]> DownloadOutputFileAsByteArrayAsync(string fileName, ContentFormat format)
        {
            var fileParts =
                await _fileManager.GetDirectoryAsync($"/{JupyterDir}/{_id}/{OutPutSubDir}/{fileName}");
            fileParts = fileParts.OrderBy(p => p.name);
            var bytesList = new List<byte>();
            foreach (var filePart in fileParts)
            {
                var part = await _fileManager.DownloadFileAsync(
                    $"/{JupyterDir}/{_id}/{OutPutSubDir}/{fileName}", filePart.name, format);
                bytesList.AddRange(Encoding.ASCII.GetBytes(part.content));
            }
            return bytesList.ToArray();
        }

        public  async Task<byte[]> DownloadOutputFileAsByteArrayAsync(string fileName)
        {
            return await DownloadOutputFileAsByteArrayAsync(fileName, Format);
        }


        public async Task<Dictionary<string, Stream>> DownloadOutputFilesAsync(ContentFormat format)
        {
            var fileDirectories = await _fileManager.GetDirectoryAsync($"/{JupyterDir}/{_id}/{OutPutSubDir}");
            var files = new Dictionary<string, Stream>();
            foreach (var fileDirectory in fileDirectories)
            {
                files.Add(fileDirectory.name, 
                    await DownloadOutputFileAsync(fileDirectory.name,format));
            }
            return files;
        }

        public async Task<Dictionary<string, Stream>> DownloadOutputFilesAsync()
        {
            return await DownloadOutputFilesAsync(Format);
        }

        public async Task<Dictionary<string, byte[]>> DownloadOutputFilesAsByteArrayAsync(ContentFormat format)
        {
            var fileDirectories = await _fileManager.GetDirectoryAsync($"/{JupyterDir}/{_id}/{OutPutSubDir}");
            var files = new Dictionary<string, byte[]>();
            foreach (var fileDirectory in fileDirectories)
            {
                files.Add(fileDirectory.name,
                    await DownloadOutputFileAsByteArrayAsync(fileDirectory.name,format));
            }
            return files;
        }

        public async Task<Dictionary<string, byte[]>> DownloadOutputFilesAsByteArrayAsync()
        {
            return await DownloadOutputFilesAsByteArrayAsync(Format);
        }
        
        public async Task DeleteNonStaticFilesAsync(string fileName, string content)
        {
            await _fileManager.DeleteDirectoryAsync($"/{JupyterDir}/{_id}/{DynamicSubDir}");
            await _fileManager.CreateDirectoryAsync($"/{JupyterDir}/{_id}/{DynamicSubDir}");
            await _fileManager.DeleteDirectoryAsync($"/{JupyterDir}/{_id}/{OutPutSubDir}");
            await _fileManager.CreateDirectoryAsync($"/{JupyterDir}/{_id}/{OutPutSubDir}");
        }

        public async ValueTask DisposeAsync()
        {
            await _fileManager.DeleteDirectoryAsync($"/{JupyterDir}/{_id}");
        }

        private (string,string) FormatDetector(string name)
        {
            var splits =name.Split('.');
            if (splits.Length <= 1) return (name, null);
            
            var i = name.LastIndexOf(splits[^1], StringComparison.Ordinal);
            return (name.Remove(i-1), splits[^1]);
            
        }
    }
}