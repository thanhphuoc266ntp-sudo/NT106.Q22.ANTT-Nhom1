using RemoteMate;
using RemoteMate.Models;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class NetworkService
{
    private UdpClient udp;
    private bool _running;

    public event Action<ClientInfo> OnClientFound;

    public void StartDiscovery()
    {
        _running = true;

        udp = new UdpClient(8888);
        udp.EnableBroadcast = true;

        Task.Run(Listen);
        Task.Run(Broadcast);
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
                string msg = UserSession.HostName ?? "Unknown";
                byte[] data = Encoding.UTF8.GetBytes(msg);

                await sender.SendAsync(data, data.Length,
                    new IPEndPoint(IPAddress.Broadcast, 8888));

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

                var client = new ClientInfo
                {
                    Ip = result.RemoteEndPoint.Address.ToString(),
                    HostName = Encoding.UTF8.GetString(result.Buffer),
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