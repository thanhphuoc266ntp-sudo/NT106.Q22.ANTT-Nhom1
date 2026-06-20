using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteMate.Services
{
    public class TcpClientService
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private volatile bool _isConnected;
        private const int PORT = 9000;

        // Send lock to serialize input commands
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        public event Action<byte[]> OnScreenReceived;
        public event Action<string> OnStatusChanged;
        public event Action OnDisconnected;

        public async Task<bool> ConnectAsync(string ip)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(ip, PORT);
                _stream = _client.GetStream();

                OnStatusChanged?.Invoke($"Connected to {ip}");

                bool accepted = await SendControlRequest();

                if (!accepted)
                {
                    OnStatusChanged?.Invoke("Access Denied");
                    Disconnect();
                    return false;
                }

                OnStatusChanged?.Invoke("Access Granted");

                _isConnected = true;
                _ = Task.Run(ReceiveLoop);

                return true;
            }
            catch (Exception ex)
            {
                OnStatusChanged?.Invoke($"Error: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> SendControlRequest()
        {
            try
            {
                // Gửi token kèm theo (nếu có)
                string token = UserSession.AccessToken ?? string.Empty;
                string msg = string.IsNullOrEmpty(token)
                    ? $"CONTROL_REQUEST|{UserSession.IpAddress}|{UserSession.Username}\n"
                    : $"CONTROL_REQUEST|{UserSession.IpAddress}|{UserSession.Username}|{token}\n";
                byte[] data = Encoding.UTF8.GetBytes(msg);

                await _stream.WriteAsync(data, 0, data.Length);

                // Đọc chính xác 6 byte trả lời ("ACCEPT" hoặc "REJECT") với timeout 5000 ms
                byte[] buffer = new byte[6];
                var readTask = ReadExact(buffer, 6);
                if (await Task.WhenAny(readTask, Task.Delay(5000)) != readTask)
                    return false; // timeout

                bool ok = readTask.Result;
                if (!ok) return false;

                string response = Encoding.UTF8.GetString(buffer, 0, 6);
                return response == "ACCEPT";
            }
            catch
            {
                return false;
            }
        }

        // NEW: Send input command (thread-safe, ensures newline and flush)
        public async Task SendInputAsync(string command)
        {
            if (_stream == null || string.IsNullOrWhiteSpace(command)) return;
            if (!_client?.Connected ?? true) return;

            await _sendLock.WaitAsync();
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(command + "\n");
                await _stream.WriteAsync(data, 0, data.Length);
                await _stream.FlushAsync();
            }
            catch
            {
                // swallow - caller can observe disconnection via events
            }
            finally
            {
                _sendLock.Release();
            }
        }

        // NEW: convenience helpers
        public Task SendMouseMoveAsync(int x, int y) =>
            SendInputAsync($"INPUT|MOVE|{x}|{y}");

        public Task SendMouseDownAsync(string button, int x, int y) =>
            SendInputAsync($"INPUT|DOWN|{button}|{x}|{y}");

        public Task SendMouseUpAsync(string button, int x, int y) =>
            SendInputAsync($"INPUT|UP|{button}|{x}|{y}");

        public Task SendMouseWheelAsync(int delta, int x, int y) =>
            SendInputAsync($"INPUT|WHEEL|{delta}|{x}|{y}");

        public Task SendKeyDownAsync(int virtualKey) =>
            SendInputAsync($"INPUT|KEYDOWN|{virtualKey}");

        public Task SendKeyUpAsync(int virtualKey) =>
            SendInputAsync($"INPUT|KEYUP|{virtualKey}");

        private async Task<bool> ReadExact(byte[] buffer, int size)
        {
            int total = 0;
            while (total < size)
            {
                int read = await _stream.ReadAsync(buffer, total, size - total);
                if (read == 0) return false;
                total += read;
            }
            return true;
        }

        private async Task ReceiveLoop()
        {
            try
            {
                while (_isConnected && _client.Connected)
                {
                    byte[] lenBuf = new byte[4];
                    bool ok = await ReadExact(lenBuf, 4);
                    if (!ok) break;

                    int size = BitConverter.ToInt32(lenBuf, 0);

                    if (size <= 0 || size > 10_000_000)
                        break;

                    byte[] img = new byte[size];
                    ok = await ReadExact(img, size);
                    if (!ok) break;

                    OnScreenReceived?.Invoke(img);
                }
            }
            catch { }
            finally
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (!_isConnected)
            {
                // Even if not marked as connected, close resources to ensure socket is released.
                try { _stream?.Close(); } catch { }
                try { _client?.Close(); } catch { }
                OnDisconnected?.Invoke();
                OnStatusChanged?.Invoke("Disconnected");
                return;
            }

            _isConnected = false;

            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }

            OnDisconnected?.Invoke();
            OnStatusChanged?.Invoke("Disconnected");
        }
    }
}