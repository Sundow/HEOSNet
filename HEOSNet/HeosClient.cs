using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;

namespace HEOSNet
{
    public class HeosClient(string host, int port = 1255) : IDisposable
    {
        private readonly string _host = host;
        private readonly int _port = port;
        private TcpClient? _client;
        private NetworkStream? _stream;

        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private readonly StringBuilder _receiveBuffer = new();
        private readonly byte[] _readBuffer = new byte[8192];

        public Task ConnectAsync() => ConnectAsync(null);

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
            if (_stream == null) throw new InvalidOperationException("Client is not connected.");
            timeout ??= TimeSpan.FromSeconds(30);
            await _sendLock.WaitAsync();
            try
            {
                using CancellationTokenSource cts = new(timeout.Value);

                string outbound = command.EndsWith("\r\n", StringComparison.Ordinal) ? command : command + "\r\n";
                byte[] commandBytes = Encoding.ASCII.GetBytes(outbound);
                await _stream.WriteAsync(commandBytes, cts.Token);
                await _stream.FlushAsync(cts.Token);

                while (true)
                {
                    if (TryExtractNextJson(_receiveBuffer, out string? json)) return json!;

                    int bytesRead = await _stream.ReadAsync(_readBuffer, cts.Token);
                    if (bytesRead == 0) throw new IOException("Connection closed by remote host.");
                    string chunk = Encoding.UTF8.GetString(_readBuffer, 0, bytesRead);
                    _receiveBuffer.Append(chunk);

                    if (TryExtractNextJson(_receiveBuffer, out json)) return json!;
                }
            }
            finally
            {
                _sendLock.Release();
            }
        }

        // Generic completion-aware command (handles intermediate 'command under process')
        public async Task<string> SendCommandWithCompletionAsync(HeosCommand command, TimeSpan? overallTimeout = null)
        {
            if (_stream == null) throw new InvalidOperationException("Client is not connected.");
            overallTimeout ??= TimeSpan.FromSeconds(15);

            string targetCommand = command.CommandGroup + "/" + command.Command;

            await _sendLock.WaitAsync();
            try
            {
                using CancellationTokenSource cts = new(overallTimeout.Value);
                string outbound = command.ToString();
                outbound = outbound.EndsWith("\r\n", StringComparison.Ordinal) ? outbound : outbound + "\r\n";
                byte[] commandBytes = Encoding.ASCII.GetBytes(outbound);
                await _stream!.WriteAsync(commandBytes, cts.Token);
                await _stream.FlushAsync(cts.Token);

                while (true)
                {
                    if (TryExtractNextJson(_receiveBuffer, out string? json))
                    {
                        if (IsCommandResponse(json, targetCommand, out bool final))
                        {
                            if (final) return json!; // final response for this command
                            // intermediate -> continue reading
                        }
                        // unrelated event discarded
                    }

                    int bytesRead = await _stream.ReadAsync(_readBuffer, cts.Token);
                    if (bytesRead == 0) throw new IOException("Connection closed by remote host.");
                    string chunk = Encoding.UTF8.GetString(_readBuffer, 0, bytesRead);
                    _receiveBuffer.Append(chunk);
                }
            }
            finally
            {
                _sendLock.Release();
            }
        }

        // Backwards compatibility wrapper (browse specific); now uses generic method.
        public Task<string> SendBrowseWithCompletionAsync(HeosCommand command, TimeSpan? overallTimeout = null) =>
            SendCommandWithCompletionAsync(command, overallTimeout);

        private static bool TryExtractNextJson(StringBuilder buffer, [NotNullWhen(true)] out string? json)
        {
            json = null;
            if (buffer.Length == 0) return false;

            int start = buffer.ToString().IndexOf('{');
            if (start < 0)
            {
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
                    if (escape) escape = false;
                    else if (c == '\\') escape = true;
                    else if (c == '"') inString = false;
                }
                else
                {
                    if (c == '"') inString = true;
                    else if (c == '{') depth++;
                    else if (c == '}')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            int end = i + 1;
                            string candidate = buffer.ToString(start, end - start);
                            buffer.Remove(0, end);
                            json = candidate;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // Returns true if JSON corresponds to the target command; final indicates if processing finished.
        private static bool IsCommandResponse(string json, string targetCommand, out bool final)
        {
            final = false;
            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("heos", out var heos)) return false;
                if (!heos.TryGetProperty("command", out var cmdProp)) return false;
                string? cmd = cmdProp.GetString();
                if (!string.Equals(cmd, targetCommand, StringComparison.OrdinalIgnoreCase)) return false;
                if (heos.TryGetProperty("message", out var msgProp))
                {
                    string? msg = msgProp.GetString();
                    if (!string.IsNullOrEmpty(msg) && msg.Contains("command under process", StringComparison.OrdinalIgnoreCase))
                    {
                        final = false; // intermediate frame
                        return true;
                    }
                }
                final = true;
                return true;
            }
            catch
            {
                return false;
            }
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

