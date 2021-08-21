using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SampleWS
{
    public class JupyterFileManager : IJupyterFileManager
    {
        //path should start with: /
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

        public async Task CreateDirectoryAsync(string path)
        {
            if (await ExistDirectoryAsync(path))
                throw new DirectoryExistException();

            var message = RequestMessageMaker(HttpMethod.Put, BaseAddress + AdditionalAddress + path,
                "{\"type\":\"directory\"}");
            var response = await _client.SendAsync(message);
            switch (response.StatusCode)
            {
                case HttpStatusCode.Created : return;
                case HttpStatusCode.NotFound: throw new DirectoryNotFoundException();
                case HttpStatusCode.BadRequest:
                    throw (JsonConvert.DeserializeObject<BadRequestException>(
                        await response.Content.ReadAsStringAsync()) ?? new BadRequestException());
                case HttpStatusCode.InternalServerError when (await response.Content.ReadAsStringAsync()).Contains(
                    "The system cannot find the path specified"): throw new DirectoryNotFoundException();
                case HttpStatusCode.InternalServerError:
                    throw (JsonConvert.DeserializeObject<InternalServerException>(
                        await response.Content.ReadAsStringAsync()) ?? new InternalServerException());
                default:
                    throw new HttpRequestException($"Create Directory Exception{response.StatusCode}", null,
                        response.StatusCode);
            }
        }

        public async Task<string> UploadFileAsync(string path, string name, string content,
            ContentFormat ? format = ContentFormat .Text)
        {
            if (await ExistFileAsync(path, name))
                throw new ConflictException();
            
            switch (format)
            {
                case ContentFormat .Base64:
                    content = $"{{\"type\": \"file\",\"format\": \"base64\",\"content\": \"{content}\" }}";
                    break;
                case ContentFormat .Text:
                    content = $"{{\"type\": \"file\",\"format\": \"text\",\"content\": \"{content}\" }}";
                    break;
                case ContentFormat .Json:
                    content = $"{{\"type\": \"file\",\"format\": \"json\",\"content\": \"{content}\" }}";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }

            var message =
                RequestMessageMaker(HttpMethod.Put, BaseAddress + AdditionalAddress + path + "/" + name, content);
            var response = await _client.SendAsync(message);
              // Console.WriteLine($"\n>>>request <-> {await message.Content.ReadAsStringAsync()}\n");
            
              Console.WriteLine($"\n<<<{response.StatusCode} <-> {await response.Content.ReadAsStringAsync()}\n");
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    return $"{path}/{name}";
                //if multi threads work on same file, here may change content of a  file with content instead of throwing ConflictException()
                case HttpStatusCode.Created:
                    return $"{path}/{name}";
                case HttpStatusCode.BadRequest:
                    throw (JsonConvert.DeserializeObject<BadRequestException>(
                        await response.Content.ReadAsStringAsync()) ?? new BadRequestException());
                case HttpStatusCode.InternalServerError when (await response.Content.ReadAsStringAsync()).Contains(
                    "No such file or directory"): throw new DirectoryNotFoundException();
                case HttpStatusCode.InternalServerError:
                    throw (JsonConvert.DeserializeObject<InternalServerException>(
                        await response.Content.ReadAsStringAsync()) ?? new InternalServerException());
                default:
                    throw new HttpRequestException($"Create Directory Exception{response.StatusCode}", null,
                        response.StatusCode);
            }
        }

        public async Task<FileDetails> DownloadFileAsync(string path, string name,
            ContentFormat ? format = ContentFormat .Text)
        {
            var newPath = format == ContentFormat .Base64
                ? $"{BaseAddress}{AdditionalAddress}{path}/{name}?type=file&format=base64"
                : $"{BaseAddress}{AdditionalAddress}{path}/{name}?type=file&format=text";
            var message = RequestMessageMaker(HttpMethod.Get, newPath);
            var response = await _client.SendAsync(message);
            if (response.StatusCode != HttpStatusCode.OK)
                return response.StatusCode switch
                {
                    HttpStatusCode.NotFound => throw new FileNotFoundException(),
                    HttpStatusCode.BadRequest => throw (JsonConvert.DeserializeObject<BadRequestException>(
                        await response.Content.ReadAsStringAsync()) ?? new BadRequestException()),
                    HttpStatusCode.InternalServerError when (await response.Content.ReadAsStringAsync()).Contains(
                        "The system cannot find the path specified") => throw new DirectoryNotFoundException(),
                    HttpStatusCode.InternalServerError => throw (JsonConvert.DeserializeObject<InternalServerException>(
                        await response.Content.ReadAsStringAsync()) ?? new InternalServerException()),
                    _ => throw new HttpRequestException($"Create Directory Exception{response.StatusCode}", null,
                        response.StatusCode)
                };

            var file = JsonConvert.DeserializeObject<FileDetails>(await response.Content.ReadAsStringAsync());
            return file;
        }

        public async Task<IEnumerable<FileDetails>> GetDirectoryAsync(string path)
        {
            var message = RequestMessageMaker(HttpMethod.Get, BaseAddress + AdditionalAddress + path);
            var response = await _client.SendAsync(message);
            if (response.StatusCode != HttpStatusCode.OK)
                return response.StatusCode switch
                {
                    HttpStatusCode.NotFound => throw new DirectoryNotFoundException(),
                    HttpStatusCode.BadRequest => throw (JsonConvert.DeserializeObject<BadRequestException>(
                        await response.Content.ReadAsStringAsync()) ?? new BadRequestException()),
                    HttpStatusCode.InternalServerError when (await response.Content.ReadAsStringAsync()).Contains(
                        "The system cannot find the path specified") => throw new DirectoryNotFoundException(),
                    HttpStatusCode.InternalServerError => throw (JsonConvert.DeserializeObject<InternalServerException>(
                        await response.Content.ReadAsStringAsync()) ?? new InternalServerException()),
                    _ => throw new HttpRequestException($"Create Directory Exception{response.StatusCode}", null,
                        response.StatusCode)
                };

            var parsed = JObject.Parse(await response.Content.ReadAsStringAsync());
            var content = parsed["content"]?.ToString(Formatting.None);
            var deserializedObjectList = JsonConvert.DeserializeObject<IEnumerable<FileDetails>>(content ?? "");
            return deserializedObjectList;
        }

        public async Task EditFileAsync(string path, string name, string newContent,
            ContentFormat ? format = ContentFormat .Text)
        {
            if (!await ExistFileAsync(path, name))
                throw new FileNotFoundException();
            string stringContent;
            switch (format)
            {
                case ContentFormat .Base64:
                    stringContent = $"{{\"type\": \"file\",\"format\": \"base64\",\"content\": \"{newContent}\"}}";
                    break;
                case ContentFormat .Text:
                    stringContent = $"{{\"type\": \"file\",\"format\": \"text\",\"content\": \"{newContent}\"}}";
                    break;
                case ContentFormat .Json:
                    stringContent = $"{{\"type\": \"file\",\"format\": \"json\",\"content\": \"{newContent}\"}}";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }

            var message =
                RequestMessageMaker(HttpMethod.Put, BaseAddress + AdditionalAddress + path + "/" + name, stringContent);
            var response = await _client.SendAsync(message);


            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    return;
                case HttpStatusCode.Created:
                    return;
                //if multi threads are working on same file, here may create a new file with content instead of throwing FileNotFoundException()
                case HttpStatusCode.BadRequest:
                    throw (JsonConvert.DeserializeObject<BadRequestException>(
                        await response.Content.ReadAsStringAsync()) ?? new BadRequestException());
                case HttpStatusCode.InternalServerError when (await response.Content.ReadAsStringAsync()).Contains(
                    "No such file or directory"): throw new DirectoryNotFoundException();
                case HttpStatusCode.InternalServerError:
                    throw (JsonConvert.DeserializeObject<InternalServerException>(
                        await response.Content.ReadAsStringAsync()) ?? new InternalServerException());
                default:
                    throw new HttpRequestException($"Create Directory Exception{response.StatusCode}", null,
                        response.StatusCode);
            }
        }

        public async Task RenameFileAsync(string path, string name, string newName)
        {
            var message = RequestMessageMaker(HttpMethod.Patch, BaseAddress + AdditionalAddress + path + "/" + name,
                $"{{\"path\": \"{path}/{newName}\"}}");
            var response = await _client.SendAsync(message);


            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    return;
                case HttpStatusCode.Conflict:
                    throw new ConflictException();
                case HttpStatusCode.BadRequest:
                    throw JsonConvert.DeserializeObject<BadRequestException>(
                        await response.Content.ReadAsStringAsync()) ?? new BadRequestException();
                case HttpStatusCode.InternalServerError when (await response.Content.ReadAsStringAsync()).Contains(
                    "No such file or directory:"): throw new DirectoryNotFoundException();
                case HttpStatusCode.InternalServerError when (await response.Content.ReadAsStringAsync()).Contains(
                    "The system cannot find the file specified:"): throw new FileNotFoundException();
                case HttpStatusCode.InternalServerError:
                    throw (JsonConvert.DeserializeObject<InternalServerException>(
                        await response.Content.ReadAsStringAsync()) ?? new InternalServerException());
                default:
                    throw new HttpRequestException("Get Directory Exception", null, response.StatusCode);
            }
        }

        public async Task DeleteFileAsync(string path, string name)
        {
            name = name == null ? "" : "/" + name;
            var message = RequestMessageMaker(HttpMethod.Delete, BaseAddress + AdditionalAddress + path + name);
            var response = await _client.SendAsync(message);

            switch (response.StatusCode)
            {
                case HttpStatusCode.NoContent:
                    return;
                case HttpStatusCode.BadRequest:
                    throw JsonConvert.DeserializeObject<BadRequestException>(
                        await response.Content.ReadAsStringAsync()) ?? new BadRequestException();
                case HttpStatusCode.NotFound:
                case HttpStatusCode.InternalServerError when (await response.Content.ReadAsStringAsync()).Contains(
                    "File or directory does not exist:"): throw new DirectoryNotFoundException();
                case HttpStatusCode.InternalServerError when (await response.Content.ReadAsStringAsync()).Contains(
                    "The system cannot find the file specified:"): throw new FileNotFoundException();
                case HttpStatusCode.InternalServerError:
                    throw (JsonConvert.DeserializeObject<InternalServerException>(
                        await response.Content.ReadAsStringAsync()) ?? new InternalServerException());
                default:
                    throw new HttpRequestException("Delete Directory Exception", null, response.StatusCode);
            }
        }

        public async Task DeleteDirectoryAsync(string path)
        {
            await DeleteFileAsync(path, null);
        }

        #region Stream

        public async Task<bool> UploadFileAsStreamAsync(string path, string name, Stream content,
            ContentFormat ? format = ContentFormat .Text)
        {
            var message = RequestMessageMaker(HttpMethod.Post, BaseAddress + AdditionalAddress + path);
            var response = await _client.SendAsync(message);
            if (response.StatusCode != HttpStatusCode.Created)
                throw new HttpRequestException("Make New File Exception", null, response.StatusCode);

            var parsed = JObject.Parse(await response.Content.ReadAsStringAsync());
            var tempName = parsed["name"]?.ToString();

            await RenameFileAsync(path, tempName, name);

            var changeContentSuccess = await ChangeContentFileAsStreamAsync(path, name, content, format);
            return changeContentSuccess;
        }

        public async Task<bool> ChangeContentFileAsStreamAsync(string path, string name, Stream newContent,
            ContentFormat ? format = ContentFormat .Text)
        {
            /* Stream streamContent;
             newContent.
             switch (format)
             {
                 case Format .base64:
                     streamContent = $"{{\"type\": \"file\",\"format\": \"base64\",\"content\": \"{newContent}\"}}";
                     break;
                 case Format .text:
                     streamContent= $"{{\"type\": \"file\",\"format\": \"text\",\"content\": \"{newContent}\"}}";
                     break;
                 case Format .json:
                     streamContent = $"{{\"type\": \"file\",\"format\": \"json\",\"content\": \"{newContent}\"}}";
                     break;
                 case null:
                 default:
                     throw new ArgumentOutOfRangeException(nameof(format), format, null);
             }
 
             var message =
                 RequestMessageAsStreamMaker(HttpMethod.Put, BaseAddress + AdditionalAddress + path + "/" + name, streamContent);
             var response = await _client.SendAsync(message);
             if (response.StatusCode == HttpStatusCode.OK)
                 return true;
 
             throw new HttpRequestException("Change Content Of File Exception", null, response.StatusCode);*/
            return false;
        }

        public async Task<Stream> DownloadFileAsStreamAsync(string path, string name,
            ContentFormat ? format = ContentFormat .Text)
        {
            var newPath = format == ContentFormat .Base64
                ? $"{BaseAddress}{AdditionalAddress}{path}/{name}?type=file&format=base64"
                : $"{BaseAddress}{AdditionalAddress}{path}/{name}?type=file&format=text";
            var message = RequestMessageMaker(HttpMethod.Get, newPath);
            var response = await _client.SendAsync(message);
            if (response.StatusCode != HttpStatusCode.OK)
                throw new HttpRequestException("Get File Exception", null, response.StatusCode);

            var content = await response.Content.ReadAsStreamAsync();
            return content;
        }

        private HttpRequestMessage RequestMessageAsStreamMaker(HttpMethod method, string uri,
            Stream streamContent = null)
        {
            var message = new HttpRequestMessage(method, uri);
            message.Headers.Add(XsrfHeaderKey, XsrfToken);
            if (streamContent is not null)
            {
                message.Content = new StreamContent(streamContent);
            }

            return message;
        }

        #endregion

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

        private async Task<bool> ExistAsync(string path, string name, bool isFile)
        {
            var stringContent = isFile
                ? $"{BaseAddress}{AdditionalAddress}{path}/{name}?type=file&content=0"
                : $"{BaseAddress}{AdditionalAddress}{path}?type=directory&content=0";
            var message = RequestMessageMaker(HttpMethod.Get, stringContent);
            var response = await _client.SendAsync(message);
            return response.StatusCode switch
            {
                HttpStatusCode.OK => true,
                HttpStatusCode.NotFound => false,
                HttpStatusCode.BadRequest => throw (JsonConvert.DeserializeObject<BadRequestException>(
                    await response.Content.ReadAsStringAsync()) ?? new BadRequestException()),
                HttpStatusCode.InternalServerError when (await response.Content.ReadAsStringAsync()).Contains(
                    "The system cannot find the path specified") => false,
                HttpStatusCode.InternalServerError => throw (JsonConvert.DeserializeObject<InternalServerException>(
                    await response.Content.ReadAsStringAsync()) ?? new InternalServerException()),
                _ => throw new HttpRequestException($"Exist Directory Exception{response.StatusCode}", null,
                    response.StatusCode)
            };
        }
    }
}

//todo delete: Console.WriteLine($"\n>>>{response.StatusCode} <-> {await response.Content.ReadAsStringAsync()}\n");
//todo return static dir name to static from output
//todo download file? stream?