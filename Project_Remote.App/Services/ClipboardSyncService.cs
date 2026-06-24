using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows;
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
            try
            {
                if (string.IsNullOrEmpty(text))
                    return;

                if (text.Length > MAX_TEXT_LENGTH)
                    return;

                _suppressUntil = DateTime.Now.AddMilliseconds(800);

                WpfClipboard.SetText(text);

                _lastClipboardSequence = GetClipboardSequenceNumber();
            }
            catch
            {
            }
        }

        public void ApplyRemoteImage(byte[] pngData)
        {
            try
            {
                if (pngData == null || pngData.Length == 0)
                    return;

                if (pngData.Length > MAX_IMAGE_BYTES)
                    return;

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

                WpfClipboard.SetImage(bitmap);

                _lastClipboardSequence = GetClipboardSequenceNumber();
            }
            catch
            {
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (DateTime.Now < _suppressUntil)
                    return;

                uint currentSequence = GetClipboardSequenceNumber();

                if (currentSequence == _lastClipboardSequence)
                    return;

                _lastClipboardSequence = currentSequence;

                if (WpfClipboard.ContainsText())
                {
                    string text = WpfClipboard.GetText();

                    if (!string.IsNullOrEmpty(text) && text.Length <= MAX_TEXT_LENGTH)
                    {
                        OnLocalTextChanged?.Invoke(text);
                    }

                    return;
                }

                if (WpfClipboard.ContainsImage())
                {
                    BitmapSource? image = WpfClipboard.GetImage();

                    if (image == null)
                        return;

                    byte[] pngData = EncodeBitmapSourceToPng(image);

                    if (pngData.Length <= 0 || pngData.Length > MAX_IMAGE_BYTES)
                        return;

                    OnLocalImageChanged?.Invoke(pngData);
                }
            }
            catch
            {
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
    }
}