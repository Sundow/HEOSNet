using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HEOSNet
{
    internal static class HeosTelnetDetector
    {
        // Prefix fragments commonly present in models that expose legacy telnet (ASCII) control.
        // This list can be extended as new models are encountered.
        private static readonly string[] ModelPrefixes =
        [
            "AVR-", "AVC-", "SR", "NR", "PMA", "PM", "DRA", "X"
        ];

        private static readonly ConcurrentDictionary<IPAddress, bool> Cache = new();

        public static async Task<bool> IsTelnetSupportedAsync(IPAddress ip, string? model, TimeSpan? timeout = null)
        {
            if (Cache.TryGetValue(ip, out bool cached)) return cached;

            timeout ??= TimeSpan.FromMilliseconds(800);
            bool heuristicMatch = !string.IsNullOrWhiteSpace(model) &&
                                   ModelPrefixes.Any(p => model!.Contains(p, StringComparison.OrdinalIgnoreCase));

            bool result;
            if (heuristicMatch)
            {
                // For heuristic positives we still verify, but use a shorter probe so we return quickly.
                var probeTimeout = TimeSpan.FromMilliseconds(Math.Min(timeout.Value.TotalMilliseconds, 500));
                result = await ProbeAsync(ip, probeTimeout);
                // If probe failed (maybe disabled / firewall) we still record false to avoid repeated attempts.
            }
            else
            {
                // Non-heuristic models: fall back to probing with the provided timeout.
                result = await ProbeAsync(ip, timeout.Value);
            }

            Cache[ip] = result;
            return result;
        }

        private static async Task<bool> ProbeAsync(IPAddress ip, TimeSpan timeout)
        {
            using CancellationTokenSource cts = new(timeout);
            try
            {
                using TcpClient tcp = new();
                await tcp.ConnectAsync(ip, 23, cts.Token);
                using NetworkStream stream = tcp.GetStream();
                byte[] query = Encoding.ASCII.GetBytes("PW?\r\n");
                await stream.WriteAsync(query, cts.Token);

                byte[] buffer = new byte[32];
                using CancellationTokenSource readCts = new(TimeSpan.FromMilliseconds(Math.Min(300, Math.Max(120, timeout.TotalMilliseconds / 2))));
                int read = await stream.ReadAsync(buffer, readCts.Token);
                if (read <= 0) return false;
                string resp = Encoding.ASCII.GetString(buffer, 0, read).Trim();
                return resp.StartsWith("PW", StringComparison.OrdinalIgnoreCase); // PWON / PWSTANDBY
            }
            catch
            {
                return false;
            }
        }
    }
}
