using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SampleWS;
using SampleWS.JupyterFileHandler;

namespace SampleWS
{
    class Program
    {
        private static string TargetName => "my_target4";
        private static string CSharpKernel => ".net-csharp";
        private static string PythonKernel => "python3";
        private static string RKernel => "ir";

        private const string XsrfHeaderKey = "X-XSRFToken";
        private const string XsrfCookieKey = "_xsrf";
        private static string XsrfToken = "2|18c66a09|7a13bf5ddbdce3960d3cba4f8780ca0a|1626098682";
        private const string LoginCookieKey = "username-localhost-8888";

        private static string LoginCookieValue =
            "2|1:0|10:1627387906|23:username-localhost-8888|44:MjZiMmMxNTgwNDlkNDRmNTg3YTIxZGQ2NDgzNmNmMzc=|2ad40c3950582499e850824e7c7e851852dfb3e7dcd81c2592f691e5ef0ae6e9";

        private const string Password = "m.jupyter";


        private static readonly string CommId = Guid.NewGuid().ToString();

        static async Task Main(string[] args)
        {
            var cookies = await Login();
            const string nameVar = "big1_";

            IJupyterFileManager fileManager =
                new JupyterFileManager("http://localhost:8888", XsrfToken, cookies[0], cookies[1]);
            JupyterFileHandler.JupyterFileHandler fh =await JupyterFileHandler.JupyterFileHandler.
                CreateAsync(fileManager, new JupyterFileSplitter(),"datatexdt");
            
            var fileStream = new FileStream("D:\\Uni\\DataMining\\Hamiz\\tiny.csv", FileMode.Open);

            var t =await fh.SendStaticFilesAsync("testfile.csv",fileStream, ContentFormat.Text);
            
            // await fh.CreateDirectoryAsync(path);
            // await fh.DeleteFileAsync(path, "data2.csv");
            return;

            await fileManager.UploadFileAsync("", nameVar + "testfile.txt", BaseConverter.Base64Encode("\"Hello\n World! !!!\""),
                ContentFormat.Base64);
            await fileManager.RenameFileAsync("", nameVar + "utestfile.txt", "test.txt");
            // var file =await fh.DownloadFileAsync("",nameVar+"testfile.txt",Format.DownloadFormat.base64);

            // var s =await fh.DownloadFileAsStreamAsync("",nameVar+"testfile.txt",Format.DownloadFormat.text);
            // byte[] bytes = new byte[s.Length + 10];
            // int numBytesToRead = (int)s.Length;
            // int numBytesRead = 0;
            // do
            // {
            //     // Read may return anything from 0 to 10.
            //     int n = s.Read(bytes, numBytesRead, 10);
            //     numBytesRead += n;
            //     numBytesToRead -= n;
            // } while (numBytesToRead > 0);
            // s.Close();
            // string str = Encoding.Default.GetString(bytes);
            // Console.WriteLine(str);
            //
            // Console.WriteLine(BaseConverter.Base64Decode(file.content));
            // Console.WriteLine(file.content);
            // Console.WriteLine(file.last_modified.GetType()+" "+file.last_modified.ToLongTimeString()+" "+file.last_modified.ToLongDateString()+" "+file.size+" "+file.mimetype);
            //
            // StringBuilder  sb = new StringBuilder();
            // using (StringWriter sw = new StringWriter(sb))
            // using (JsonTextWriter writer = new JsonTextWriter(sw))
            // {
            //     writer.QuoteChar = '\'';
            //
            //     JsonSerializer ser = new JsonSerializer();
            //     ser.Serialize(writer,file );
            // }
            //
            // var js = sb.ToString();
            // Console.WriteLine(js);
            // await fh.UploadFileAsync("",nameVar+"2testfile.json",js,Format.UploadFormat.json);


            // var t = await fh.ExistDirectoryAsync("/AAAAAAAAA");
            // await fh.CreateDirectoryAsync("/AAAAAAAAA");
            // await fh.GetDirectoryAsync("/test_directory_pyrun");
            // await fh.ChangeContentFileAsync("","vvi.txt","vrv");
            // await fh.DeleteFileAsync("","vvi.txt");


            // var kernelId = await StartKernel(CSharpKernel);
            // var sessionId = await StartSession(kernelId, CSharpKernel);
            // var webSocket = await ConnectToKernelAsync(kernelId, sessionId);
            // Console.WriteLine($"WebSocket state: {webSocket.State}");
        }


