using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SampleWS
{
    class Program
    {
        private static string TargetName => "my_target4";
        private static string CSharpKernel => ".net-csharp";
        private static string PythonKernel => "python3";


        private static readonly string CommId = Guid.NewGuid().ToString();

        static async Task Main(string[] args)
        {
            // var kernelId = "10ff846c-f93e-4207-880e-2b9fe3faf455";
            var kernelId = await StartKernel(CSharpKernel);

            // var sessionId = "544c7e73-b5c6-45fe-bd3b-ec184c6614fa";
            var sessionId = await StartSession(kernelId, CSharpKernel);

            // await StartSession(kernelId);
            // var http = new HttpClient();
            // var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "http://localhost:8888/api/kernels")
            // {
            //     Content = new StringContent(JsonConvert.SerializeObject(new StartKernel() {name = "net-csharp"})),
            //     Headers = {{"X-XSRFToken", "2|5965414d|3bb094199a7fc8d24c9f910bc623e14e|1626098682"}},
            // };
            // var responseMessage = await http.SendAsync(httpRequestMessage);
            //
            // var content = await responseMessage.Content.ReadAsStringAsync();
            // var res = JsonConvert.DeserializeObject<StartKernelResponse>(content);

            var cts = new CancellationTokenSource();
            // const string kernelId = "788e2414-6a9a-420b-bb30-fd09a1eebd19";
            //const string token = "e18ea91ce003cf8d29785bccf801ccc783d096b0f17262a3";
            // var uri = $"ws://localhost:8888/api/kernels/{kernelId}/channels?token={token}";
            // var uri = $"ws://localhost:8888/api/kernels/{kernelId}/channels";
            var uri = $"ws://localhost:8888/api/kernels/{kernelId}/channels";

            var uri2 = new Uri(uri);

            var webSocket = new ClientWebSocket();
            webSocket.Options.Cookies = new CookieContainer();
            webSocket.Options.Cookies.Add(new Cookie("username-localhost-8888",
                "2|1:0|10:1627120645|23:username-localhost-8888|44:OTRkMzE1YzU1M2RhNDQzOTllNjNmNDA2NWU2MGE2YTY=|1745520ca85f96381babd44b5d2674f02365afcc457ad98efc62421b2902adf9",
                "/",
                uri2.Host)
            {
                Expires = new DateTime(2025, 10, 2)
            });
            webSocket.Options.Cookies.Add(new Cookie("_xsrf", "2|5965414d|3bb094199a7fc8d24c9f910bc623e14e|1626098682", "/", uri2.Host)
            {
                Expires = new DateTime(2025, 10, 2)
            });

            //Console.WriteLine(webSocket.State);
            await webSocket.ConnectAsync(uri2, cts.Token);
            Console.WriteLine(webSocket.State);
            var message = CreateJupyterMessage(sessionId);
            //var message = CreateJupyterMessage2();
            var json = JsonConvert.SerializeObject(message);
            await webSocket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, cts.Token);
            // message = CreateJupyterMessage3();
            // json = JsonConvert.SerializeObject(message);
            // Console.WriteLine(json);
            // await webSocket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, cts.Token);
            await Receive(webSocket);
        }

        private static async Task<string> StartSession(string kernelId, string kernelName)
        {
            var baseAddress = new Uri("http://localhost:8888/api/sessions");
            var cookieContainer = new CookieContainer();
            cookieContainer.Add(baseAddress, new Cookie("username-localhost-8888",
                "2|1:0|10:1627120645|23:username-localhost-8888|44:OTRkMzE1YzU1M2RhNDQzOTllNjNmNDA2NWU2MGE2YTY=|1745520ca85f96381babd44b5d2674f02365afcc457ad98efc62421b2902adf9",
                "/",
                "localhost"));
            cookieContainer.Add(new Cookie("_xsrf", "2|5965414d|3bb094199a7fc8d24c9f910bc623e14e|1626098682", "/", "localhost"));
            using var handler = new HttpClientHandler() {CookieContainer = cookieContainer};
            using var client = new HttpClient(handler) {BaseAddress = baseAddress};
            var httpRequestMessage1 = new HttpRequestMessage(HttpMethod.Post, baseAddress)
            {
                Content = new StringContent(GetSessionContent(kernelId, kernelName)),
                Headers = {{"X-XSRFToken", "2|5965414d|3bb094199a7fc8d24c9f910bc623e14e|1626098682"}},
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
                id = kernelId,
                name = "",
                path = "",
                type = "notebook",
                kernel = new Kernel()
                {
                    id = kernelId,
                    name = kernelName,
                    connections = 1
                }
            });
        }

        private static async Task<string> StartKernel(string kernelName)
        {
            var baseAddress = new Uri("http://localhost:8888/api/kernels");
            var cookieContainer = new CookieContainer();
            cookieContainer.Add(baseAddress, new Cookie("username-localhost-8888",
                "2|1:0|10:1627120645|23:username-localhost-8888|44:OTRkMzE1YzU1M2RhNDQzOTllNjNmNDA2NWU2MGE2YTY=|1745520ca85f96381babd44b5d2674f02365afcc457ad98efc62421b2902adf9",
                "/",
                "localhost"));
            cookieContainer.Add(new Cookie("_xsrf", "2|5965414d|3bb094199a7fc8d24c9f910bc623e14e|1626098682", "/", "localhost"));
            using var handler = new HttpClientHandler() {CookieContainer = cookieContainer};
            using var client = new HttpClient(handler) {BaseAddress = baseAddress};
            var httpRequestMessage1 = new HttpRequestMessage(HttpMethod.Post, baseAddress)
            {
                Content = new StringContent(JsonConvert.SerializeObject(new StartKernel() {name = kernelName})),
                Headers = {{"X-XSRFToken", "2|5965414d|3bb094199a7fc8d24c9f910bc623e14e|1626098682"}},
            };
            var responseMessage = await client.SendAsync(httpRequestMessage1);
            var content = await responseMessage.Content.ReadAsStringAsync();
            var res = JsonConvert.DeserializeObject<StartKernelResponse>(content);
            return res.id;
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
                //var obj = JsonConvert.DeserializeObject<JupyterMessage>(received);
                Console.WriteLine(received);
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();
            } while (true);
        }

        private static JupyterMessage CreateJupyterMessage(string sessionId)
        {
            return new JupyterMessage()
            {
                header = new JupyterMessage.Header()
                {
                    date = DateTime.Now,
                    msg_id = "a7b59eab-705c98c3f8d941fc2f6c26d7_55",
                    msg_type = JupyterMessage.Header.MsgType.execute_request,
                    session = sessionId,
                    username = "hosseinaghaei",
                    //username = "username",
                    version = "5.3"
                    //version = "5.2"
                },
                channel = JupyterMessage.Channel.shell,
                content = new JupyterMessage.ExecuteRequestContent()
                {
                    allow_stdin = true,
                    //code = "a = 5\nprint(\"The Result : \", a**2)",
                    //code = "count = 0\nfor i in range(int(1e10)):\n  count+=1\nprint(count)",
                    code = "display(\"bos back\");",
                    silent = false,
                    stop_on_error = true,
                    store_history = true,
                    user_expressions = { }
                },
                metadata = { },
                parent_header = { },
                buffers = Array.Empty<object>()
            };
        }

        private static JupyterMessage CreateJupyterMessage2()
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
            return new() {{"Pic", File.ReadAllBytes("/Users/hosseinaghaei/Downloads/boy_result.jpg")}};
        }
    }
}