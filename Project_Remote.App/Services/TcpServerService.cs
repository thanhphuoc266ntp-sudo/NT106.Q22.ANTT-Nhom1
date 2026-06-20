using RemoteMate.Models;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Security.Claims;
using RemoteMate.Services;
using System.Threading;

namespace RemoteMate.Services
{
    public class TcpServerService
    {
        private TcpListener _listener = null!;
        private CancellationTokenSource _cts = new();
        private volatile bool _isRunning;
        private const int PORT = 9000; // Đổi sang 9000 để tránh conflict với UDP 8888

        public event Action<string>? OnStatusChanged;
        public event Func<ControlRequest, Task<bool>>? OnControlRequest;

        public void Start()
        {
            _isRunning = true;
            _cts = new CancellationTokenSource();

            _listener = new TcpListener(IPAddress.Any, PORT);
            _listener.Start();
            OnStatusChanged?.Invoke($"Server started on port {PORT}");

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
                        break; // Stop() được gọi, thoát vòng lặp
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
                    // Đọc message an toàn bằng StreamReader
                    string? message = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(message)) return;
                    if (!message.StartsWith("CONTROL_REQUEST")) return;

                    var parts = message.Split('|');
                    if (parts.Length < 3) return;

                    var req = new ControlRequest
                    {
                        FromIp = parts[1],
                        UserName = parts[2]
                    };

                    // Lấy token nếu có
                    string tokenStr = parts.Length >= 4 ? parts[3] : string.Empty;

                    // Validate token (nếu có). Nếu không hợp lệ -> REJECT
                    if (!string.IsNullOrEmpty(tokenStr))
                    {
                        string? validationError;
                        var principal = AuthService.ValidateToken(tokenStr, out validationError);
                        if (principal == null)
                        {
                            byte[] res = Encoding.UTF8.GetBytes("REJECT");
                            await stream.WriteAsync(res.AsMemory(0, res.Length), token);
                            return;
                        }

                        // Optional: đảm bảo token username trùng với request username
                        var nameClaim = principal.FindFirst("name")?.Value ?? principal.FindFirst(ClaimTypes.Name)?.Value;
                        if (!string.IsNullOrEmpty(nameClaim) && nameClaim != req.UserName)
                        {
                            byte[] res = Encoding.UTF8.GetBytes("REJECT");
                            await stream.WriteAsync(res.AsMemory(0, res.Length), token);
                            return;
                        }
                    }
                    else
                    {
                        // Nếu muốn bắt buộc token, có thể REJECT ở đây. Hiện cho phép request không token.
                    }

                    bool accepted = false;
                    if (OnControlRequest != null)
                        accepted = await OnControlRequest.Invoke(req);

                    string response = accepted ? "ACCEPT" : "REJECT";
                    byte[] resBytes = Encoding.UTF8.GetBytes(response);
                    await stream.WriteAsync(resBytes.AsMemory(0, resBytes.Length), token);

                    if (!accepted) return;

                    OnStatusChanged?.Invoke($"Accepted: {req.UserName}");

                    var capture = new ScreenCaptureService();

                    while (_isRunning && client.Connected && !token.IsCancellationRequested)
                    {
                        try
                        {
                            var start = DateTime.Now;
                            byte[] img = capture.CaptureScreen(30);
                            byte[] len = BitConverter.GetBytes(img.Length);

                            await stream.WriteAsync(len.AsMemory(0, 4), token);
                            await stream.WriteAsync(img.AsMemory(0, img.Length), token);

                            int delay = 33 - (int)(DateTime.Now - start).TotalMilliseconds;
                            if (delay > 0)
                                await Task.Delay(delay, token);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch
                        {
                            break;
                        }
                    }
                }
            }
            catch { }
            finally
            {
                OnStatusChanged?.Invoke("Client disconnected");
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _cts.Cancel();      // Hủy tất cả task đang chạy
            _listener?.Stop();
            OnStatusChanged?.Invoke("Server stopped");
        }
    }
}