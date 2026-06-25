namespace RemoteMate.Models
{
    public class AppSettings
    {
        public bool AudioEnabled { get; set; } = true;
        public bool ClipboardSyncEnabled { get; set; } = true;
        public string ScreenshotFolder { get; set; } = string.Empty;
        public string ReceivedFileFolder { get; set; } = string.Empty;
        public int ScreenQuality { get; set; } = 60;
    }
}