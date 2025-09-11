using System.Net.Sockets;
using System.Text;

namespace HEOSNet
{
    public class HeosClient(string host, int port = 1255) : IDisposable
    {
        private readonly string _host = host;
        private readonly int _port = port;
        private TcpClient? _client;
        private NetworkStream? _stream;

        public Task ConnectAsync()
        {
            return ConnectAsync(null);
        }

        public async Task ConnectAsync(TimeSpan? timeout)
        {
            timeout ??= TimeSpan.FromSeconds(30);
            _client = new TcpClient();
            using CancellationTokenSource cts = new(timeout.Value);
            await _client.ConnectAsync(_host, _port, cts.Token);
            _stream = _client.GetStream();
        }

        public virtual Task<string> SendCommandAsync(string command) => SendCommandAsync(command, null);

        public virtual async Task<string> SendCommandAsync(string command, TimeSpan? timeout)
        {
            timeout ??= TimeSpan.FromSeconds(30);
            if (_stream == null)
            {
                throw new InvalidOperationException("Client is not connected.");
            }
            var commandBytes = Encoding.ASCII.GetBytes(command + "\r\n");
            using CancellationTokenSource cts = new(timeout.Value);
            await _stream.WriteAsync(commandBytes, cts.Token);

            var buffer = new byte[4096];
            int byteCount = await _stream.ReadAsync(buffer, cts.Token);
            return Encoding.ASCII.GetString(buffer, 0, byteCount);
        }

        public void Disconnect()
        {
            _stream?.Close();
            _client?.Close();
        }

        public void Dispose()
        {
            Disconnect();
            GC.SuppressFinalize(this);
        }

        public async Task SendTelnetCommandAsync(string command)
        {
            using TcpClient tcpClient = new();
            await tcpClient.ConnectAsync(_host, 23);
            using var stream = tcpClient.GetStream();
            var data = Encoding.ASCII.GetBytes(command + "\r\n");
            await stream.WriteAsync(data);
        }
    }
}