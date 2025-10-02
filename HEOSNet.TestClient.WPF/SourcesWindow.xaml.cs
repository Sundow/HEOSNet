using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using System.Globalization;

namespace HEOSNet.TestClient.WPF
{
    public partial class SourcesWindow : Window
    {
        private readonly HeosClient _client;
        private readonly ObservableCollection<SourceRow> _rows = [];
        private bool _busy;

        public SourcesWindow(HeosClient client)
        {
            InitializeComponent();
            _client = client;
            SourcesGrid.ItemsSource = _rows;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e) => await ReloadAsync();

        private async Task ReloadAsync()
        {
            SetBusy(true, "Loading...");
            try
            {
                _rows.Clear();
                HeosBrowse browse = new(_client);
                var resp = await browse.GetMusicSourcesAsync();
                var items = HeosBrowse.ParseSources(resp); // updated parser
                foreach (var item in items)
                {
                    _rows.Add(new SourceRow(item.Name, item.RawType, item.Playable, item.SourceSid));
                }
                StatusText.Text = $"Sources: {_rows.Count}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
            }
            finally
            {
                SetBusy(false, null);
            }
        }

        private void SetBusy(bool on, string? text)
        {
            _busy = on;
            RefreshButton.IsEnabled = !on;
            if (text != null) StatusText.Text = text;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await ReloadAsync();

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            if (SourcesGrid.SelectedItem is SourceRow row)
            {
                BrowseSourceWindow dlg = new(_client, row.SourceSid, row.Name) { Owner = this };
                dlg.ShowDialog();
            }
        }

        public class SourceRow(string name, string rawType, bool playable, int sourceSid)
        {
            public string Name { get; } = name;
            public string RawType { get; } = rawType;
            public bool Playable { get; } = playable;
            public int SourceSid { get; } = sourceSid;
        }
    }

    public class HeosServerVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is string s && s.Equals("heos_server", StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
