using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RemoteMate.Models;

namespace RemoteMate.Services
{
    public class ChatServerService
    {
        private TcpListener _listener;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private volatile bool _isRunning;

        private const int PORT = 9002;

        public event Action<ChatMessage>? OnMessageReceived;
        public event Action<string>? OnStatusChanged;

        public void Start()
        {
            if (_isRunning)
                return;

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
                        TcpClient client = await _listener.AcceptTcpClientAsync(_cts.Token);
                        _ = Task.Run(() => HandleClient(client, _cts.Token));
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

        private async Task HandleClient(TcpClient client, CancellationToken token)
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string? line = await reader.ReadLineAsync();

                    if (string.IsNullOrWhiteSpace(line))
                        return;

                    string[] parts = line.Split('|');

                    if (parts.Length != 4 || parts[0] != "CHAT")
                        return;

                    string fromIp = FromBase64(parts[1]);
                    string fromUserName = FromBase64(parts[2]);
                    string message = FromBase64(parts[3]);

                    ChatMessage chatMessage = new ChatMessage
                    {
                        FromIp = fromIp,
                        FromUserName = fromUserName,
                        Message = message,
                        SentAt = DateTime.Now,
                        IsMine = false
                    };

                    OnMessageReceived?.Invoke(chatMessage);
                    OnStatusChanged?.Invoke($"Nhận tin nhắn từ {fromUserName}");
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged?.Invoke("Lỗi nhận tin nhắn: " + ex.Message);
            }
        }

        private string FromBase64(string base64)
        {
            byte[] bytes = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(bytes);
        }

        public void Stop()
        {
            _isRunning = false;

            try { _cts.Cancel(); } catch { }
            try { _listener?.Stop(); } catch { }

            OnStatusChanged?.Invoke("Chat server stopped");
        }
    }
}