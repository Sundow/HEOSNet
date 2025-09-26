using System.Collections.ObjectModel;
using System.Windows;

namespace HEOSNet.TestClient.WPF
{
    public partial class QueueWindow : Window
    {
        private readonly HeosClient _client;
        private readonly int _pid;

        private bool _busy;

        private readonly ObservableCollection<QueueRow> _rows = [];

        public QueueWindow(HeosClient client, int pid)
        {
            InitializeComponent();
            _client = client;
            _pid = pid;
            QueueGrid.ItemsSource = _rows;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e) => await ReloadAsync();

        private async Task ReloadAsync()
        {
            SetBusy(true, "Loading...");
            try
            {
                _rows.Clear();
                HeosQueue queue = new(_client);
                var items = await queue.GetQueueItemsAsync(_pid);
                int idx = 1;
                foreach (var item in items)
                {
                    _rows.Add(new QueueRow(idx++, item.Qid, item.Title, item.Artist, item.Album));
                }
                StatusText.Text = $"Items: {_rows.Count}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
            }
            finally
            {
                SetBusy(false, null);
                UpdateButtons();
            }
        }

        private void SetBusy(bool on, string? status)
        {
            _busy = on;
            RefreshButton.IsEnabled = !on;
            QueueGrid.IsEnabled = !on;
            if (status != null) StatusText.Text = status;
        }

        private void UpdateButtons()
        {
            int sel = QueueGrid.SelectedIndex;
            bool has = sel >= 0;
            UpButton.IsEnabled = has && sel > 0 && !_busy;
            DownButton.IsEnabled = has && sel < _rows.Count - 1 && !_busy;
            DeleteButton.IsEnabled = has && !_busy;
        }

        private void QueueGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => UpdateButtons();

        private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await ReloadAsync();

        private async void UpButton_Click(object sender, RoutedEventArgs e)
        {
            if (QueueGrid.SelectedItem is not QueueRow row) return;
            int index = _rows.IndexOf(row);
            if (index <= 0) return;

            await MoveAsync(row.Qid, _rows[index - 1].Qid, index, index - 1);
        }

        private async void DownButton_Click(object sender, RoutedEventArgs e)
        {
            if (QueueGrid.SelectedItem is not QueueRow row) return;
            int index = _rows.IndexOf(row);
            if (index < 0 || index >= _rows.Count - 1) return;

            await MoveAsync(row.Qid, _rows[index + 1].Qid, index, index + 1);
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (QueueGrid.SelectedItem is not QueueRow row) return;
            SetBusy(true, "Deleting...");
            try
            {
                HeosQueue queue = new(_client);
                await queue.RemoveFromQueueAsync(_pid, row.Qid);
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Delete failed: {ex.Message}";
                SetBusy(false, null);
            }
        }

        private async Task MoveAsync(int sourceQid, int destQid, int sourceIndex, int newIndex)
        {
            SetBusy(true, "Moving...");
            try
            {
                HeosQueue queue = new(_client);
                await queue.MoveQueueItemAsync(_pid, sourceQid, destQid);

                var item = _rows[sourceIndex];
                _rows.RemoveAt(sourceIndex);
                _rows.Insert(newIndex, item);
                Renumber();
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Move failed: {ex.Message}";
                SetBusy(false, null);
            }
        }

        private void Renumber()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                _rows[i].Index = i + 1;
            }
        }

        public class QueueRow(int index, int qid, string title, string artist, string album)
        {
            public int Index { get; set; } = index;
            public int Qid { get; } = qid;
            public string Title { get; } = title;
            public string Artist { get; } = artist;
            public string Album { get; } = album;
        }
    }
}
