using NAudio.Wave;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteMate.Services
{
    public class AudioServerService
    {
        private const int PORT = 9003;

        private TcpListener? _listener;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private volatile bool _isRunning;

        public void Start()
        {
            if (_isRunning)
                return;

            _isRunning = true;
            _cts = new CancellationTokenSource();

            _listener = new TcpListener(IPAddress.Any, PORT);
            _listener.Start();

            Task.Run(async () =>
            {
                while (_isRunning && !_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        TcpClient client = await _listener.AcceptTcpClientAsync(_cts.Token);
                        _ = Task.Run(() => HandleClientAsync(client, _cts.Token));
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

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            bool clientAlive = true;

            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                using (WasapiLoopbackCapture capture = new WasapiLoopbackCapture())
                {
                    WaveFormat inputFormat = capture.WaveFormat;

                    await WriteIntAsync(stream, inputFormat.SampleRate, token);
                    await WriteIntAsync(stream, 16, token);
                    await WriteIntAsync(stream, inputFormat.Channels, token);
                    await WriteIntAsync(stream, (int)WaveFormatEncoding.Pcm, token);

                    object writeLock = new object();

                    capture.DataAvailable += (s, e) =>
                    {
                        if (!clientAlive || !_isRunning)
                            return;

                        try
                        {
                            byte[] pcmData = ConvertToPcm16(e.Buffer, e.BytesRecorded, inputFormat);

                            if (pcmData.Length == 0)
                                return;

                            byte[] lenBytes = BitConverter.GetBytes(pcmData.Length);

                            lock (writeLock)
                            {
                                stream.Write(lenBytes, 0, lenBytes.Length);
                                stream.Write(pcmData, 0, pcmData.Length);
                            }
                        }
                        catch
                        {
                            clientAlive = false;

                            try
                            {
                                capture.StopRecording();
                            }
                            catch
                            {
                            }
                        }
                    };

                    capture.StartRecording();

                    while (_isRunning &&
                           clientAlive &&
                           client.Connected &&
                           !token.IsCancellationRequested)
                    {
                        await Task.Delay(200, token);
                    }

                    try
                    {
                        capture.StopRecording();
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
            finally
            {
                
            }
        }

        private async Task WriteIntAsync(NetworkStream stream, int value, CancellationToken token)
        {
            byte[] data = BitConverter.GetBytes(value);
            await stream.WriteAsync(data.AsMemory(0, data.Length), token);
        }

        public void Stop()
        {
            _isRunning = false;

            try
            {
                _cts.Cancel();
            }
            catch
            {
            }

            try
            {
                _listener?.Stop();
            }
            catch
            {

            }
        }

        private byte[] ConvertToPcm16(byte[] inputBuffer, int bytesRecorded, WaveFormat inputFormat)
        {
            if (inputFormat.BitsPerSample == 32)
            {
                int samples = bytesRecorded / 4;
                byte[] output = new byte[samples * 2];

                for (int i = 0; i < samples; i++)
                {
                    float sample = BitConverter.ToSingle(inputBuffer, i * 4);

                    if (sample > 1.0f) sample = 1.0f;
                    if (sample < -1.0f) sample = -1.0f;

                    short pcm = (short)(sample * short.MaxValue);

                    output[i * 2] = (byte)(pcm & 0xff);
                    output[i * 2 + 1] = (byte)((pcm >> 8) & 0xff);
                }

                return output;
            }

            if (inputFormat.BitsPerSample == 16)
            {
                byte[] output = new byte[bytesRecorded];
                Buffer.BlockCopy(inputBuffer, 0, output, 0, bytesRecorded);
                return output;
            }

            return Array.Empty<byte>();
        }
    }
}