        private static async Task SendBigData(string sessionId, WebSocket webSocket, IDataTransfer gc)
        {
            var message = CreateCSExecuteRequestMessage(sessionId, gc.InitialPart);
            var requestJson = JsonConvert.SerializeObject(message);
            // Console.WriteLine("request1>>>>>>>>>>>>>>>>>>>>>>>>> \n" + requestJson);
            Console.WriteLine("-------------------------------------------------------------------------------");
            await webSocket.SendAsync(Encoding.UTF8.GetBytes(requestJson), WebSocketMessageType.Text, true, default);
            var t = Receive(webSocket);

            gc.RepeativePart.ForEach(async part =>
            {
                message = CreateCSExecuteRequestMessage(sessionId, part);
                requestJson = JsonConvert.SerializeObject(message);
                // Console.WriteLine("request2>>>>>>>>>>>>>>>\n" + requestJson);
                Console.WriteLine("-------------------------------------------------------------------------------");
                await webSocket.SendAsync(Encoding.UTF8.GetBytes(requestJson), WebSocketMessageType.Text, true,
                    default);
            });

            message = CreateCSExecuteRequestMessage(sessionId, gc.FinalPart);
            requestJson = JsonConvert.SerializeObject(message);
            // Console.WriteLine("request3>>>>>>>>>>>>>>> " + requestJson);
            Console.WriteLine("-------------------------------------------------------------------------------");
            await webSocket.SendAsync(Encoding.UTF8.GetBytes(requestJson), WebSocketMessageType.Text, true, default);

            await t;
        }

        private static async Task<List<Cookie>> Login()
        {
            var cookies = new CookieContainer();
            var handler = new HttpClientHandler {CookieContainer = cookies};
            var baseAddress = new Uri($"http://localhost:8888/login?password={Password}");
            var message = new HttpRequestMessage(HttpMethod.Get, baseAddress);
            using var client = new HttpClient(handler);
            var response1 = await client.SendAsync(message);

            var cookie = cookies.GetCookies(baseAddress).Cast<Cookie>().ToList();
            XsrfToken = cookie[0].Value;
            var cookieContainer = new CookieContainer();
            cookieContainer.Add(GetXsrfCookie());
            var handle = new HttpClientHandler() {CookieContainer = cookieContainer};
            var req = new HttpRequestMessage(HttpMethod.Post, baseAddress)
            {
                Headers = {{XsrfHeaderKey, XsrfToken}}
            };

            using var client2 = new HttpClient(handle);
            var response2 = await client2.SendAsync(req);
            var cok = cookieContainer.GetCookies(baseAddress).ToList();

            LoginCookieValue = cok[0].Value;
            return cok;
        }

        private static async Task<ClientWebSocket> ConnectToKernelAsync(string kernelId, string sessionId)
        {
            var uri = $"ws://localhost:8888/api/kernels/{kernelId}/channels?session_id={sessionId}";
            var uri2 = new Uri(uri);
            Console.WriteLine(uri2.Host);
            var webSocket = new ClientWebSocket();
            webSocket.Options.Cookies = new CookieContainer();
            webSocket.Options.Cookies.Add(GetLoginCookie());
            webSocket.Options.Cookies.Add(GetXsrfCookie());
            await webSocket.ConnectAsync(uri2, default);
            return webSocket;
        }

        private static async Task<string> StartSession(string kernelId, string kernelName)
        {
            var baseAddress = new Uri("http://localhost:8888/api/sessions");
            var cookieContainer = new CookieContainer();
            foreach (var cookie in GetCookies())
            {
                cookieContainer.Add(cookie);
            }

            using var handler = new HttpClientHandler() {CookieContainer = cookieContainer};
            using var client = new HttpClient(handler) {BaseAddress = baseAddress};
            var httpRequestMessage1 = new HttpRequestMessage(HttpMethod.Post, baseAddress)
            {
                Content = new StringContent(GetSessionContent(kernelId, kernelName)),
                Headers = {{XsrfHeaderKey, XsrfToken}},
            };
            var responseMessage = await client.SendAsync(httpRequestMessage1);
            var content = await responseMessage.Content.ReadAsStringAsync();
            var res = JsonConvert.DeserializeObject<Session>(content);
            return res.id;
        }

        private static string GetSessionContent(string kernelId, string kernelName)
        {
            return JsonConvert.SerializeObject(new Session()
            {
                // id = kernelId,
                // name = "",
                //path = "asghar.ipynb",
                type = "notebook",
                kernel = new Kernel()
                {
                    id = kernelId,
                    // name = kernelName,
                    // connections = 1
                }
            });
        }

        private static IEnumerable<Cookie> GetCookies()
        {
            return new List<Cookie>()
            {
                GetLoginCookie(),
                GetXsrfCookie()
            };
        }

