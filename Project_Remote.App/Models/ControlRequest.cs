using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteMate.Models
{
    public class ControlRequest
    {
        public string FromIp { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public DateTime RequestTime { get; set; } = DateTime.Now;
    }
}