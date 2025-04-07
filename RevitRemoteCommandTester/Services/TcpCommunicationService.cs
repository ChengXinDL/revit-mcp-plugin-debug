using Newtonsoft.Json;
using System.Net.Sockets;
using System.Text;

namespace RevitRemoteCommandTester.Services
{
    public class TcpCommunicationService
    {
        private string serverAddress;
        private int serverPort;

        public TcpCommunicationService(string serverAddress = "localhost", int serverPort = 8080)
        {
            this.serverAddress = serverAddress;
            this.serverPort = serverPort;
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
                parameters,
                id = 1
            };

            string commandJson = JsonConvert.SerializeObject(jsonRpcRequest, Formatting.Indented);
            return await SendRawCommandAsync(commandJson);
        }

        /// <summary>
        /// 发送原始JSON命令
        /// </summary>
        /// <param name="jsonRequest">要发送的JSON</param>
        /// <returns>服务器响应</returns>
        public async Task<string> SendRawCommandAsync(string jsonRequest)
        {
            using (TcpClient client = new TcpClient())
            {
                // 设置连接超时
                var connectTask = client.ConnectAsync(serverAddress, serverPort);
                if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
                {
                    throw new TimeoutException("Connection to server timed out.");
                }

                NetworkStream stream = client.GetStream();

                // 发送命令
                byte[] requestData = Encoding.UTF8.GetBytes(jsonRequest);
                await stream.WriteAsync(requestData, 0, requestData.Length);

                // 接收响应
                byte[] responseData = new byte[4096];
                int bytesRead = await stream.ReadAsync(responseData, 0, responseData.Length);
                string response = Encoding.UTF8.GetString(responseData, 0, bytesRead);

                return response;
            }
        }

        // 设置新的连接参数
        public void UpdateConnectionSettings(string serverAddress, int serverPort)
        {
            this.serverAddress = serverAddress;
            this.serverPort = serverPort;
        }
    }
}
