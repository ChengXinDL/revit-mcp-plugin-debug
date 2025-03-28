using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
//using Newtonsoft.Json;

namespace RevitSocketClient
{
    //class Program
    //{
    //    static void Main(string[] args)
    //    {
    //        Console.WriteLine("Revit Socket 客户端 (调试模式)");

    //        while (true)
    //        {
    //            TcpClient client = null;
    //            NetworkStream stream = null;

    //            try
    //            {
    //                // 连接到服务器
    //                client = new TcpClient("localhost", 8080);
    //                stream = client.GetStream();
    //                stream.ReadTimeout = 5000; // 设置接收超时为 5 秒

    //                Console.WriteLine("已连接到服务器");

    //                // 提示用户选择命令
    //                Console.WriteLine("请选择要执行的命令：");
    //                Console.WriteLine("1: 创建墙体");
    //                Console.WriteLine("2: 获取当前视图元素");
    //                Console.WriteLine("3: 从文件加载请求");
    //                Console.WriteLine("4: 批量测试模式");
    //                Console.WriteLine("exit: 退出程序");
    //                string choice = Console.ReadLine();

    //                // 如果用户输入 "exit"，退出程序
    //                if (choice.ToLower() == "exit")
    //                {
    //                    Console.WriteLine("退出程序...");
    //                    break;
    //                }

    //                string jsonRpcRequest;

    //                // 使用 switch 根据用户输入选择请求
    //                switch (choice)
    //                {
    //                    case "1": // 创建墙体
    //                        Console.WriteLine("请输入墙体参数（格式：startX,startY,endX,endY,height,thickness）:");
    //                        string input = Console.ReadLine();
    //                        string[] parts = input.Split(',');

    //                        // 验证输入参数
    //                        if (parts.Length != 6)
    //                        {
    //                            Console.WriteLine("参数数量错误！使用默认参数");
    //                            parts = new[] { "0", "0", "20", "0", "10", "0.3" };
    //                        }

    //                        foreach (var part in parts)
    //                        {
    //                            if (!double.TryParse(part, out _))
    //                            {
    //                                Console.WriteLine("参数必须为数字！使用默认参数");
    //                                parts = new[] { "0", "0", "20", "0", "10", "0.3" };
    //                                break;
    //                            }
    //                        }

    //                        jsonRpcRequest = $@"{{
    //                            ""jsonrpc"": ""2.0"",
    //                            ""method"": ""createWall"",
    //                            ""params"": {{
    //                                ""startX"": {parts[0]},
    //                                ""startY"": {parts[1]},
    //                                ""endX"": {parts[2]},
    //                                ""endY"": {parts[3]},
    //                                ""height"": {parts[4]},
    //                                ""thickness"": {parts[5]}
    //                            }},
    //                            ""id"": 1
    //                        }}";
    //                        break;

    //                    case "2": // 获取当前视图元素
    //                        jsonRpcRequest = @"{
    //                            ""jsonrpc"": ""2.0"",
    //                            ""method"": ""get_current_view_elements"",
    //                            ""params"": {
    //                                ""limit"": 50,
    //                                ""includeHidden"": false,
    //                                ""modelCategoryList"": [
    //                                    ""OST_Walls"",
    //                                    ""OST_Doors"",
    //                                    ""OST_Windows"",
    //                                    ""OST_Floors""
    //                                ]
    //                            },
    //                            ""id"": 1
    //                        }";
    //                        break;

    //                    case "3": // 从文件加载请求
    //                        Console.WriteLine("请输入 JSON 文件路径（例如：request.json）:");
    //                        string filePath = Console.ReadLine();
    //                        if (File.Exists(filePath))
    //                        {
    //                            jsonRpcRequest = File.ReadAllText(filePath);
    //                        }
    //                        else
    //                        {
    //                            Console.WriteLine("文件不存在，使用默认请求");
    //                            jsonRpcRequest = @"{
    //                                ""jsonrpc"": ""2.0"",
    //                                ""method"": ""get_current_view_elements"",
    //                                ""params"": {
    //                                    ""limit"": 50,
    //                                    ""includeHidden"": false,
    //                                    ""modelCategoryList"": [
    //                                        ""OST_Walls"",
    //                                        ""OST_Doors"",
    //                                        ""OST_Windows"",
    //                                        ""OST_Floors""
    //                                    ]
    //                                },
    //                                ""id"": 1
    //                            }";
    //                        }
    //                        break;

