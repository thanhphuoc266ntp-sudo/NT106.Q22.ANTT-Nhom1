using System;

namespace RemoteMate.Models
{
    public class ControlRequest
    {
        public string FromIp { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string HostName { get; set; } = string.Empty;

        public DateTime RequestTime { get; set; } = DateTime.Now;
    }
}