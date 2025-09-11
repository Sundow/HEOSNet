using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace HEOSNet.TestClient.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private ObservableCollection<HeosDevice> _discoveredDevices = [];

    public MainWindow()
    {
        InitializeComponent();
        DiscoveredDevicesListBox.ItemsSource = _discoveredDevices;
    }

    private async void DiscoverDevices_Click(object sender, RoutedEventArgs e)
    {
        _discoveredDevices.Clear();

        try
        {
            IEnumerable<HeosDevice> devices = await HeosDiscovery.DiscoverDevices(TimeSpan.FromSeconds(20));

            _discoveredDevices.Clear();
            if (devices != null && devices.Any())
            {
                foreach (var device in devices)
                {
                    _discoveredDevices.Add(device);
                }
            }
            else
            {
                MessageBox.Show("No HEOS devices found.");
            }
        }
        catch (Exception ex)
        {
            _discoveredDevices.Clear();
            MessageBox.Show($"Error during discovery: {ex.Message}");
        }
    }

    private void DiscoveredDevicesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DiscoveredDevicesListBox.SelectedItem is HeosDevice selectedDevice)
        {
            var deviceControlWindow = new DeviceControlWindow(selectedDevice.IpAddress.ToString());
            deviceControlWindow.Show();
        }
    }
}