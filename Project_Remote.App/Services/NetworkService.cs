using RemoteMate;
using RemoteMate.Models;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Net.NetworkInformation;

public class NetworkService
{
    private UdpClient udp;
    private bool _running;

    public event Action<ClientInfo> OnClientFound;

    public void StartDiscovery()
    {
        _running = true;

        udp = new UdpClient();
        udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udp.Client.Bind(new IPEndPoint(IPAddress.Any, 8888));
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
                string username = UserSession.Username ?? "Unknown";
                string hostName = UserSession.HostName ?? Environment.MachineName;

                string msg = $"DISCOVER|{username}|{hostName}";
                byte[] data = Encoding.UTF8.GetBytes(msg);

                foreach (var broadcastIp in GetBroadcastAddresses())
                {
                    await sender.SendAsync(
                        data,
                        data.Length,
                        new IPEndPoint(broadcastIp, 8888)
                    );
                }

                await Task.Delay(2000);
            }
            catch { }
        }
    }

    private List<IPAddress> GetBroadcastAddresses()
    {
        var result = new List<IPAddress>();

        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;

            var props = ni.GetIPProperties();

            foreach (var ua in props.UnicastAddresses)
            {
                if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
                    continue;

                if (ua.IPv4Mask == null)
                    continue;

                byte[] ipBytes = ua.Address.GetAddressBytes();
                byte[] maskBytes = ua.IPv4Mask.GetAddressBytes();
                byte[] broadcastBytes = new byte[4];

                for (int i = 0; i < 4; i++)
                {
                    broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
                }

                result.Add(new IPAddress(broadcastBytes));
            }
        }

        result.Add(IPAddress.Broadcast);
        return result.Distinct().ToList();
    }

    private async Task Listen()
    {
        while (_running)
        {
            try
            {
                var result = await udp.ReceiveAsync();

                string message = Encoding.UTF8.GetString(result.Buffer);

                string username = string.Empty;
                string hostName = message;

                var parts = message.Split('|');

                if (parts.Length >= 3 && parts[0] == "DISCOVER")
                {
                    username = parts[1];
                    hostName = parts[2];
                }

                var client = new ClientInfo
                {
                    Ip = result.RemoteEndPoint.Address.ToString(),
                    UserName = username,
                    HostName = hostName,
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