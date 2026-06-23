using System;

namespace RemoteMate.Models
{
    public class SessionHistoryItem
    {
        public int Id { get; set; }

        public string OwnerUsername { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public string RemoteUsername { get; set; } = string.Empty;

        public string RemoteHostName { get; set; } = string.Empty;

        public string RemoteIp { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public int DurationSeconds { get; set; }

        public string Status { get; set; } = string.Empty;

        public string Note { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public string DurationText
        {
            get
            {
                if (DurationSeconds <= 0)
                    return "Dưới 1 phút";

                int minutes = DurationSeconds / 60;
                int seconds = DurationSeconds % 60;

                if (minutes == 0)
                    return $"{seconds} giây";

                return $"{minutes} phút {seconds} giây";
            }
        }

        public string DisplayText
        {
            get
            {
                string roleText = Role == "Controller"
                    ? "Điều khiển"
                    : "Được điều khiển bởi";

                string remote = GetRemoteDisplayName();

                string statusText = Status switch
                {
                    "Completed" => "Thành công",
                    "Rejected" => "Bị từ chối",
                    "Failed" => "Lỗi kết nối",
                    "Disconnected" => "Đã ngắt",
                    _ => Status
                };

                return $"{StartTime:HH:mm} - {roleText} {remote} • {statusText}";
            }
        }

        private string GetRemoteDisplayName()
        {
            bool hasUsername = !string.IsNullOrWhiteSpace(RemoteUsername);
            bool hasHostName = !string.IsNullOrWhiteSpace(RemoteHostName);

            if (hasUsername && hasHostName)
                return $"{RemoteUsername} / {RemoteHostName}";

            if (hasUsername)
                return RemoteUsername;

            if (hasHostName)
                return RemoteHostName;

            if (!string.IsNullOrWhiteSpace(RemoteIp))
                return RemoteIp;

            return "Không xác định";
        }
    }
}