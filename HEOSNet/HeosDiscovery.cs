using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json.Linq;

namespace HEOSNet
{
    public static class HeosDiscovery
    {
        private const string SsdpMulticastAddress = "239.255.255.250";
        private const int SsdpPort = 1900;
        private const string SsdpSearchRequest = "M-SEARCH * HTTP/1.1\r\nHost: 239.255.255.250:1900\r\nMan: \"ssdp:discover\"\r\nST: urn:schemas-denon-com:device:ACT-Denon:1\r\nMX: 3\r\n\r\n";

        public static async Task<IEnumerable<HeosDevice>> DiscoverDevicesAsync(TimeSpan timeout)
        {
            List<HeosDevice> heosDevices = [];
            IEnumerable<IPAddress> discoveredIps = await DiscoverDeviceIpsAsync(timeout);

            foreach (IPAddress ipAddress in discoveredIps)
            {
                try
                {
                    HeosDevice heosDevice = await GetDeviceDetailsAsync(ipAddress);
                    heosDevices.Add(heosDevice);
                }
                catch (Exception)
                {
                    // Ignore devices that fail to provide details
                }
            }

            return heosDevices;
        }

        private static async Task<HeosDevice> GetDeviceDetailsAsync(IPAddress ipAddress)
        {
            using HeosClient client = new(ipAddress.ToString());
            await client.ConnectAsync(TimeSpan.FromSeconds(5));

            string response = await client.SendCommandAsync(new HeosCommand("player", "get_players").ToString(), TimeSpan.FromSeconds(5));

            JObject jsonResponse = JObject.Parse(response);
            if (jsonResponse["payload"] is JArray players)
            {
                // Prefer exact IP match; fallback to first player if none matches
                JObject? player = players.FirstOrDefault(p => p["ip"]?.ToString() == ipAddress.ToString()) as JObject
                                  ?? players.FirstOrDefault() as JObject;

                if (player is not null)
                {
                    string name = player["name"]?.ToString() ?? "Unknown";
                    string model = player["model"]?.ToString() ?? "Unknown";

                    int? pid = null;
                    var pidToken = player["pid"];
                    if (pidToken != null)
                    {
                        if (pidToken.Type == JTokenType.Integer)
                            pid = pidToken.Value<int>();
                        else if (int.TryParse(pidToken.ToString(), out int parsed))
                            pid = parsed;
                    }

                    bool supportsTelnet = await HeosTelnetDetector.IsTelnetSupportedAsync(ipAddress, model);

                    return new HeosDevice(ipAddress, name, model, supportsTelnet, pid);
                }
            }

            throw new Exception("Failed to get device details.");
        }

        public static async Task<IEnumerable<IPAddress>> DiscoverDeviceIpsAsync(TimeSpan timeout)
        {
            List<IPAddress> discoveredDevices = [];

            NetworkInterface? networkInterface = GetActiveNetworkInterface() ?? throw new Exception("No active network interface found.");
            IPAddress? ipAddress = (networkInterface.GetIPProperties().UnicastAddresses
                .FirstOrDefault(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)?.Address) ?? throw new Exception("No suitable IPv4 address found on the active network interface.");
            using UdpClient udpClient = new();
            udpClient.Client.Bind(new IPEndPoint(ipAddress, 0)); // Bind to specific interface's IP address

            IPAddress multicastAddress = IPAddress.Parse(SsdpMulticastAddress);
            udpClient.JoinMulticastGroup(multicastAddress);

            byte[] requestBytes = Encoding.UTF8.GetBytes(SsdpSearchRequest);
            IPEndPoint remoteEndPoint = new(multicastAddress, SsdpPort);

            // Send search request (can be sent multiple times for reliability if desired)
            await udpClient.SendAsync(requestBytes, requestBytes.Length, remoteEndPoint);

            DateTime endTime = DateTime.UtcNow + timeout;

            try
            {
                while (DateTime.UtcNow < endTime)
                {
                    TimeSpan remaining = endTime - DateTime.UtcNow;
                    if (remaining <= TimeSpan.Zero)
                        break; // precaution

                    try
                    {
                        using CancellationTokenSource cts = new(remaining);
                        UdpReceiveResult result = await udpClient.ReceiveAsync(cts.Token);
                        string response = Encoding.UTF8.GetString(result.Buffer);

                        // Parse SSDP headers robustly (case-insensitive)
                        if (!string.IsNullOrEmpty(response))
                        {
                            // Split on CRLF; remove empty; trim each line
                            var lines = response.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
                                                .Select(l => l.Trim());
                            string? locationLine = lines.FirstOrDefault(l => l.StartsWith("LOCATION:", StringComparison.OrdinalIgnoreCase));
                            if (locationLine != null)
                            {
                                string locationUrl = locationLine["LOCATION:".Length..].Trim();
                                if (Uri.TryCreate(locationUrl, UriKind.Absolute, out Uri? uri) && IPAddress.TryParse(uri.Host, out IPAddress? discoveredIpAddress))
                                {
                                    // Basic HEOS heuristic: allow all that respond to this ST, optionally filter by presence of 'heos'
                                    if (!discoveredDevices.Contains(discoveredIpAddress))
                                    {
                                        discoveredDevices.Add(discoveredIpAddress);
                                    }
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // No packet arrived in remaining window; exit overall loop
                        break;
                    }
                }
            }
            catch (SocketException)
            {
                // Ignore socket errors during discovery window
            }

            udpClient.DropMulticastGroup(multicastAddress);

            return discoveredDevices.Distinct();
        }

        private static NetworkInterface? GetActiveNetworkInterface() =>
            NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(ni =>
                    ni.OperationalStatus == OperationalStatus.Up &&
                    (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet) &&
                    ni.GetIPProperties().UnicastAddresses.Any(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork));
    }
}
