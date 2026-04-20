using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteMate.Models
{
    public class ClientInfo
    {
        public string Ip { get; set; } = string.Empty;

        public string HostName { get; set; } = string.Empty;

        public DateTime LastSeen { get; set; } = DateTime.Now;
    }
}