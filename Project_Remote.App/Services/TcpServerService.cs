using RemoteMate.Models;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RemoteMate.Services
{
    public class TcpServerService
    {
        private TcpListener _listener = null!;
        private CancellationTokenSource _cts = new();
        private volatile bool _isRunning;
        private const int PORT = 9000;
        public event Action<string>? OnStatusChanged;
        public event Func<ControlRequest, Task<bool>>? OnControlRequest;
        public event Action<ControlRequest>? OnSessionAccepted;
        public event Action<ControlRequest>? OnSessionRejected;
        public event Action<ControlRequest>? OnSessionEnded;

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
                        break; 
                    }
                    catch { }
                }
            });
        }

        private async Task HandleClient(TcpClient client, CancellationToken token)
        {
            ControlRequest? currentRequest = null;
            bool sessionAccepted = false;

            try
            {
                using (client)
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true))
                {
                    string? message = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(message)) return;
                    if (!message.StartsWith("CONTROL_REQUEST")) return;

                    var parts = message.Split('|');
                    if (parts.Length < 3) return;

                    var req = new ControlRequest
                    {
                        FromIp = parts[1],
                        UserName = parts[2],
                        HostName = parts.Length >= 4 ? parts[3] : string.Empty
                    };

                    currentRequest = req;

                    bool accepted = false;
                    if (OnControlRequest != null)
                        accepted = await OnControlRequest.Invoke(req);

                    string response = accepted ? "ACCEPT" : "REJECT";
                    byte[] res = Encoding.UTF8.GetBytes(response);
                    await stream.WriteAsync(res, token);

                    if (!accepted)
                    {
                        OnSessionRejected?.Invoke(req);
                        return;
                    }

                    sessionAccepted = true;
                    OnSessionAccepted?.Invoke(req);

                    OnStatusChanged?.Invoke($"Accepted: {req.UserName}");

                    var inputService = new RemoteInputService();

                    _ = Task.Run(async () =>
                    {
                        while (_isRunning && client.Connected && !token.IsCancellationRequested)
                        {
                            try
                            {
                                string? command = await reader.ReadLineAsync();

                                if (command == null)
                                    break;

                                inputService.Execute(command);
                            }
                            catch
                            {
                                break;
                            }
                        }
                    }, token);

                    var capture = new ScreenCaptureService();

                    while (_isRunning && client.Connected && !token.IsCancellationRequested)
                    {
                        try
                        {
                            var start = DateTime.Now;
                            byte[] img = capture.CaptureScreen(70);
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
                if (sessionAccepted && currentRequest != null)
                {
                    OnSessionEnded?.Invoke(currentRequest);
                }

                OnStatusChanged?.Invoke("Client disconnected");
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _cts.Cancel();      
            _listener?.Stop();
            OnStatusChanged?.Invoke("Server stopped");
        }
    }
}