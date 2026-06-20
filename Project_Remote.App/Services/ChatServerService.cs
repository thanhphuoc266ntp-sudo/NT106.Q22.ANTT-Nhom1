using RemoteMate.Models;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteMate.Services
{
    public class ChatServerService
    {
        private TcpListener? _listener;
        private CancellationTokenSource _cts = new();
        private volatile bool _isRunning;
        private const int PORT = 9002;

        public event Action<string>? OnStatusChanged;
        public event Action<ChatMessage>? OnMessageReceived;

        public void Start()
        {
            if (_isRunning) return;

            _isRunning = true;
            _cts = new CancellationTokenSource();

            _listener = new TcpListener(IPAddress.Any, PORT);
            _listener.Start();
            OnStatusChanged?.Invoke($"Chat server started on port {PORT}");

            Task.Run(async () =>
            {
                while (_isRunning)
                {
                    try
                    {
                        var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                        _ = Task.Run(() => HandleClient(client, _cts.Token));
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch { }
                }
            });
        }

        private async Task HandleClient(TcpClient client, CancellationToken token)
        {
            try
            {
                using (client)
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true))
                {
                    string? line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) return;

                    var parts = line.Split('|');
                    if (parts.Length != 4) return;
                    if (parts[0] != "CHAT") return;

                    try
                    {
                        string fromIp = Encoding.UTF8.GetString(Convert.FromBase64String(parts[1]));
                        string fromUser = Encoding.UTF8.GetString(Convert.FromBase64String(parts[2]));
                        string message = Encoding.UTF8.GetString(Convert.FromBase64String(parts[3]));

                        var msg = new ChatMessage
                        {
                            FromIp = fromIp,
                            FromUserName = fromUser,
                            Message = message,
                            SentAt = DateTime.Now,
                            IsMine = false
                        };

                        OnMessageReceived?.Invoke(msg);
                        OnStatusChanged?.Invoke($"Tin nhắn từ {fromUser} ({fromIp})");
                    }
                    catch
                    {
                        // Ignore malformed base64 / decoding errors
                    }
                }
            }
            catch { }
        }

        public void Stop()
        {
            _isRunning = false;
            _cts.Cancel();
            try { _listener?.Stop(); } catch { }
            OnStatusChanged?.Invoke("Chat server stopped");
        }
    }
}