using System.Windows;
using HEOSNet;

namespace HEOSNet.TestClient.WPF
{
    public partial class DeviceControlWindow : Window
    {
        private readonly HeosDevice _device;
        private readonly string _host;
        private HeosClient _client;
        private HeosPlayer _player;
        
        private int? _pid;

        public DeviceControlWindow(HeosDevice device)
        {
            InitializeComponent();
            _device = device;
            _host = device.IpAddress.ToString();
            _client = new HeosClient(_host);
            _player = new HeosPlayer(_client);

            if (!_device.SupportsTelnet)
            {
                PowerOnButton.IsEnabled = false;
                StandbyButton.IsEnabled = false;
                VolumeUpButton.IsEnabled = false;
                VolumeDownButton.IsEnabled = false;
                MuteOnButton.IsEnabled = false;
                MuteOffButton.IsEnabled = false;
            }
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
            if (_device.SupportsTelnet)
                await _player.PowerOnAsync();
        }

        private async void StandbyButton_Click(object sender, RoutedEventArgs e)
        {
            if (_device.SupportsTelnet)
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
            if (_device.SupportsTelnet)
                await _player.VolumeUpAsync();
        }

        private async void VolumeDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (_device.SupportsTelnet)
                await _player.VolumeDownAsync();
        }

        private async void MuteOnButton_Click(object sender, RoutedEventArgs e)
        {
            if (_device.SupportsTelnet)
                await _player.MuteOnAsync();
        }

        private async void MuteOffButton_Click(object sender, RoutedEventArgs e)
        {
            if (_device.SupportsTelnet)
                await _player.MuteOffAsync();
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            _client.Disconnect();
        }
    }
}

