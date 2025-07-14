using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HEOSNet
{
    public static class HeosDiscovery
    {
        private const string SsdpMulticastAddress = "239.255.255.250";
        private const int SsdpPort = 1900;
        private const string SsdpSearchRequest = @"M-SEARCH * HTTP/1.1
                                                 HOST: 239.255.255.250:1900
                                                 MAN: ""ssdp:discover""
                                                 MX: 3
                                                 ST: urn:schemas-denon-com:device:ACT-Denon:1";

        public static async Task<IEnumerable<IPAddress>> DiscoverDevices(TimeSpan timeout)
        {
            List<IPAddress> discoveredDevices = [];

            using (UdpClient udpClient = new())
            {
                udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0)); // Bind to any available port

                IPAddress multicastAddress = IPAddress.Parse(SsdpMulticastAddress);
                udpClient.JoinMulticastGroup(multicastAddress);

                byte[] requestBytes = Encoding.UTF8.GetBytes(SsdpSearchRequest);
                IPEndPoint remoteEndPoint = new(multicastAddress, SsdpPort);

                await udpClient.SendAsync(requestBytes, requestBytes.Length, remoteEndPoint);

                DateTime startTime = DateTime.Now;
                while (DateTime.Now - startTime < timeout)
                {
                    try
                    {
                        UdpReceiveResult result = await udpClient.ReceiveAsync().WithTimeout(timeout - (DateTime.Now - startTime));
                        string response = Encoding.UTF8.GetString(result.Buffer);

                        if (response.Contains("HEOS") && response.Contains("LOCATION:"))
                        {
                            string? locationLine = response.Split([ ',' ], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(line => line.StartsWith("LOCATION:", StringComparison.OrdinalIgnoreCase));
                            if (locationLine != null)
                            {
                                string locationUrl = locationLine["LOCATION:".Length..].Trim();
                                if (Uri.TryCreate(locationUrl, UriKind.Absolute, out Uri? uri))
                                {
                                    if (IPAddress.TryParse(uri.Host, out IPAddress? ipAddress))
                                    {
                                        discoveredDevices.Add(ipAddress);
                                    }
                                }
                            }
                        }
                    }
                    catch (TimeoutException)
                    {
                        // Timeout occurred, stop listening
                        break;
                    }
                    catch (SocketException)
                    {
                        // Socket error, likely due to timeout or network issue
                        break;
                    }
                }

                udpClient.DropMulticastGroup(multicastAddress);
            }

            return discoveredDevices.Distinct();
        }
    }

    public static class TaskExtensions
    {
        public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
        {
            if (task == await Task.WhenAny(task, Task.Delay(timeout)))
            {
                return await task;
            }
            else
            {
                throw new TimeoutException();
            }
        }
    }
}