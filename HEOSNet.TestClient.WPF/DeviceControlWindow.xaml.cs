
using System.Windows;
using HEOSNet;

namespace HEOSNet.TestClient.WPF
{
    public partial class DeviceControlWindow : Window
    {
        private readonly string _host;
        private HeosClient _client;
        private HeosPlayer _player;
        
        private int? _pid;

        public DeviceControlWindow(string host)
        {
            InitializeComponent();
            _host = host;
            _client = new HeosClient(_host);
            _player = new HeosPlayer(_client);
            
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
            await _player.PowerOnAsync();
        }

        private async void StandbyButton_Click(object sender, RoutedEventArgs e)
        {
            await _player.PowerStandbyAsync();
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
            await _player.VolumeUpAsync();
        }

        private async void VolumeDownButton_Click(object sender, RoutedEventArgs e)
        {
            await _player.VolumeDownAsync();
        }

        private async void MuteOnButton_Click(object sender, RoutedEventArgs e)
        {
            await _player.MuteOnAsync();
        }

        private async void MuteOffButton_Click(object sender, RoutedEventArgs e)
        {
            await _player.MuteOffAsync();
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            _client.Disconnect();
        }
    }
}

