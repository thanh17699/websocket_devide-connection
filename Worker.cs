using System.Net.Sockets;
using System.Net;
using System.Text.RegularExpressions;
using System.Text;
using Vidoc.Socket.Utils;
using Newtonsoft.Json.Linq;
using Vidoc.Socket.Enums;
using Vidoc.Socket.ServerResponseMessage.Success;
using Newtonsoft.Json;
using idoc.Socket.TerminalSendMessage;
using Vidoc.Socket.Requests;
using Vidoc.Socket.ServerResponseMessage.Http;
using Vidoc.Socket.ServerResponseMessage.Fail;
using Vidoc.Socket.Configs;

namespace Vidoc.Socket
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly AppConfig _appConfig;

        public Worker(ILogger<Worker> logger, AppConfig appConfig)
        {
            _logger = logger;
            _appConfig = appConfig;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            List<System.Net.Sockets.Socket> clients = new List<System.Net.Sockets.Socket>();
            TcpListener tcpListener = new TcpListener(IPAddress.Parse(_appConfig.SocketServer.Localaddr), _appConfig.SocketServer.Port);
            tcpListener.Start();
            while (true)
            {
                System.Net.Sockets.Socket client = tcpListener.AcceptSocket();
                if (client.Connected)
                {
                    clients.Add(client);
                    Thread nuevoHilo = new(() => Listeners(client));
                    nuevoHilo.Start();
                }
            }
        }

        private void Listeners(System.Net.Sockets.Socket client)
        {
            Console.WriteLine("Client:" + client.RemoteEndPoint + " now connected to server.");
            NetworkStream stream = new(client);

            while (true)
            {
                while (!stream.DataAvailable) ;
                while (client.Available < 3) ; // match against "get"

                byte[] bytes = new byte[client.Available];
                stream.Read(bytes, 0, bytes.Length);
                string s = Encoding.UTF8.GetString(bytes);

                if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
                {
                    Console.WriteLine("=====Handshaking from client=====\n{0}", s);

                    // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
                    // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
                    // 3. Compute SHA-1 and Base64 hash of the new value
                    // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
                    string swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                    string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                    byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                    string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                    // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
                    byte[] response = Encoding.UTF8.GetBytes(
                        "HTTP/1.1 101 Switching Protocols\r\n" +
                        "Connection: Upgrade\r\n" +
                        "Upgrade: websocket\r\n" +
                        "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

                    stream.Write(response, 0, response.Length);
                }
                else
                {
                    string terminalMessageString = StringUtils.DecodeMessage(bytes);
                    _logger.LogInformation($"terminal message::: {terminalMessageString}");
                    if (!string.IsNullOrEmpty(terminalMessageString))
                    {
                        JObject? jsonObject = !string.IsNullOrEmpty(terminalMessageString) ? JObject.Parse(terminalMessageString) : null;
                        if (jsonObject != null)
                        {
                            string cmd = (string)jsonObject["cmd"];
                            if (cmd == ECmd.reg.ToString())
                            {
                                SrmRegister srmRegister = new()
                                {
                                    ret = "reg",
                                    result = true,
                                    cloudtime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                    nosenduser = true,
                                };
                                string strSrmRegister = JsonConvert.SerializeObject(srmRegister);
                                byte[] srmRegisterByte = StringUtils.EncodeMessageToSend(strSrmRegister);
                                client.Send(srmRegisterByte);
                            }
                            else if (cmd == ECmd.sendlog.ToString())
                            {
                                TsSendlogMessage? terminalSendlogMessage = JsonConvert.DeserializeObject<TsSendlogMessage>(terminalMessageString);
                                if (terminalSendlogMessage != null && terminalSendlogMessage.record != null && terminalSendlogMessage.record.Count > 0)
                                {
                                    bool isSuccess = true;
                                    foreach (var record in terminalSendlogMessage.record)
                                    {
                                        try
                                        {
                                            string uri = $"{_appConfig.VidocUri.Checkin}{record.enrollid}";
                                            BodyRequest body = new()
                                            {
                                                ThoiGian = record.time
                                            };
                                            string jsonData = JsonConvert.SerializeObject(body);
                                            var httpRequest = (HttpWebRequest)WebRequest.Create(uri);
                                            httpRequest.ContentType = "application/json;charset=utf-8";
                                            httpRequest.Headers.Add("X-API-KEY", _appConfig.XApiKey);
                                            httpRequest.Method = WebRequestMethods.Http.Post;
                                            httpRequest.KeepAlive = false;
                                            httpRequest.ProtocolVersion = HttpVersion.Version10;
                                            httpRequest.AllowAutoRedirect = false;
                                            httpRequest.AllowWriteStreamBuffering = false;
                                            httpRequest.ContentLength = jsonData.Length;
                                            using (var streamWriter = new StreamWriter(httpRequest.GetRequestStream()))
                                            {
                                                streamWriter.Write(jsonData);
                                                streamWriter.Flush();
                                                streamWriter.Close();
                                            }
                                            var webResponse = (HttpWebResponse)httpRequest.GetResponse();
                                            if (webResponse.StatusCode == HttpStatusCode.OK)
                                            {
                                                string responseString = new StreamReader(webResponse.GetResponseStream()).ReadToEnd();
                                                if (!string.IsNullOrEmpty(responseString))
                                                {
                                                    HttpResponseData? responseData = JsonConvert.DeserializeObject<HttpResponseData>(responseString);
                                                    if (responseData != null && responseData.statusCode == 200 && responseData.data != null && (bool)responseData.data == true)
                                                    {
                                                        isSuccess = true;
                                                    }
                                                    else
                                                    {
                                                        isSuccess = false;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            _logger.LogError("post with error::: {} | {}", e.Message, e.Message.ToString());
                                        }

                                    }
                                    if (isSuccess)
                                    {
                                        SrmSendlogSuccess srmSendlogSuccess = new()
                                        {
                                            ret = terminalSendlogMessage.cmd,
                                            result = true,
                                            count = terminalSendlogMessage.count,
                                            logindex = terminalSendlogMessage.logindex,
                                            cloudtime = DateTime.Now.ToString("yyyy-MM/dd HH:mm:ss"),
                                            access = 1
                                        };
                                        string strSendlogSuccess = JsonConvert.SerializeObject(srmSendlogSuccess);
                                        byte[] sendLogSuccessByte = StringUtils.EncodeMessageToSend(strSendlogSuccess);
                                        client.Send(sendLogSuccessByte);
                                    }
                                    else
                                    {
                                        SrmSendlogFail srmSendlogFail = new()
                                        {
                                            ret = terminalSendlogMessage.cmd,
                                            result = true,
                                            reason = 1
                                        };
                                        string strSendlogFail = JsonConvert.SerializeObject(srmSendlogFail);
                                        byte[] sendLogFailByte = StringUtils.EncodeMessageToSend(strSendlogFail);
                                        client.Send(sendLogFailByte);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}