using RemoteMate.Models;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RemoteMate.Services
{
    public class NetworkService
    {
        private UdpClient udp;
        private bool _running;

        public event Action<ClientInfo> OnClientFound;

        public void StartDiscovery()
        {
            try
            {
                _running = true;

                // NÂNG CẤP 1: Cho phép nhiều app dùng chung Port 8888 (rất tiện để test local)
                udp = new UdpClient();
                udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udp.Client.Bind(new IPEndPoint(IPAddress.Any, 8888));
                udp.EnableBroadcast = true;

                Task.Run(Listen);
                Task.Run(Broadcast);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khởi tạo UDP: {ex.Message}");
            }
        }

        public void Stop()
        {
            _running = false;
            udp?.Close();
        }

        private async Task Broadcast()
        {
            using var sender = new UdpClient();
            sender.EnableBroadcast = true;

            while (_running)
            {
                try
                {
                    // NÂNG CẤP 2: Dùng Environment.MachineName làm định danh để chống tự soi gương
                    string msg = Environment.MachineName;
                    byte[] data = Encoding.UTF8.GetBytes(msg);

                    await sender.SendAsync(data, data.Length, new IPEndPoint(IPAddress.Broadcast, 8888));

                    await Task.Delay(2000);
                }
                catch { }
            }
        }

        private async Task Listen()
        {
            while (_running)
            {
                try
                {
                    var result = await udp.ReceiveAsync();

                    string remoteIp = result.RemoteEndPoint.Address.ToString();
                    string receivedHostName = Encoding.UTF8.GetString(result.Buffer);

                    // NÂNG CẤP 3: LỌC BỎ CHÍNH MÌNH (Chặn hiện tượng Loopback)
                    // Nếu tên máy nhận được giống hệt tên máy mình, lập tức ngó lơ!
                    if (receivedHostName == Environment.MachineName || remoteIp == "127.0.0.1")
                    {
                        continue;
                    }

                    var client = new ClientInfo
                    {
                        Ip = remoteIp,
                        HostName = receivedHostName,
                        LastSeen = DateTime.Now
                    };

                    OnClientFound?.Invoke(client);
                }
                catch
                {
                    break;
                }
            }
        }
    }
}