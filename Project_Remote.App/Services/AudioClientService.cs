using NAudio.Wave;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteMate.Services
{
    public class AudioClientService
    {
        private const int PORT = 9003;

        private TcpClient? _client;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        private BufferedWaveProvider? _bufferedWaveProvider;
        private WaveOutEvent? _waveOut;

        private volatile bool _isConnected;

        public async Task<bool> ConnectAsync(string remoteIp)
        {
            try
            {
                Disconnect();

                _cts = new CancellationTokenSource();

                _client = new TcpClient();
                await _client.ConnectAsync(IPAddress.Parse(remoteIp), PORT, _cts.Token);

                NetworkStream stream = _client.GetStream();

                int sampleRate = await ReadIntAsync(stream, _cts.Token);
                int bitsPerSample = await ReadIntAsync(stream, _cts.Token);
                int channels = await ReadIntAsync(stream, _cts.Token);
                int encoding = await ReadIntAsync(stream, _cts.Token);

                WaveFormat waveFormat = new WaveFormat(sampleRate, bitsPerSample, channels);

                _bufferedWaveProvider = new BufferedWaveProvider(waveFormat)
                {
                    BufferDuration = TimeSpan.FromSeconds(2),
                    DiscardOnBufferOverflow = true
                };

                _waveOut = new WaveOutEvent();
                _waveOut.Init(_bufferedWaveProvider);
                _waveOut.Play();

                _isConnected = true;

                _ = Task.Run(() => ReceiveAudioLoopAsync(stream, _cts.Token));

                return true;
            }
            catch
            {
                Disconnect();
                return false;
            }
        }

        private async Task ReceiveAudioLoopAsync(NetworkStream stream, CancellationToken token)
        {
            try
            {
                while (_isConnected &&
                       _client != null &&
                       _client.Connected &&
                       !token.IsCancellationRequested)
                {
                    byte[] lenBuffer = new byte[4];

                    bool ok = await ReadExactAsync(stream, lenBuffer, 4, token);
                    if (!ok)
                        break;

                    int size = BitConverter.ToInt32(lenBuffer, 0);

                    if (size <= 0 || size > 1_000_000)
                        break;

                    byte[] audioBuffer = new byte[size];

                    ok = await ReadExactAsync(stream, audioBuffer, size, token);
                    if (!ok)
                        break;

                    _bufferedWaveProvider?.AddSamples(audioBuffer, 0, size);
                }
            }
            catch
            {
            }
            finally
            {
                Disconnect();
            }
        }

        private async Task<int> ReadIntAsync(NetworkStream stream, CancellationToken token)
        {
            byte[] buffer = new byte[4];

            bool ok = await ReadExactAsync(stream, buffer, 4, token);
            if (!ok)
                throw new Exception("Không đọc được audio header");

            return BitConverter.ToInt32(buffer, 0);
        }

        private async Task<bool> ReadExactAsync(NetworkStream stream, byte[] buffer, int size, CancellationToken token)
        {
            int totalRead = 0;

            while (totalRead < size)
            {
                int read = await stream.ReadAsync(
                    buffer.AsMemory(totalRead, size - totalRead),
                    token
                );

                if (read <= 0)
                    return false;

                totalRead += read;
            }

            return true;
        }

        public void Disconnect()
        {
            _isConnected = false;

            try
            {
                _cts.Cancel();
            }
            catch
            {
            }

            try
            {
                _waveOut?.Stop();
                _waveOut?.Dispose();
            }
            catch
            {
            }

            try
            {
                _client?.Close();
            }
            catch
            {
            }

            _waveOut = null;
            _bufferedWaveProvider = null;
            _client = null;
        }
    }
}