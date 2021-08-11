using System;
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

            var gc = new CodeTransformer();
            gc.GenerateHardCode(1000000);
            
            await Login();
            var kernelId = await StartKernel(CSharpKernel);
            var sessionId = await StartSession(kernelId, CSharpKernel);

            var webSocket = await ConnectToKernelAsync(kernelId,sessionId);

            Console.WriteLine($"WebSocket state: {webSocket.State}");

            await SendBigData(sessionId,webSocket,gc);
          
          
        }

        

        private static async Task SendBigData(string sessionId,WebSocket webSocket,CodeTransformer gc)
        {
            var startTime = DateTime.Now;
            // Marshal.SizeOf(new SampleObject() {row = 4, id = "fefefsfesvvrbrerevervre", dt = DateTime.Now});
            
            var message = CreateCSExecuteRequestMessage(sessionId,gc.hardCodeInit);
            var requestJson = JsonConvert.SerializeObject(message);
            Console.WriteLine("request1>>>>>>>>>>>>>>>>>>>>>>>>> \n"+requestJson);
            Console.WriteLine("-------------------------------------------------------------------------------");
            await webSocket.SendAsync(Encoding.UTF8.GetBytes(requestJson), WebSocketMessageType.Text, true, default);
            var t =Receive(webSocket);
            // Thread.Sleep(2000);
            gc._hardCodeRepeatPart.ForEach(async part =>
            {
                message = CreateCSExecuteRequestMessage(sessionId,part);
                requestJson = JsonConvert.SerializeObject(message);
                // Console.WriteLine("request2>>>>>>>>>>>>>>>\n"+requestJson);
                // Console.WriteLine("-------------------------------------------------------------------------------");
                await webSocket.SendAsync(Encoding.UTF8.GetBytes(requestJson), WebSocketMessageType.Text, true, default);
                
                // await Receive(webSocket);
                // Thread.Sleep(100);  
            });
            
            message = CreateCSExecuteRequestMessage(sessionId,gc.hardCodeFinal);
            requestJson = JsonConvert.SerializeObject(message);
            Console.WriteLine("request3>>>>>>>>>>>>>>> "+requestJson);
            Console.WriteLine("-------------------------------------------------------------------------------");
            await webSocket.SendAsync(Encoding.UTF8.GetBytes(requestJson), WebSocketMessageType.Text, true, default);
            // await Receive(webSocket);
            try
            {
                await t;
            }
            finally
            {
                Console.WriteLine($"{DateTime.Now}---{startTime}");
            } 
            
        }
        
        private static async Task Login()
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
        }

        private static async Task<ClientWebSocket> ConnectToKernelAsync(string kernelId,string sessionId)
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

                // var parsed = JObject.Parse(received);
                // if(parsed["msg_type"].Equals("status") && parsed.ContainsKey("parent_header")))
                // var obj = JsonConvert.DeserializeObject<JupyterMessage>(received);
                Console.WriteLine($"received {DateTime.Now.ToLongTimeString()}<<<<<<<<<<<<\n{received}\n------------------------------------------------------");
                // Console.WriteLine(received);
                // Console.WriteLine("-------------------------------------------------------------------------------");
                // Console.WriteLine();

            } while (true);
        }

        private static JupyterMessage CreateExecuteRequestMessage(string sessionId)
        {
            return new JupyterMessage()
            {
                header = new JupyterMessage.Header()
                {
                    date = DateTime.Now,
                    msg_id = "asghar",
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
                    code = "a = 5\nprint(\"The Result : \", a**2)",
                    //code = "count = 0\nfor i in range(int(1e10)):\n  count+=1\nprint(count)",
                    //code = "display(\"bos back\")",
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
        private static JupyterMessage CreateCSExecuteRequestMessage(string sessionId,string executecode=null)
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
                    code =executecode??"var a = 100;\ndisplay(a);",
                    //code = "count = 0\nfor i in range(int(1e10)):\n  count+=1\nprint(count)",
                    silent = false,
                    stop_on_error = true,
                    store_history = true,
                    user_expressions = null
                },
                metadata = new Dictionary<string,object>()
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
    }
}