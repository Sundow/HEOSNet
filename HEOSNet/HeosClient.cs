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

        // Synchronize command sends / receives so we correlate correct response
        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private readonly StringBuilder _receiveBuffer = new();
        private readonly byte[] _readBuffer = new byte[8192];

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
            if (_stream == null)
            {
                throw new InvalidOperationException("Client is not connected.");
            }
            timeout ??= TimeSpan.FromSeconds(30);
            await _sendLock.WaitAsync();
            try
            {
                using CancellationTokenSource cts = new(timeout.Value);

                // Write command
                string outbound = command.EndsWith("\r\n", StringComparison.Ordinal) ? command : command + "\r\n";
                byte[] commandBytes = Encoding.ASCII.GetBytes(outbound);
                await _stream.WriteAsync(commandBytes, cts.Token);
                await _stream.FlushAsync(cts.Token);

                // Read until we have a full JSON object (HEOS responses are single JSON per command)
                while (true)
                {
                    // Try to extract without additional read first (in case previous call left extra data)
                    if (TryExtractNextJson(_receiveBuffer, out string? json))
                    {
                        return json!;
                    }

                    int bytesRead = await _stream.ReadAsync(_readBuffer, cts.Token);
                    if (bytesRead == 0)
                    {
                        throw new IOException("Connection closed by remote host.");
                    }
                    string chunk = Encoding.UTF8.GetString(_readBuffer, 0, bytesRead);
                    _receiveBuffer.Append(chunk);

                    if (TryExtractNextJson(_receiveBuffer, out json))
                    {
                        return json!;
                    }
                }
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private static bool TryExtractNextJson(StringBuilder buffer, out string? json)
        {
            json = null;
            if (buffer.Length == 0)
                return false;

            // Find the first '{'
            int start = buffer.ToString().IndexOf('{');
            if (start < 0)
            {
                // No JSON start yet, discard any leading noise
                buffer.Clear();
                return false;
            }

            int depth = 0;
            bool inString = false;
            bool escape = false;
            for (int i = start; i < buffer.Length; i++)
            {
                char c = buffer[i];
                if (inString)
                {
                    if (escape)
                    {
                        escape = false;
                    }
                    else if (c == '\\')
                    {
                        escape = true;
                    }
                    else if (c == '"')
                    {
                        inString = false;
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inString = true;
                    }
                    else if (c == '{')
                    {
                        depth++;
                    }
                    else if (c == '}')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            int end = i + 1; // exclusive
                            string candidate = buffer.ToString(start, end - start);
                            // Remove consumed portion
                            buffer.Remove(0, end);
                            json = candidate;
                            return true;
                        }
                    }
                }
            }
            return false; // need more data
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