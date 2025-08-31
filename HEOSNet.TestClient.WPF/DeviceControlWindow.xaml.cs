
using System.Windows;
using HEOSNet;

namespace HEOSNet.TestClient.WPF
{
    public partial class DeviceControlWindow : Window
    {
        private readonly string _host;
        private HeosClient _client;
        private HeosPlayer _player;
        private HeosTelnetClient _telnetClient;
        private int? _pid;

        public DeviceControlWindow(string host)
        {
            InitializeComponent();
            _host = host;
            _client = new HeosClient(_host);
            _player = new HeosPlayer(_client);
            _telnetClient = new HeosTelnetClient(_host);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await _client.ConnectAsync();
            var playersResponse = await _player.GetPlayersAsync();
            if (playersResponse.Payload.HasValue)
            {
                var players = playersResponse.Payload.Value.EnumerateArray();
                if (players.Any())
                {
                    _pid = players.First().GetProperty("pid").GetInt32();
                }
            }
        }

        private async void PowerOnButton_Click(object sender, RoutedEventArgs e)
        {
            await _telnetClient.PowerOnAsync();
        }

        private async void StandbyButton_Click(object sender, RoutedEventArgs e)
        {
            await _telnetClient.PowerStandbyAsync();
        }

        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (_pid.HasValue)
            {
                await _player.SetPlayStateAsync(_pid.Value, "play");
            }
        }

        private async void VolumeUpButton_Click(object sender, RoutedEventArgs e)
        {
            await _telnetClient.VolumeUpAsync();
        }

        private async void VolumeDownButton_Click(object sender, RoutedEventArgs e)
        {
            await _telnetClient.VolumeDownAsync();
        }

        private async void MuteOnButton_Click(object sender, RoutedEventArgs e)
        {
            await _telnetClient.MuteOnAsync();
        }

        private async void MuteOffButton_Click(object sender, RoutedEventArgs e)
        {
            await _telnetClient.MuteOffAsync();
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            _client.Disconnect();
        }
    }
}

