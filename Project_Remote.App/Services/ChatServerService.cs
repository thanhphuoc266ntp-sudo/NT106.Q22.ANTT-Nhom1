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

            OnStatusChanged?.Invoke($"Chat/File server started on port {PORT}");

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
                {
                    string? line = await ReadLineAsync(stream, token);

                    if (string.IsNullOrWhiteSpace(line))
                        return;

                    string[] parts = line.Split('|');

                    if (parts[0] == "CHAT")
                    {
                        HandleChat(parts);
                    }
                    else if (parts[0] == "FILE")
                    {
                        await HandleFile(parts, stream, token);
                    }
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged?.Invoke("Lỗi nhận chat/file: " + ex.Message);
            }
        }

        private void HandleChat(string[] parts)
        {
            if (parts.Length != 4)
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

        private async Task HandleFile(string[] parts, NetworkStream stream, CancellationToken token)
        {
            if (parts.Length != 5)
                return;

            string fromIp = FromBase64(parts[1]);
            string fromUserName = FromBase64(parts[2]);
            string fileName = FromBase64(parts[3]);
            long fileSize = long.Parse(parts[4]);

            fileName = Path.GetFileName(fileName);

            string saveFolder = AppSettingsService.GetReceivedFileFolder();

            Directory.CreateDirectory(saveFolder);

            string savePath = GetUniqueFilePath(Path.Combine(saveFolder, fileName));

            using (FileStream fs = new FileStream(savePath, FileMode.CreateNew, FileAccess.Write))
            {
                byte[] buffer = new byte[81920];
                long totalRead = 0;

                while (totalRead < fileSize)
                {
                    int toRead = (int)Math.Min(buffer.Length, fileSize - totalRead);
                    int read = await stream.ReadAsync(buffer, 0, toRead, token);

                    if (read == 0)
                        break;

                    await fs.WriteAsync(buffer, 0, read, token);
                    totalRead += read;
                }
            }

            ChatMessage fileMessage = new ChatMessage
            {
                FromIp = fromIp,
                FromUserName = fromUserName,
                Message = $"Đã nhận file: {fileName}\nLưu tại: {savePath}",
                SentAt = DateTime.Now,
                IsMine = false
            };

            OnMessageReceived?.Invoke(fileMessage);
            OnStatusChanged?.Invoke($"Nhận file từ {fromUserName}: {fileName}");
        }

        private async Task<string?> ReadLineAsync(NetworkStream stream, CancellationToken token)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                byte[] buffer = new byte[1];

                while (true)
                {
                    int read = await stream.ReadAsync(buffer, 0, 1, token);

                    if (read == 0)
                        break;

                    if (buffer[0] == '\n')
                        break;

                    ms.WriteByte(buffer[0]);
                }

                return Encoding.UTF8.GetString(ms.ToArray()).Trim();
            }
        }

        private string FromBase64(string base64)
        {
            byte[] bytes = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(bytes);
        }

        private string GetUniqueFilePath(string path)
        {
            if (!File.Exists(path))
                return path;

            string directory = Path.GetDirectoryName(path) ?? "";
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);

            int count = 1;

            while (true)
            {
                string newPath = Path.Combine(directory, $"{fileNameWithoutExt} ({count}){extension}");

                if (!File.Exists(newPath))
                    return newPath;

                count++;
            }
        }

        public void Stop()
        {
            _isRunning = false;

            try { _cts.Cancel(); } catch { }
            try { _listener?.Stop(); } catch { }

            OnStatusChanged?.Invoke("Chat/File server stopped");
        }
    }
}