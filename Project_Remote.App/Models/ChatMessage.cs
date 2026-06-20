using System;

namespace RemoteMate.Models
{
    public class ChatMessage
    {
        public string FromIp { get; set; } = string.Empty;

        public string FromUserName { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.Now;

        public bool IsMine { get; set; }
    }
}