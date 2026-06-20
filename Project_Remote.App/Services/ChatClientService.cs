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

        public async Task SendMessageAsync(string remoteIp, string message)
        {
            if (string.IsNullOrWhiteSpace(remoteIp))
            {
                OnStatusChanged?.Invoke("Chưa chọn máy nhận tin nhắn.");
                return;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                OnStatusChanged?.Invoke("Tin nhắn rỗng.");
                return;
            }

            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(remoteIp, PORT);

                using var stream = client.GetStream();

                string fromIp = ((IPEndPoint)client.Client.LocalEndPoint!).Address.ToString();
                string fromUserName = UserSession.Username ?? "Unknown";

                string bIp = Convert.ToBase64String(Encoding.UTF8.GetBytes(fromIp));
                string bUser = Convert.ToBase64String(Encoding.UTF8.GetBytes(fromUserName));
                string bMsg = Convert.ToBase64String(Encoding.UTF8.GetBytes(message));

                string payload = $"CHAT|{bIp}|{bUser}|{bMsg}\n";
                byte[] data = Encoding.UTF8.GetBytes(payload);

                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();

                OnStatusChanged?.Invoke("Đã gửi tin nhắn.");
            }
            catch (Exception ex)
            {
                OnStatusChanged?.Invoke($"Lỗi gửi: {ex.Message}");
            }
        }
    }
}