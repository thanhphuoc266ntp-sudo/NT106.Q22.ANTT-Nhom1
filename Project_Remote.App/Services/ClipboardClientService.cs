using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RemoteMate.Services
{
    public class ClipboardClientService
    {
        private const int PORT = 9004;
        private const int MAX_IMAGE_BYTES = 10 * 1024 * 1024;

        private TcpClient? _client;
        private StreamWriter? _writer;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        public bool IsConnected
        {
            get
            {
                try
                {
                    return _client != null && _client.Connected && _writer != null;
                }
                catch
                {
                    return false;
                }
            }
        }

        public async Task<bool> ConnectAsync(string remoteIp)
        {
            try
            {
                Disconnect();

                _client = new TcpClient();
                await _client.ConnectAsync(IPAddress.Parse(remoteIp), PORT);

                NetworkStream stream = _client.GetStream();

                _writer = new StreamWriter(stream, new UTF8Encoding(false))
                {
                    AutoFlush = true
                };

                return true;
            }
            catch
            {
                Disconnect();
                return false;
            }
        }

        public async Task<bool> SendTextAsync(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
            return await SendLineAsync($"CLIP_TEXT|{base64}");
        }

        public async Task<bool> SendImageAsync(byte[] pngData)
        {
            if (pngData == null || pngData.Length == 0)
                return false;

            if (pngData.Length > MAX_IMAGE_BYTES)
                return false;

            string base64 = Convert.ToBase64String(pngData);
            return await SendLineAsync($"CLIP_IMAGE|{base64}");
        }

        private async Task<bool> SendLineAsync(string line)
        {
            if (_client == null || _writer == null || !_client.Connected)
                return false;

            try
            {
                await _sendLock.WaitAsync();

                try
                {
                    await _writer.WriteLineAsync(line);
                    await _writer.FlushAsync();
                }
                finally
                {
                    _sendLock.Release();
                }

                return true;
            }
            catch
            {
                Disconnect();
                return false;
            }
        }

        public void Disconnect()
        {
            try { _writer?.Close(); } catch { }
            try { _client?.Close(); } catch { }

            _writer = null;
            _client = null;
        }
    }
}