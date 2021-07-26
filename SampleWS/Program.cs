﻿using System;
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

        private const string XsrfHeaderKey = "X-XSRFToken";
        private const string XsrfCookieKey = "_xsrf";
        private const string XsrfToken = "2%7C700b3e6c%7C00863d280e479c2310a79ca3c0cf7fe3%7C1626703811";

        private const string LoginCookieKey = "username-localhost-8888";
        private const string LoginCookieValue = "2|1:0|10:1627303366|23:username-localhost-8888|44:YWM5OGYwNGVlMGIyNGQ0MDk3OGYzOTRjZGMwNmM3NjI=|734984294192067c95b042198543c7eb907d8200bde8a2f8c47de1d2a348befe";
        private const string Password = "m.jupyter";


        private static readonly string CommId = Guid.NewGuid().ToString();

        static async Task Main(string[] args)
        {
            var kernelId = await StartKernel(CSharpKernel);
            var sessionId = await StartSession(kernelId, CSharpKernel);
            
            var webSocket = await ConnectToKernelAsync(kernelId);
            
            Console.WriteLine($"WebSocket state: {webSocket.State}");
            
            var message = CreateJupyterMessage(sessionId);
            var json = JsonConvert.SerializeObject(message);
            await webSocket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, default);
            await Receive(webSocket);
        }

        private static async Task<ClientWebSocket> ConnectToKernelAsync(string kernelId)
        {
            var uri = $"ws://localhost:8888/api/kernels/{kernelId}/channels";
            var uri2 = new Uri(uri);

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
                path = "asghar.ipynb",
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
                LoginCookieValue,
                "/",
                "localhost")
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
                    msg_id = "asghar",
                    msg_type = JupyterMessage.Header.MsgType.execute_request,
                    session = sessionId,
                    // username = "hosseinaghaei",
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
                    code = "display(\"bos back\")",
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