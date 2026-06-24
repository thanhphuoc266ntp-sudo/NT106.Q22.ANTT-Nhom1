using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RemoteMate.Services
{
    public class ClipboardServerService
    {
        private const int PORT = 9004;
        private const int MAX_TEXT_LENGTH = 200_000;
        private const int MAX_IMAGE_BYTES = 10 * 1024 * 1024;
        private const int MAX_IMAGE_BASE64_LENGTH = 15 * 1024 * 1024;

        private TcpListener? _listener;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private volatile bool _isRunning;

        public event Action<string>? OnClipboardTextReceived;
        public event Action<byte[]>? OnClipboardImageReceived;

        public void Start()
        {
            if (_isRunning)
                return;

            _isRunning = true;
            _cts = new CancellationTokenSource();

            _listener = new TcpListener(IPAddress.Any, PORT);
            _listener.Start();

            Task.Run(async () =>
            {
                while (_isRunning && !_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        TcpClient client = await _listener.AcceptTcpClientAsync(_cts.Token);
                        _ = Task.Run(() => HandleClientAsync(client, _cts.Token));
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                    }
                }
            });
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    while (_isRunning && client.Connected && !token.IsCancellationRequested)
                    {
                        string? line = await reader.ReadLineAsync();

                        if (line == null)
                            break;

                        HandleLine(line);
                    }
                }
            }
            catch
            {
            }
        }

        private void HandleLine(string line)
        {
            try
            {
                if (line.StartsWith("CLIP_TEXT|"))
                {
                    HandleText(line);
                }
                else if (line.StartsWith("CLIP_IMAGE|"))
                {
                    HandleImage(line);
                }
            }
            catch
            {
            }
        }

        private void HandleText(string line)
        {
            int index = line.IndexOf('|');

            if (index < 0 || index >= line.Length - 1)
                return;

            string payload = line.Substring(index + 1);

            string text = Encoding.UTF8.GetString(
                Convert.FromBase64String(payload)
            );

            if (string.IsNullOrEmpty(text))
                return;

            if (text.Length > MAX_TEXT_LENGTH)
                return;

            OnClipboardTextReceived?.Invoke(text);
        }

        private void HandleImage(string line)
        {
            int index = line.IndexOf('|');

            if (index < 0 || index >= line.Length - 1)
                return;

            string payload = line.Substring(index + 1);

            if (payload.Length > MAX_IMAGE_BASE64_LENGTH)
                return;

            byte[] imageBytes = Convert.FromBase64String(payload);

            if (imageBytes.Length <= 0 || imageBytes.Length > MAX_IMAGE_BYTES)
                return;

            OnClipboardImageReceived?.Invoke(imageBytes);
        }

        public void Stop()
        {
            _isRunning = false;

            try { _cts.Cancel(); } catch { }
            try { _listener?.Stop(); } catch { }
        }
    }
}