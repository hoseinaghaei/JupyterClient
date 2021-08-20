using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;


namespace SampleWS.JupyterFileHandler
{
    //todo myHttpClient
    public class JupyterFileHandler : IJupyterFileHandler, IAsyncDisposable
    {
        private readonly IJupyterFileManager _fileManager;
        private readonly IJupyterFileSplitter _fileSplitter;

        private static readonly string JupyterDir = "jupyter_files";

        private const string StaticSubDir = "static_files";
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
            await _fileManager.CreateDirectoryAsync($"/{JupyterDir}/{_id}/{StaticSubDir}");
            await _fileManager.CreateDirectoryAsync($"/{JupyterDir}/{_id}/{DynamicSubDir}");
            await _fileManager.CreateDirectoryAsync($"/{JupyterDir}/{_id}/{OutPutSubDir}");
        }

        public async Task<List<string>> SendStaticFilesAsync(string fileName, Stream content, ContentFormat format)
        {
            var addressList = new List<string>();
            _fileSplitter.Split(content, format);
            while (_fileSplitter.HasNextPacket)
            {
                addressList.Add(await _fileManager.UploadFileAsync($"/{JupyterDir}/{_id}/{StaticSubDir}",
                    $"P{_fileSplitter.NextPacketNum}_{fileName}", await _fileSplitter.GetSplit(), format));
            }
            return addressList;
        }

        public async Task<List<string>>  SendStaticFilesAsync(string fileName, Stream content)
        {
            return await SendStaticFilesAsync(fileName, content, Format);
        }

        public async Task SendDynamicFilesAsync(string fileName, Stream content, ContentFormat format)
        {
            var addressList = new List<string>();
            _fileSplitter.Split(content, format);
            while (_fileSplitter.HasNextPacket)
            {
                addressList.Add( await _fileManager.UploadFileAsync($"/{JupyterDir}/{_id}/{DynamicSubDir}", fileName, content, format));
                ;
            }
            return addressList;
            await _fileManager.UploadFileAsync($"/{JupyterDir}/{_id}/{DynamicSubDir}", fileName, content, format);
        }

        public async Task SendDynamicFilesAsync(string fileName, string content)
        {
            await _fileManager.UploadFileAsync($"/{JupyterDir}/{_id}/{DynamicSubDir}", fileName, content, Format);
        }

        public async Task<IEnumerable<FileDetails>> DownloadOutputFilesAsync(string fileName, string content)
        {
            var filesDetailsEnumerable = await _fileManager.GetDirectoryAsync($"/{JupyterDir}/{_id}/{OutPutSubDir}");
            var files = new List<FileDetails>();
            foreach (var file in files)
            {
                files.Add(
                    await _fileManager.DownloadFileAsync($"/{JupyterDir}/{_id}/{OutPutSubDir}", file.name, Format));
            }

            return files;
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
    }
}