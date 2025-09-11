using System.Net;

namespace HEOSNet
{
    public class HeosDevice(IPAddress ipAddress, string name, string model)
    {
        public IPAddress IpAddress { get; } = ipAddress;
        public string Name { get; } = name;
        public string Model { get; } = model;
    }
}