        private static async Task<string> StartKernel(string kernelName)
        {
            var baseAddress = new Uri("http://localhost:8888/api/kernels");
            var cookieContainer = new CookieContainer();
            foreach (var cookie in GetCookies())
            {
                cookieContainer.Add(cookie);
            }

            using var handler = new HttpClientHandler() {CookieContainer = cookieContainer};
            using var client = new HttpClient(handler) {BaseAddress = baseAddress};
            var httpRequestMessage1 = new HttpRequestMessage(HttpMethod.Post, baseAddress)
            {
                Content = new StringContent(JsonConvert.SerializeObject(new StartKernel() {name = kernelName})),
                Headers = {{XsrfHeaderKey, XsrfToken}}
            };
            var responseMessage = await client.SendAsync(httpRequestMessage1);
            var content = await responseMessage.Content.ReadAsStringAsync();
            var res = JsonConvert.DeserializeObject<StartKernelResponse>(content);
            return res.id;
        }

        private static Cookie GetXsrfCookie()
        {
            return new Cookie(XsrfCookieKey, XsrfToken, "/", "localhost")
            {
                Expires = DateTime.Now.AddYears(1)
            };
        }

        private static Cookie GetLoginCookie()
        {
            return new Cookie(LoginCookieKey,
                LoginCookieValue, "/", "localhost")
            {
                Expires = DateTime.Now.AddYears(1)
            };
        }


        private static async Task Receive(WebSocket socket)
        {
            var buffer = new ArraySegment<byte>(new byte[2048]);
            do
            {
                WebSocketReceiveResult result;
                await using var ms = new MemoryStream();
                do
                {
                    result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                } while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Close)
                    break;


                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms, Encoding.UTF8);
                var received = await reader.ReadToEndAsync();


                Console.WriteLine(
                    $"received {DateTime.Now.ToLongTimeString()}<<<<<<<<<<<<\n{received}\n------------------------------------------------------");

