using System.Collections.ObjectModel;
using System.Net;
using System.Windows;
using System.Windows.Input;

namespace HEOSNet.TestClient.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private ObservableCollection<string> _discoveredDevices = [];

    public MainWindow()
    {
        InitializeComponent();
        DiscoveredDevicesListBox.ItemsSource = _discoveredDevices;
    }

    private async void DiscoverDevices_Click(object sender, RoutedEventArgs e)
    {
        _discoveredDevices.Clear();
        _discoveredDevices.Add("Searching for HEOS devices...");

        try
        {
            IEnumerable<IPAddress> devices = await HeosDiscovery.DiscoverDevices(TimeSpan.FromSeconds(20));

            _discoveredDevices.Clear();
            if (devices != null && devices.Any())
            {
                foreach (var device in devices)
                {
                    _discoveredDevices.Add(device.ToString());
                }
            }
            else
            {
                _discoveredDevices.Add("No HEOS devices found.");
            }
        }
        catch (Exception ex)
        {
            _discoveredDevices.Clear();
            _discoveredDevices.Add($"Error during discovery: {ex.Message}");
        }
    }

    private void DiscoveredDevicesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DiscoveredDevicesListBox.SelectedItem is string selectedDeviceIp)
        {
            var deviceControlWindow = new DeviceControlWindow(selectedDeviceIp);
            deviceControlWindow.Show();
        }
    }
}