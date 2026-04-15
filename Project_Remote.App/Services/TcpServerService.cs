using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RemoteMate.Services
{
    public class TcpServerService
    {
        private TcpListener _listener;
        private bool _isRunning;
        private const int PORT = 8888;

        public event Action<string> OnStatusChanged;
        public event Action<byte[]> OnScreenDataReceived;

        public void Start()
        {
            _isRunning = true;
            _listener = new TcpListener(IPAddress.Any, PORT);
            _listener.Start();

            OnStatusChanged?.Invoke($"Server started on port {PORT}");

            Task.Run(async () =>
            {
                while (_isRunning)
                {
                    try
                    {
                        TcpClient client = await _listener.AcceptTcpClientAsync();
                        OnStatusChanged?.Invoke($"Client connected: {client.Client.RemoteEndPoint}");

                        _ = Task.Run(() => HandleClient(client));
                    }
                    catch (Exception ex)
                    {
                        if (_isRunning)
                            OnStatusChanged?.Invoke($"Error: {ex.Message}");
                    }
                }
            });
        }

        private async Task HandleClient(TcpClient client)
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                {
                    ScreenCaptureService capture = new ScreenCaptureService();

                    while (_isRunning && client.Connected)
                    {
                        byte[] screenData = capture.CaptureScreen(30);

                        byte[] lengthPrefix = BitConverter.GetBytes(screenData.Length);
                        await stream.WriteAsync(lengthPrefix, 0, 4);
                        await stream.WriteAsync(screenData, 0, screenData.Length);

                        await Task.Delay(33);
                    }
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged?.Invoke($"Client disconnected: {ex.Message}");
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
        }
    }
}