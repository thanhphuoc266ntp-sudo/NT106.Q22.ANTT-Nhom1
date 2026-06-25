using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfClipboard = System.Windows.Clipboard;

namespace RemoteMate.Services
{
    public class ClipboardSyncService
    {
        private readonly DispatcherTimer _timer;

        private uint _lastClipboardSequence = 0;
        private DateTime _suppressUntil = DateTime.MinValue;

        private const int MAX_TEXT_LENGTH = 200_000;
        private const int MAX_IMAGE_BYTES = 10 * 1024 * 1024;

        public event Action<string>? OnLocalTextChanged;
        public event Action<byte[]>? OnLocalImageChanged;

        [DllImport("user32.dll")]
        private static extern uint GetClipboardSequenceNumber();

        public ClipboardSyncService()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += Timer_Tick;
        }

        public void Start()
        {
            _lastClipboardSequence = GetClipboardSequenceNumber();
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public void ApplyRemoteText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            if (text.Length > MAX_TEXT_LENGTH)
                return;

            var dispatcher = System.Windows.Application.Current.Dispatcher;

            if (!dispatcher.CheckAccess())
            {
                dispatcher.Invoke(() => ApplyRemoteText(text));
                return;
            }

            _suppressUntil = DateTime.Now.AddMilliseconds(800);

            if (SetTextSafe(text))
                _lastClipboardSequence = GetClipboardSequenceNumber();
        }

        public void ApplyRemoteImage(byte[] pngData)
        {
            if (pngData == null || pngData.Length == 0)
                return;

            if (pngData.Length > MAX_IMAGE_BYTES)
                return;

            var dispatcher = System.Windows.Application.Current.Dispatcher;

            if (!dispatcher.CheckAccess())
            {
                dispatcher.Invoke(() => ApplyRemoteImage(pngData));
                return;
            }

            BitmapImage bitmap = new BitmapImage();

            using (MemoryStream ms = new MemoryStream(pngData))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze();
            }

            _suppressUntil = DateTime.Now.AddMilliseconds(800);

            if (SetImageSafe(bitmap))
                _lastClipboardSequence = GetClipboardSequenceNumber();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (DateTime.Now < _suppressUntil)
                return;

            uint currentSequence = GetClipboardSequenceNumber();

            if (currentSequence == _lastClipboardSequence)
                return;

            _lastClipboardSequence = currentSequence;

            if (ContainsTextSafe())
            {
                string text = GetTextSafe();

                if (!string.IsNullOrEmpty(text) && text.Length <= MAX_TEXT_LENGTH)
                    OnLocalTextChanged?.Invoke(text);

                return;
            }

            if (ContainsImageSafe())
            {
                BitmapSource? image = GetImageSafe();

                if (image == null)
                    return;

                byte[] pngData = EncodeBitmapSourceToPng(image);

                if (pngData.Length > 0 && pngData.Length <= MAX_IMAGE_BYTES)
                    OnLocalImageChanged?.Invoke(pngData);
            }
        }

        private byte[] EncodeBitmapSourceToPng(BitmapSource image)
        {
            try
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));

                using MemoryStream ms = new MemoryStream();
                encoder.Save(ms);

                return ms.ToArray();
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }

        private bool ContainsTextSafe()
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    return WpfClipboard.ContainsText();
                }
                catch
                {
                    Thread.Sleep(20);
                }
            }

            return false;
        }

        private string GetTextSafe()
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    return WpfClipboard.GetText();
                }
                catch
                {
                    Thread.Sleep(20);
                }
            }

            return string.Empty;
        }

        private bool SetTextSafe(string text)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    WpfClipboard.SetText(text);
                    return true;
                }
                catch
                {
                    Thread.Sleep(20);
                }
            }

            return false;
        }

        private bool ContainsImageSafe()
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    return WpfClipboard.ContainsImage();
                }
                catch
                {
                    Thread.Sleep(20);
                }
            }

            return false;
        }

        private BitmapSource? GetImageSafe()
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    BitmapSource? image = WpfClipboard.GetImage();

                    if (image != null && image.CanFreeze)
                        image.Freeze();

                    return image;
                }
                catch
                {
                    Thread.Sleep(20);
                }
            }

            return null;
        }

        private bool SetImageSafe(BitmapSource image)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    WpfClipboard.SetImage(image);
                    return true;
                }
                catch
                {
                    Thread.Sleep(20);
                }
            }

            return false;
        }
    }
}