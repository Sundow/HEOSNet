using System.Net.Sockets;
using System.Text;

namespace HEOSNet
{
    public class HeosClient(string host, int port = 1255)
    {
        private readonly string _host = host;
        private readonly int _port = port;
        private TcpClient? _client;
        private NetworkStream? _stream;

        public async Task ConnectAsync()
        {
            _client = new TcpClient();
            await _client.ConnectAsync(_host, _port);
            _stream = _client.GetStream();
        }

        public virtual async Task<string> SendCommandAsync(string command)
        {
            if (_stream == null)
            {
                throw new InvalidOperationException("Client is not connected.");
            }
            var commandBytes = Encoding.ASCII.GetBytes(command + "\r\n");
            await _stream.WriteAsync(commandBytes, 0, commandBytes.Length);

            var buffer = new byte[4096];
            int byteCount = await _stream.ReadAsync(buffer, 0, buffer.Length);
            return Encoding.ASCII.GetString(buffer, 0, byteCount);
        }

        public void Disconnect()
        {
            _stream?.Close();
            _client?.Close();
        }

        public async Task SendTelnetCommandAsync(string command)
        {
            using (TcpClient tcpClient = new())
            {
                await tcpClient.ConnectAsync(_host, 23);
                using (var stream = tcpClient.GetStream())
                {
                    var data = Encoding.ASCII.GetBytes(command + "\r\n");
                    await stream.WriteAsync(data, 0, data.Length);
                }
            }
        }
    }
}
