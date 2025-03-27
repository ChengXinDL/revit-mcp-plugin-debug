using System;
using System.Net.Sockets;
using System.Text;

namespace RevitSocketClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Revit Socket 客户端");

            try
            {
                TcpClient client = new TcpClient("localhost", 8080);
                NetworkStream stream = client.GetStream();

                //// 创建符合JSON-RPC 2.0协议的墙体创建请求
                //string jsonRpcRequest = @"{
                //    ""jsonrpc"": ""2.0"",
                //    ""method"": ""get_available_family_types"",
                //    ""params"": {
                //          ""familyNameFilter"": ""墙""
                //        },
                //    ""id"": 1
                //}";

                // 创建符合JSON-RPC 2.0协议的墙体创建请求
                string jsonRpcRequest = @"{
                    ""command"": ""command1"",
                    ""parameters"": {},
                }";

                Console.WriteLine("发送请求: " + jsonRpcRequest);

                // 发送命令
                byte[] data = Encoding.UTF8.GetBytes(jsonRpcRequest);
                stream.Write(data, 0, data.Length);

                // 接收响应
                data = new byte[4096];
                int bytes = stream.Read(data, 0, data.Length);
                string response = Encoding.UTF8.GetString(data, 0, bytes);

                Console.WriteLine("服务器响应: " + response);

                // 关闭客户端
                client.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("错误: " + e.Message);
            }

            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}