using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RevitRemoteCommandTester.Services
{
    public class WebSocketCommunicationService
    {
        private string _serverUrl;
        private int _serverPort;
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cancellationTokenSource;

        public WebSocketCommunicationService(string serverAddress = "localhost", int serverPort = 8082)
        {
            _serverUrl = $"ws://{serverAddress}:{serverPort}";
            _serverPort = serverPort;
            _webSocket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// 连接到WebSocket服务器
        /// </summary>
        public async Task ConnectAsync()
        {
            try
            {
                await _webSocket.ConnectAsync(new Uri(_serverUrl), _cancellationTokenSource.Token);
                Console.WriteLine($"Connected to WebSocket server at {_serverUrl}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket connection error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 断开与WebSocket服务器的连接
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnected", CancellationToken.None);
                Console.WriteLine("WebSocket connection closed");
            }
        }

        /// <summary>
        /// 向服务器发送JSON-RPC命令
        /// </summary>
        /// <param name="method">命令名称</param>
        /// <param name="parameters">命令参数</param>
        /// <returns>服务器响应</returns>
        public async Task<string> SendCommandAsync(string method, object parameters)
        {
            // 构建符合JSON-RPC 2.0规范的请求
            var jsonRpcRequest = new
            {
                jsonrpc = "2.0",
                method,
                @params = parameters,
                id = 1
            };

            string commandJson = JsonConvert.SerializeObject(jsonRpcRequest, Formatting.None);
            return await SendRawCommandAsync(commandJson);
        }

        /// <summary>
        /// 发送原始JSON命令
        /// </summary>
        /// <param name="jsonRequest">要发送的JSON</param>
        /// <returns>服务器响应</returns>
        public async Task<string> SendRawCommandAsync(string jsonRequest)
        {
            if (_webSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("WebSocket connection is not open. Call ConnectAsync first.");
            }

            // 发送文本消息
            byte[] buffer = Encoding.UTF8.GetBytes(jsonRequest);
            await _webSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                _cancellationTokenSource.Token
            );

            // 接收响应
            byte[] responseBuffer = new byte[4096];
            WebSocketReceiveResult result;
            StringBuilder responseBuilder = new StringBuilder();

            do
            {
                result = await _webSocket.ReceiveAsync(
                    new ArraySegment<byte>(responseBuffer),
                    _cancellationTokenSource.Token
                );

                string responseText = Encoding.UTF8.GetString(responseBuffer, 0, result.Count);
                responseBuilder.Append(responseText);
            }
            while (!result.EndOfMessage);

            return responseBuilder.ToString();
        }

        // 设置新的连接参数
        public void UpdateConnectionSettings(string serverAddress, int serverPort)
        {
            _serverUrl = $"ws://{serverAddress}:{serverPort}";
            _serverPort = serverPort;
        }

        /// <summary>
        /// 检查连接状态
        /// </summary>
        public bool IsConnected => _webSocket.State == WebSocketState.Open;
    }
}