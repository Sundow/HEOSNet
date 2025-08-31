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

        public async Task PowerOnAsync()
        {
            await SendCommandAsync("PWON");
        }

        public async Task PowerStandbyAsync()
        {
            await SendCommandAsync("PWSTANDBY");
        }

        public async Task VolumeUpAsync()
        {
            await SendCommandAsync("MVUP");
        }

        public async Task VolumeDownAsync()
        {
            await SendCommandAsync("MVDOWN");
        }

        public async Task MuteOnAsync()
        {
            await SendCommandAsync("MUON");
        }

        public async Task MuteOffAsync()
        {
            await SendCommandAsync("MUOFF");
        }

        public async Task SendCommandAsync(string command)
        {
            using (var tcpClient = new TcpClient())
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