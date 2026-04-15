using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RemoteMate.Services
{
    public class TcpClientService
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private bool _isConnected;
        private const int PORT = 8888;

        public event Action<byte[]> OnScreenReceived;
        public event Action<string> OnStatusChanged;
        public event Action OnDisconnected;

        public async Task<bool> ConnectAsync(string ipAddress)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(ipAddress, PORT);
                _stream = _client.GetStream();
                _isConnected = true;

                OnStatusChanged?.Invoke($"Connected to {ipAddress}:{PORT}");

                _ = Task.Run(ReceiveScreenData);
                return true;
            }
            catch (Exception ex)
            {
                OnStatusChanged?.Invoke($"Connection failed: {ex.Message}");
                return false;
            }
        }

        private async Task ReceiveScreenData()
        {
            try
            {
                while (_isConnected && _client.Connected)
                {
                    byte[] lengthBuffer = new byte[4];
                    int bytesRead = await _stream.ReadAsync(lengthBuffer, 0, 4);

                    if (bytesRead != 4)
                        break;

                    int imageSize = BitConverter.ToInt32(lengthBuffer, 0);

                    byte[] imageBuffer = new byte[imageSize];
                    int totalBytesRead = 0;

                    while (totalBytesRead < imageSize)
                    {
                        int read = await _stream.ReadAsync(imageBuffer, totalBytesRead, imageSize - totalBytesRead);
                        if (read == 0)
                            break;
                        totalBytesRead += read;
                    }

                    if (totalBytesRead == imageSize)
                    {
                        OnScreenReceived?.Invoke(imageBuffer);
                    }
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged?.Invoke($"Receive error: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            _isConnected = false;
            _stream?.Close();
            _client?.Close();
            OnDisconnected?.Invoke();
            OnStatusChanged?.Invoke("Disconnected");
        }

        public bool IsConnected()
        {
            return _isConnected && _client != null && _client.Connected;
        }
    }
}