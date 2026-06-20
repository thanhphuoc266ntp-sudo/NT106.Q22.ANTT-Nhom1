using System.Linq;

namespace RemoteMate
{
    public static class UserSession
    {
        public static string? FullName { get; set; }
        public static string? Email { get; set; }
        public static string? Username { get; set; }

        public static string? IpAddress { get; set; }
        public static string? HostName { get; set; }

        // Lưu access token trong session (nullable)
        public static string? AccessToken { get; set; }

        public static void InitNetworkInfo()
        {
            HostName = System.Environment.MachineName;

            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            IpAddress = host.AddressList
                .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                ?.ToString();
        }

        public static void Clear()
        {
            FullName = null;
            Email = null;
            Username = null;
            IpAddress = null;   
            HostName = null;
            AccessToken = null;
        }
    }
}