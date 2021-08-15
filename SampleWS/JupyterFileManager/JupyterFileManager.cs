using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SampleWS
{
    public class JupyterFileManager : IJupyterFileManager
    {
        //path starts with /...
        public string BaseAddress { get; }
        public string XsrfToken { get; }
        public Cookie LoginCookie { get; }
        public Cookie XsrfCookie { get; }

        private const string XsrfHeaderKey = "X-XSRFToken";
        private const string AdditionalAddress = "/api/contents";
        private readonly CookieContainer _cookies = new();
        private readonly HttpClient _client;

        public JupyterFileManager(string baseAddress, string xsrfToken, Cookie loginCookie, Cookie xsrfCookie)
        {
            BaseAddress = baseAddress;
            XsrfToken = xsrfToken;
            LoginCookie = loginCookie;
            XsrfCookie = xsrfCookie;
            _cookies.Add(LoginCookie);
            _cookies.Add(XsrfCookie);
            var handler = new HttpClientHandler {CookieContainer = _cookies};
            _client = new HttpClient(handler);
        }

        public async Task<bool> ExistFileAsync(string path, string name)
        {
            return await ExistAsync(path, name, true);
        }
        public async Task<bool> ExistDirectoryAsync(string path)
        {
            return await ExistAsync(path, null, false);
        }

        public async Task<bool> CreateDirectoryAsync(string path)
        {
            var message = RequestMessageMaker(HttpMethod.Put, BaseAddress + AdditionalAddress + path,"{\"type\":\"directory\"}");
            var response = await _client.SendAsync(message);
            var t = 5;
            if (response.StatusCode == HttpStatusCode.Created)
                return true;

            throw new HttpRequestException("Create Directory Exception", null, response.StatusCode);
        }

        public async Task<bool> UploadFileAsync(string path, string name, string content, bool? isBase64 = false)
        {
            var message = RequestMessageMaker(HttpMethod.Post, BaseAddress + AdditionalAddress + path);
            var response = await _client.SendAsync(message);
            if (response.StatusCode != HttpStatusCode.Created)
                throw new HttpRequestException("Make New File Exception", null, response.StatusCode);

            var parsed = JObject.Parse(await response.Content.ReadAsStringAsync());
            var tempName = parsed["name"]?.ToString();

            var renameSuccess = await RenameFileAsync(path, tempName, name);
            if (!renameSuccess) return false;

            var changeContentSuccess = await ChangeContentFileAsync(path, name, content, isBase64);
            return changeContentSuccess;
        }

        public async Task<FileDetails> DownloadFileAsync(string path, string name, bool? isBase64 = false)
        {
            var newPath = isBase64 == true
                ? $"{BaseAddress}{AdditionalAddress}{path}/{name}?type=file&format=base64"
                : $"{BaseAddress}{AdditionalAddress}{path}/{name}?type=file&format=text";
            var message = RequestMessageMaker(HttpMethod.Get, newPath);
            var response = await _client.SendAsync(message);
            if (response.StatusCode != HttpStatusCode.OK)
                throw new HttpRequestException("Get File Exception", null, response.StatusCode);

            var file = JsonConvert.DeserializeObject<FileDetails>(await response.Content.ReadAsStringAsync());
            return file;
        }

        public async Task<IEnumerable<FileDetails>> GetDirectoryAsync(string path)
        {
            var message = RequestMessageMaker(HttpMethod.Get, BaseAddress + AdditionalAddress + path);
            var response = await _client.SendAsync(message);
            if (response.StatusCode != HttpStatusCode.OK)
                throw new HttpRequestException("Get Directory Exception", null, response.StatusCode);

            var parsed = JObject.Parse(await response.Content.ReadAsStringAsync());
            var content = parsed["content"]?.ToString(Formatting.None);
            var deserializedObjectList = JsonConvert.DeserializeObject<IEnumerable<FileDetails>>(content ?? "");
            return deserializedObjectList;
        }

        public async Task<bool> ChangeContentFileAsync(string path, string name, string newContent,
            bool? isBase64 = false)
        {
            var stringContent = isBase64 == true
                ? $"{{\"type\": \"file\",\"format\": \"base64\",\"content\": \"{newContent}\"}}"
                : $"{{\"type\": \"file\",\"format\": \"text\",\"content\": \"{newContent}\"}}";

            var message =
                RequestMessageMaker(HttpMethod.Put, BaseAddress + AdditionalAddress + path + "/" + name, stringContent);
            var response = await _client.SendAsync(message);
            if (response.StatusCode == HttpStatusCode.OK)
                return true;

            throw new HttpRequestException("Change Content Of File Exception", null, response.StatusCode);

        }

        public async Task<bool> RenameFileAsync(string path, string name, string newName)
        {
            var message = RequestMessageMaker(HttpMethod.Patch, BaseAddress + AdditionalAddress + path + "/" + name,
                $"{{\"path\": \"{path}/{newName}\"}}");
            var response = await _client.SendAsync(message);
            if (response.StatusCode == HttpStatusCode.OK)
                return true;
            if (response.StatusCode == HttpStatusCode.Conflict)
                throw new HttpRequestException("Http Conflict Error", null, response.StatusCode);
            
            throw new HttpRequestException("Get Directory Exception", null, response.StatusCode);
        }

        public async Task<bool> DeleteFileAsync(string path, string name)
        {
            name = name == null ? "" : "/" + name;
            var message = RequestMessageMaker(HttpMethod.Delete, BaseAddress + AdditionalAddress + path + name);
            var response = await _client.SendAsync(message);
            if (response.StatusCode == HttpStatusCode.NoContent)
                return true;

            throw new HttpRequestException("Delete File Exception", null, response.StatusCode);
        }

        public async Task<bool> DeleteDirectoryAsync(string path)
        {
            return await DeleteFileAsync(path, null);
        }

        private HttpRequestMessage RequestMessageMaker(HttpMethod method, string uri, string stringContent = null)
        {
            var message = new HttpRequestMessage(method, uri);
            message.Headers.Add(XsrfHeaderKey, XsrfToken);
            if (stringContent is not null)
            {
                message.Content = new StringContent(stringContent);
            }
            return message;
        }

        private async Task<bool> ExistAsync(string path,string name,bool isFile)
        {
            var stringContent = isFile
                ? $"{BaseAddress}{AdditionalAddress}{path}/{name}?type=file&content=0"
                : $"{BaseAddress}{AdditionalAddress}{path}?type=directory&content=0";
            var message = RequestMessageMaker(HttpMethod.Get,stringContent);
            var response = await _client.SendAsync(message);
            return response.StatusCode switch
            {
                HttpStatusCode.OK => true,
                HttpStatusCode.NotFound => false,
                _ => throw new HttpRequestException("Get File Exception", null, response.StatusCode)
            };
        }
    }
}

//todo directories
//todo errors ? throw or false