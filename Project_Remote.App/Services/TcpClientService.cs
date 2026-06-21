using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace RemoteMate.Services
{
    public class TcpClientService
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private volatile bool _isConnected;
        private const int PORT = 9000;

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
                string localIp = ((System.Net.IPEndPoint)_client.Client.LocalEndPoint!).Address.ToString();
                string hostName = UserSession.HostName ?? Environment.MachineName;
                string username = UserSession.Username ?? "Unknown";
                string msg = $"CONTROL_REQUEST|{localIp}|{username}|{hostName}\n";
                byte[] data = Encoding.UTF8.GetBytes(msg);

                await _stream.WriteAsync(data, 0, data.Length);

                byte[] responseBuffer = new byte[6];

                var readTask = ReadExact(responseBuffer, 6);
                if (await Task.WhenAny(readTask, Task.Delay(5000)) != readTask)
                    return false;

                if (!readTask.Result)
                    return false;

                string response = Encoding.UTF8.GetString(responseBuffer).Trim();

                return response == "ACCEPT";
            }
            catch
            {
                return false;
            }
        }

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

        public async Task SendInputAsync(string command)
        {
            if (!_isConnected || _stream == null)
                return;

            byte[] data = Encoding.UTF8.GetBytes(command + "\n");

            await _sendLock.WaitAsync();

            try
            {
                await _stream.WriteAsync(data, 0, data.Length);
                await _stream.FlushAsync();
            }
            catch
            {
                Disconnect();
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public Task SendMouseMoveAsync(int x, int y)
        {
            return SendInputAsync($"INPUT|MOVE|{x}|{y}");
        }

        public Task SendMouseDownAsync(string button, int x, int y)
        {
            return SendInputAsync($"INPUT|DOWN|{button}|{x}|{y}");
        }

        public Task SendMouseUpAsync(string button, int x, int y)
        {
            return SendInputAsync($"INPUT|UP|{button}|{x}|{y}");
        }

        public Task SendMouseWheelAsync(int delta, int x, int y)
        {
            return SendInputAsync($"INPUT|WHEEL|{delta}|{x}|{y}");
        }

        public Task SendKeyDownAsync(int virtualKey)
        {
            return SendInputAsync($"INPUT|KEYDOWN|{virtualKey}");
        }

        public Task SendKeyUpAsync(int virtualKey)
        {
            return SendInputAsync($"INPUT|KEYUP|{virtualKey}");
        }

        public void Disconnect()
        {
            if (!_isConnected) return;

            _isConnected = false;

            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }

            OnDisconnected?.Invoke();
            OnStatusChanged?.Invoke("Disconnected");
        }
    }
}