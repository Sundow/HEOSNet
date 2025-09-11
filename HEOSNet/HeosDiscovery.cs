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

        public static async Task<IEnumerable<HeosDevice>> DiscoverDevices(TimeSpan timeout)
        {
            List<HeosDevice> heosDevices = [];
            IEnumerable<IPAddress> discoveredIps = await DiscoverDeviceIps(timeout);

            foreach (IPAddress ipAddress in discoveredIps)
            {
                try
                {
                    HeosDevice heosDevice = await GetDeviceDetails(ipAddress);
                    heosDevices.Add(heosDevice);
                }
                catch (Exception)
                {
                    // Ignore devices that fail to provide details
                }
            }

            return heosDevices;
        }

        private static async Task<HeosDevice> GetDeviceDetails(IPAddress ipAddress)
        {
            using HeosClient client = new(ipAddress.ToString());
            await client.ConnectAsync(TimeSpan.FromSeconds(5));
            string response = await client.SendCommandAsync(new HeosCommand("player", "get_players").ToString(), TimeSpan.FromSeconds(5));

            JObject jsonResponse = JObject.Parse(response);
            if (jsonResponse["payload"] is JArray players)
            {
                if (players.FirstOrDefault(p => p["ip"]?.ToString() == ipAddress.ToString()) is JObject player)
                {
                    string name = player["name"]?.ToString() ?? "Unknown";
                    string model = player["model"]?.ToString() ?? "Unknown";

                    return new HeosDevice(ipAddress, name, model);
                }
            }

            throw new Exception("Failed to get device details.");
        }

        public static async Task<IEnumerable<IPAddress>> DiscoverDeviceIps(TimeSpan timeout)
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

            await udpClient.SendAsync(requestBytes, requestBytes.Length, remoteEndPoint);

            try
            {
                using CancellationTokenSource cts = new(timeout);
                UdpReceiveResult result = await udpClient.ReceiveAsync(cts.Token);
                string response = Encoding.UTF8.GetString(result.Buffer);

                if (response.Contains("HEOS") && response.Contains("LOCATION:"))
                {
                    string? locationLine = response.Split(['\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(line => line.StartsWith("LOCATION:", StringComparison.OrdinalIgnoreCase));
                    if (locationLine != null)
                    {
                        string locationUrl = locationLine["LOCATION:".Length..].Trim();
                        if (Uri.TryCreate(locationUrl, UriKind.Absolute, out Uri? uri))
                        {
                            if (IPAddress.TryParse(uri.Host, out IPAddress? discoveredIpAddress))
                            {
                                discoveredDevices.Add(discoveredIpAddress);
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Timeout occurred, stop listening
            }
            catch (SocketException)
            {
                // Socket error, likely due to timeout or network issue
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
