using RemoteMate.Models;
using System.IO;
using System.Text.Json;

namespace RemoteMate.Services
{
    public static class AppSettingsService
    {
        private static readonly string SettingsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RemoteMate"
        );

        private static readonly string SettingsFile = Path.Combine(SettingsFolder, "settings.json");

        public static AppSettings Current { get; private set; } = new AppSettings();

        public static void Load()
        {
            try
            {
                Directory.CreateDirectory(SettingsFolder);

                if (!File.Exists(SettingsFile))
                {
                    Current = new AppSettings();
                    Save();
                    return;
                }

                string json = File.ReadAllText(SettingsFile);
                Current = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                Current = new AppSettings();
            }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(SettingsFolder);

                string json = JsonSerializer.Serialize(Current, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(SettingsFile, json);
            }
            catch
            {
            }
        }

        public static string GetDefaultDownloadsFolder()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads"
            );
        }

        public static string GetScreenshotFolder()
        {
            if (!string.IsNullOrWhiteSpace(Current.ScreenshotFolder))
                return Current.ScreenshotFolder;

            return GetDefaultDownloadsFolder();
        }

        public static string GetReceivedFileFolder()
        {
            if (!string.IsNullOrWhiteSpace(Current.ReceivedFileFolder))
                return Current.ReceivedFileFolder;

            return GetDefaultDownloadsFolder();
        }
    }
}