    //                    case "4": // 批量测试模式
    //                        string[] testRequests = {
    //                            @"{
    //                                ""jsonrpc"": ""2.0"",
    //                                ""method"": ""createWall"",
    //                                ""params"": {
    //                                    ""startX"": 0,
    //                                    ""startY"": 0,
    //                                    ""endX"": 20,
    //                                    ""endY"": 0,
    //                                    ""height"": 10,
    //                                    ""thickness"": 0.3
    //                                },
    //                                ""id"": 1
    //                            }",
    //                            @"{
    //                                ""jsonrpc"": ""2.0"",
    //                                ""method"": ""get_current_view_elements"",
    //                                ""params"": {
    //                                    ""limit"": 50,
    //                                    ""includeHidden"": false,
    //                                    ""modelCategoryList"": [
    //                                        ""OST_Walls"",
    //                                        ""OST_Doors"",
    //                                        ""OST_Windows"",
    //                                        ""OST_Floors""
    //                                    ]
    //                                },
    //                                ""id"": 1
    //                            }"
    //                        };

    //                        foreach (var request in testRequests)
    //                        {
    //                            Console.WriteLine("发送请求: " + request);
    //                            byte[] testData = Encoding.UTF8.GetBytes(request);
    //                            stream.Write(testData, 0, testData.Length);

    //                            testData = new byte[4096];
    //                            int testBytes = stream.Read(testData, 0, testData.Length);
    //                            string testResponse = Encoding.UTF8.GetString(testData, 0, testBytes);
    //                            Console.WriteLine("服务器响应:\n" + JsonConvert.SerializeObject(JsonConvert.DeserializeObject(testResponse), Formatting.Indented));
    //                        }
    //                        continue;

    //                    default:
    //                        Console.WriteLine("无效的选择，使用默认命令：获取当前视图元素");
    //                        jsonRpcRequest = @"{
    //                            ""jsonrpc"": ""2.0"",
    //                            ""method"": ""get_current_view_elements"",
    //                            ""params"": {
    //                                ""limit"": 50,
    //                                ""includeHidden"": false,
    //                                ""modelCategoryList"": [
    //                                    ""OST_Walls"",
    //                                    ""OST_Doors"",
    //                                    ""OST_Windows"",
    //                                    ""OST_Floors""
    //                                ]
    //                            },
    //                            ""id"": 1
    //                        }";
    //                        break;
    //                }

    //                Console.WriteLine("发送请求: " + jsonRpcRequest);

    //                // 发送命令
    //                byte[] data = Encoding.UTF8.GetBytes(jsonRpcRequest);
    //                stream.Write(data, 0, data.Length);

    //                // 接收响应
    //                data = new byte[4096];
    //                int bytes = stream.Read(data, 0, data.Length);
    //                string response = Encoding.UTF8.GetString(data, 0, bytes);

    //                // 格式化并输出响应
    //                string prettyResponse = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(response), Formatting.Indented);
    //                Console.WriteLine("服务器响应:\n" + prettyResponse);

    //                // 记录日志
    //                string log = $"[{DateTime.Now}] 请求:\n{jsonRpcRequest}\n响应:\n{prettyResponse}\n\n";
    //                File.AppendAllText("debug.log", log);
    //                Console.WriteLine("日志已保存到 debug.log");
    //            }
    //            catch (TimeoutException)
    //            {
    //                Console.WriteLine("错误: 服务器响应超时");
    //            }
    //            catch (SocketException e)
    //            {
    //                Console.WriteLine($"连接失败: {e.Message}");
    //                Console.WriteLine("按 R 重试，其他键退出");
    //                if (Console.ReadKey().Key != ConsoleKey.R) break;
    //            }
    //            catch (Exception e)
    //            {
    //                Console.WriteLine("错误: " + e.Message);
    //            }
    //            finally
    //            {
    //                client?.Close();
    //            }

    //            Console.WriteLine("按任意键继续...");
    //            Console.ReadKey();
    //        }

    //        Console.WriteLine("程序已退出。");
    //    }
    //}
}