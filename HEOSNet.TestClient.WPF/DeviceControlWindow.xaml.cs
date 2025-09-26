using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace HEOSNet.TestClient.WPF
{
    public partial class DeviceControlWindow : Window
    {
        private readonly HeosDevice _device;
        private readonly string _host;
        private readonly HeosClient _client;
        private readonly HeosPlayer _player;

        private int? _pid;
        private bool _suppressSliderEvent;
        private bool _volumeInitialized;

        public DeviceControlWindow(HeosDevice device)
        {
            InitializeComponent();
            _device = device;
            _host = device.IpAddress.ToString();
            _client = new HeosClient(_host);
            _player = new HeosPlayer(_client);

            VolumeSlider.IsEnabled = false;
            VolumeValueText.Text = "...";

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

            await InitializeVolumeAsync();
        }

        private async Task InitializeVolumeAsync()
        {
            int? currentVolume = await TryGetVolumeAsync();
            if (currentVolume.HasValue)
            {
                _suppressSliderEvent = true;
                VolumeSlider.Value = currentVolume.Value;
                VolumeValueText.Text = currentVolume.Value.ToString();
                VolumeSlider.IsEnabled = true;
                _suppressSliderEvent = false;
                _volumeInitialized = true;
            }
            else
            {
                VolumeValueText.Text = "n/a";
            }
        }

        private async Task<int?> TryGetVolumeAsync()
        {
            if (_pid.HasValue)
            {
                try
                {
                    var volumeResponse = await _player.GetVolumeAsync(_pid.Value);
                    if (!string.IsNullOrWhiteSpace(volumeResponse.Message))
                    {
                        string? levelFromMessage = GetQueryValue(volumeResponse.Message!, "level");
                        if (levelFromMessage != null && int.TryParse(levelFromMessage, out int parsedLevelFromMessage))
                        {
                            return parsedLevelFromMessage;
                        }
                    }
                }
                catch { }
            }

            if (_device.SupportsTelnet)
            {
                try
                {
                    using TcpClient tcp = new();
                    await tcp.ConnectAsync(_host, 23);
                    using NetworkStream stream = tcp.GetStream();
                    byte[] query = Encoding.ASCII.GetBytes("MV?\r\n");
                    await stream.WriteAsync(query);
                    byte[] buffer = new byte[64];
                    int read = await stream.ReadAsync(buffer);
                    if (read > 0)
                    {
                        string resp = Encoding.ASCII.GetString(buffer, 0, read).Trim();
                        if (resp.StartsWith("MV", StringComparison.OrdinalIgnoreCase))
                        {
                            string numeric = new(resp.Skip(2).TakeWhile(char.IsDigit).ToArray());
                            if (int.TryParse(numeric, out int telnetVol))
                            {
                                return telnetVol;
                            }
                        }
                    }
                }
                catch { }
            }

            return null;
        }

        private static string? GetQueryValue(string queryLike, string key)
        {
            var parts = queryLike.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                var kv = part.Split('=', 2);
                if (kv.Length == 2 && kv[0].Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    return Uri.UnescapeDataString(kv[1]);
                }
            }
            return null;
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
                await _player.PlayAsync(_pid.Value);
        }

        private async void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_pid.HasValue)
                await _player.PauseAsync(_pid.Value);
        }

        private async void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_pid.HasValue)
                await _player.StopAsync(_pid.Value);
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

        private async void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suppressSliderEvent) return;
            if (!_pid.HasValue) return;
            if (!_volumeInitialized) return;

            int newVolume = (int)e.NewValue;
            VolumeValueText.Text = newVolume.ToString();
            await _player.SetVolumeAsync(_pid.Value, newVolume);
        }

        private void ShowQueueButton_Click(object sender, RoutedEventArgs e)
        {
            if (_pid.HasValue)
            {
                QueueWindow dlg = new(_client, _pid.Value) { Owner = this };
                dlg.ShowDialog();
            }
            else
            {
                MessageBox.Show(this, "No player PID resolved yet.", "Queue", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ListSourcesButton_Click(object sender, RoutedEventArgs e)
        {
            SourcesWindow dlg = new(_client) { Owner = this };
            dlg.ShowDialog();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _client.Disconnect();
        }
    }
}

