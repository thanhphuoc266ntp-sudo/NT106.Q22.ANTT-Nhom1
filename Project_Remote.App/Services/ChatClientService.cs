using System;
using System.Net.Sockets;
using System.Text;
using System.Net;
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
                        string fromIp = ((System.Net.IPEndPoint)client.Client.LocalEndPoint).Address.ToString();
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

        private string ToBase64(string text)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
        }
    }
}