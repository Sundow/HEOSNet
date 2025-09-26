using System.Collections.ObjectModel;
using System.Windows;

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
                var items = HeosBrowse.ParseBrowseItems(resp);
                foreach (var item in items)
                {
                    _rows.Add(new SourceRow(item.Name, item.Type, item.Playable, item.Sid));
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

        public class SourceRow(string name, string type, bool playable, int sid)
        {
            public string Name { get; } = name;
            public string Type { get; } = type;
            public bool Playable { get; } = playable;
            public int Sid { get; } = sid;
        }
    }
}