                // Console.WriteLine();
            } while (true);
        }


        #region PythonMethods

        private static JupyterMessage CreateCSExecuteRequestMessage(string sessionId, string executecode = null)
        {
            return new JupyterMessage()
            {
                header = new JupyterMessage.Header()
                {
                    date = DateTime.Now,
                    msg_id = Guid.NewGuid().ToString(),
                    msg_type = JupyterMessage.Header.MsgType.execute_request,
                    session = sessionId,
                    username = "",
                    // username = "hosseinaghaei",
                    //username = "username",
                    // version = "5.3"
                    version = "5.2"
                },
                channel = JupyterMessage.Channel.shell,
                content = new JupyterMessage.ExecuteRequestContent()
                {
                    allow_stdin = true,
                    code = executecode ?? "var a = 100;\ndisplay(a);",
                    //code = "count = 0\nfor i in range(int(1e10)):\n  count+=1\nprint(count)",
                    silent = false,
                    stop_on_error = true,
                    store_history = true,
                    user_expressions = null
                },
                metadata = new Dictionary<string, object>()
                {
                    {"deletedCells", Array.Empty<object>()},
                    {"cellId", "a6418586-c721-48bd-a1cf-6122dd4d9313"}
                },
                parent_header = { },
                buffers = Array.Empty<object>()
            };
        }

        private static JupyterMessage CreateCommOpenMessage()
        {
            return new JupyterMessage()
            {
                header = new JupyterMessage.Header()
                {
                    date = DateTime.Now,
                    msg_id = "a7b59eab-705c98c3f8d941fc2f6c26d7_55",
                    msg_type = JupyterMessage.Header.MsgType.comm_open,
                    session = "a7b59eab-705c98c3f8d941fc2f6c26d7",
                    username = "hosseinaghaei",
                    version = "5.3"
                },
                channel = JupyterMessage.Channel.shell,
                content = new JupyterMessage.CommOpenContent()
                {
                    comm_id = CommId,
                    target_name = TargetName,
                    data = { }
                },
                metadata = { },
                parent_header = { }
            };
        }

        private static JupyterMessage CreateIsCompleteRequestMessage()
        {
            return new JupyterMessage()
            {
                header = new JupyterMessage.Header()
                {
                    date = DateTime.Now,
                    msg_id = "a7b59eab-705c98c3f8d941fc2f6c26d7_55",
                    msg_type = JupyterMessage.Header.MsgType.is_complete_request,
                    session = "a7b59eab-705c98c3f8d941fc2f6c26d7",
                    username = "hosseinaghaei",
                    version = "5.3"
                },
                channel = JupyterMessage.Channel.shell,
                content = new JupyterMessage.IsCompleteRequest()
                {
                    code = " "
                },
                metadata = { },
                parent_header = { }
            };
        }

        private static JupyterMessage CreateShutDownRequestMessage()
        {
            //kernel shutdown  request
            return new JupyterMessage()
            {
                header = new JupyterMessage.Header()
                {
                    date = DateTime.Now,
                    msg_id = "a7b59eab-705c98c3f8d941fc2f6c26d7_55",
                    msg_type = JupyterMessage.Header.MsgType.shutdown_request,
                    session = "a7b59eab-705c98c3f8d941fc2f6c26d7",
                    username = "hosseinaghaei",
                    version = "5.3"
                },
                channel = JupyterMessage.Channel.shell,
                content = new JupyterMessage.ShutdownRequestContent()
                {
                    restart = false
                },
                metadata = { },
                parent_header = { }
            };
        }

        private static JupyterMessage CreateKernelInfoRequestMessage()
        {
            return new()
            {
                header = new JupyterMessage.Header()
                {
                    date = DateTime.Now,
                    msg_id = "a7b59eab-705c98c3f8d941fc2f6c26d7_55",
                    msg_type = JupyterMessage.Header.MsgType.kernel_info_request,
                    session = "a7b59eab-705c98c3f8d941fc2f6c26d7",
                    username = "hosseinaghaei",
                    version = "5.3"
                },
                channel = JupyterMessage.Channel.shell,
                content = new JupyterMessage.KernelInfoRequestContent()
                    { },
                metadata = { },
                parent_header = { }
            };
        }

        private static JupyterMessage CreateInspectRequestMessage()
        {
            return new()
            {
                header = new JupyterMessage.Header()
                {
                    date = DateTime.Now,
                    msg_id = "a7b59eab-705c98c3f8d941fc2f6c26d7_55",
                    msg_type = JupyterMessage.Header.MsgType.inspect_request,
                    session = "a7b59eab-705c98c3f8d941fc2f6c26d7",
                    username = "hosseinaghaei",
                    version = "5.3"
                },
                channel = JupyterMessage.Channel.shell,
                content = new JupyterMessage.InspectRequestContent()
                {
                    code = "",
                    cursor_pos = 0,
                    detail_level = 0
                },
                metadata = { },
                parent_header = { }
            };
        }


        private static JupyterMessage CreateJupyterMessage3()
        {
            return new JupyterMessage()
            {
                header = new JupyterMessage.Header()
                {
                    date = DateTime.Now,
                    msg_id = "a7b59eab-705c98c3f8d941fc2f6c26d7_55",
                    msg_type = JupyterMessage.Header.MsgType.comm_msg,
                    session = "a7b59eab-705c98c3f8d941fc2f6c26d7",
                    username = "hosseinaghaei",
                    version = "5.3"
                },
                channel = JupyterMessage.Channel.shell,
                content = new JupyterMessage.CommMsgContent()
                {
                    comm_id = CommId,
                    data = ReadPicture()
                },
                metadata = { },
                parent_header = { }
            };
        }

        private static Dictionary<string, object> ReadPicture()
        {
            // return new() {{"Pic", File.ReadAllBytes("/Users/hosseinaghaei/Downloads/boy_result.jpg")}};
            return new() {{"Pic", File.ReadAllBytes("D:/Uni/form1-bolandi-karamozi.jpg")}};
        }

        #endregion


        static async Task SendHeavyFile(IJupyterFileManager fileManager)
        {
            var path = "/HeavyFiles4";
            const string nameVar = "big1_";
            await using var file = new FileStream("D:\\Uni\\DataMining\\Hamiz\\data.csv", FileMode.Open);
            var bytes = new byte[1000];
            var packet = new byte[100_000_000];
            var destCursor = 0;
            var packetNum = 0;
            for (var i = 0; i < file.Length; i += bytes.Length)
            {
                var len = (file.Length - i) > bytes.Length ? bytes.Length : (file.Length - i);
                file.Read(bytes, 0, (int) len);
                Array.Copy(bytes, 0, packet, destCursor, (int) len);
                destCursor += bytes.Length;
                if (destCursor >= packet.Length)
                {
                    var base64String = Convert.ToBase64String(packet, 0, packet.Length);
                    await fileManager.UploadFileAsync(path, $"data_{nameVar}{packetNum}.csv", base64String,
                        ContentFormat.Base64);
                    packetNum++;
                    destCursor = 0;
                }
            }

            if (destCursor != 0)
            {
                var base64String = Convert.ToBase64String(packet, 0, destCursor);
                await fileManager.UploadFileAsync(path, $"data_{nameVar}{packetNum}.csv", base64String,
                    ContentFormat.Base64);
            }
        }
    }
}