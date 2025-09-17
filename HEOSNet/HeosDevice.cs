using System.Net;

namespace HEOSNet
{
    public class HeosDevice(IPAddress ipAddress, string name, string model, bool supportsTelnet = false, int? pid = null)
    {
        public IPAddress IpAddress { get; } = ipAddress;

        public string Name { get; } = name;

        public string Model { get; } = model;

        public bool SupportsTelnet { get; } = supportsTelnet;

        // Player ID returned by HEOS "player/get_players"; cached to avoid extra calls.
        public int? Pid { get; internal set; } = pid;
    }
}
