using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RemoteMate.Services
{
    public class ChatClientService
    {
        private const int PORT = 9002;

        public event Action<string>? OnStatusChanged;

        public async Task<bool> SendMessageAsync(string remoteIp, string message)
        {
            if (string.IsNullOrWhiteSpace(remoteIp))
            {
                OnStatusChanged?.Invoke("Chưa chọn máy nhận tin nhắn.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                OnStatusChanged?.Invoke("Tin nhắn rỗng.");
                return false;
            }

            try
            {
                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(remoteIp, PORT);

                    using (NetworkStream stream = client.GetStream())
                    {
                        string fromIp = ((IPEndPoint)client.Client.LocalEndPoint!).Address.ToString();
                        string fromUserName = UserSession.Username ?? "Unknown";

                        string payload =
                            "CHAT|" +
                            ToBase64(fromIp) + "|" +
                            ToBase64(fromUserName) + "|" +
                            ToBase64(message) + "\n";

                        byte[] data = Encoding.UTF8.GetBytes(payload);

                        await stream.WriteAsync(data, 0, data.Length);
                        await stream.FlushAsync();
                    }
                }

                OnStatusChanged?.Invoke("Đã gửi tin nhắn.");
                return true;
            }
            catch (Exception ex)
            {
                OnStatusChanged?.Invoke("Lỗi gửi tin nhắn: " + ex.Message);
                return false;
            }
        }

        public async Task<bool> SendFileAsync(string remoteIp, string filePath)
        {
            if (string.IsNullOrWhiteSpace(remoteIp))
            {
                OnStatusChanged?.Invoke("Chưa chọn máy nhận file.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                OnStatusChanged?.Invoke("File không tồn tại.");
                return false;
            }

            try
            {
                FileInfo fileInfo = new FileInfo(filePath);

                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(remoteIp, PORT);

                    using (NetworkStream stream = client.GetStream())
                    using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        string fromIp = ((IPEndPoint)client.Client.LocalEndPoint!).Address.ToString();
                        string fromUserName = UserSession.Username ?? "Unknown";
                        string fileName = Path.GetFileName(filePath);
                        long fileSize = fileInfo.Length;

                        string header =
                            "FILE|" +
                            ToBase64(fromIp) + "|" +
                            ToBase64(fromUserName) + "|" +
                            ToBase64(fileName) + "|" +
                            fileSize + "\n";

                        byte[] headerBytes = Encoding.UTF8.GetBytes(header);
                        await stream.WriteAsync(headerBytes, 0, headerBytes.Length);

                        byte[] buffer = new byte[81920];
                        int read;

                        while ((read = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await stream.WriteAsync(buffer, 0, read);
                        }

                        await stream.FlushAsync();
                    }
                }

                OnStatusChanged?.Invoke("Đã gửi file.");
                return true;
            }
            catch (Exception ex)
            {
                OnStatusChanged?.Invoke("Lỗi gửi file: " + ex.Message);
                return false;
            }
        }

        private string ToBase64(string text)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
        }
    }
}