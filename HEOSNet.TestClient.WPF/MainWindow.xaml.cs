using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HEOSNet;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Linq;

namespace HEOSNet.TestClient.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private ObservableCollection<string> _discoveredDevices;

    public MainWindow()
    {
        InitializeComponent();
        _discoveredDevices = new ObservableCollection<string>();
        DiscoveredDevicesListBox.ItemsSource = _discoveredDevices;
    }

    private async void DiscoverDevices_Click(object sender, RoutedEventArgs e)
    {
        _discoveredDevices.Clear();
        _discoveredDevices.Add("Searching for HEOS devices...");

        try
        {
            IEnumerable<IPAddress> devices = await HeosDiscovery.DiscoverDevices(TimeSpan.FromSeconds(60));

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
}