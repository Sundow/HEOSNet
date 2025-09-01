using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HEOSNet
{
    public class HeosTelnetClient
    {
        private readonly string _host;
        private readonly int _port = 23;

        public HeosTelnetClient(string host)
        {
            _host = host;
        }

        public Task PowerOnAsync() => SendCommandAsync("PWON");

        public Task PowerStandbyAsync() => SendCommandAsync("PWSTANDBY");

        public Task VolumeUpAsync() => SendCommandAsync("MVUP");

        public Task VolumeDownAsync() => SendCommandAsync("MVDOWN");

        public Task MuteOnAsync() => SendCommandAsync("MUON");

        public Task MuteOffAsync() => SendCommandAsync("MUOFF");

        public async Task SendCommandAsync(string command)
        {
            using (TcpClient tcpClient = new())
            {
                await tcpClient.ConnectAsync(_host, _port);
                using (var stream = tcpClient.GetStream())
                {
                    var data = Encoding.ASCII.GetBytes(command + "\r\n");
                    await stream.WriteAsync(data, 0, data.Length);
                }
            }
        }
    }
}