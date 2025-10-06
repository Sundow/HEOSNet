using System.Windows;
using System.Windows.Controls;

namespace HEOSNet.TestClient.WPF;

public partial class BrowseSourceWindow : Window
{
    private readonly HeosClient _client;
    private readonly int _rootSid;
    private readonly string _sourceName;
    private readonly HeosBrowse _browse;

    private class Node
    {
        public required int EffectiveSid { get; init; }
        public string? Cid { get; init; }
        public required string Name { get; init; }
        public bool Loaded { get; set; }
        public TreeViewItem? TreeItem { get; set; }
        public bool IsSidOnly => Cid == null;
    }

    public BrowseSourceWindow(HeosClient client, int sourceSid, string sourceName)
    {
        InitializeComponent();
        _client = client;
        _rootSid = sourceSid;
        _sourceName = sourceName;
        _browse = new HeosBrowse(_client);
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        SourceNameText.Text = $"{_sourceName} (SID={_rootSid})";
        await LoadChildrenAsync(null, _rootSid, null);
    }

    private async Task LoadChildrenAsync(Node? parent, int sid, string? cid)
    {
        try
        {
            StatusText.Text = "Loading...";
            var resp = await _browse.BrowseAsync(sid, cid);
            var kids = HeosBrowse.ParseBrowseChildren(sid, resp);

            if (parent == null)
                FoldersTree.Items.Clear();
            else
                parent.TreeItem!.Items.Clear();

            foreach (var c in kids.Where(k => k.IsContainer))
                AddTreeItem(parent, c);

            if (parent == null)
            {
                // Root-level media rows: we have no container cid context -> disable track add until a folder selected
                ItemsList.ItemsSource = kids.Where(k => k.IsMedia)
                    .Select(m => new MediaRow(m.Name, m.Id, m.Playable, sid, null));
            }

            StatusText.Text = $"Containers: {(parent == null ? FoldersTree.Items.Count : parent.TreeItem!.Items.Count)}, Tracks: {kids.Count(k => k.IsMedia)}";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Error: " + ex.Message;
        }
        UpdateButtons();
    }

    private record MediaRow(string Name, string Mid, bool Playable, int Sid, string? ContainerCid)
    {
        public string Kind => "Track";
    }

    private void AddTreeItem(Node? parent, HeosBrowseItem item)
    {
        Node node = new()
        {
            EffectiveSid = item.SourceSid,
            Cid = string.IsNullOrEmpty(item.Id) ? null : item.Id,
            Name = item.Name
        };
        TreeViewItem tvi = new() { Header = node.Name, Tag = node };
        tvi.Items.Add("<loading>");
        tvi.Expanded += TreeItem_Expanded;
        node.TreeItem = tvi;

        if (parent == null)
            FoldersTree.Items.Add(tvi);
        else
            parent.TreeItem!.Items.Add(tvi);
    }

    private async void TreeItem_Expanded(object sender, RoutedEventArgs e)
    {
        if (sender is not TreeViewItem tvi || tvi.Tag is not Node node) return;
        if (node.Loaded) return;

        await LoadChildrenAsync(node, node.EffectiveSid, node.Cid);
        await LoadMediaForNodeAsync(node);
        node.Loaded = true;
    }

    private async Task LoadMediaForNodeAsync(Node node)
    {
        try
        {
            var resp = await _browse.BrowseAsync(node.EffectiveSid, node.Cid);
            var kids = HeosBrowse.ParseBrowseChildren(node.EffectiveSid, resp);
            ItemsList.ItemsSource = kids.Where(k => k.IsMedia)
                .Select(m => new MediaRow(m.Name, m.Id, m.Playable, node.EffectiveSid, node.Cid));
            StatusText.Text = $"Tracks: {kids.Count(k => k.IsMedia)}";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Error: " + ex.Message;
        }
        UpdateButtons();
    }

    private void FoldersTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (FoldersTree.SelectedItem is TreeViewItem tvi && tvi.Tag is Node node && node.Loaded)
            _ = LoadMediaForNodeAsync(node);
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        if (FoldersTree.SelectedItem is TreeViewItem tvi && tvi.Tag is Node node)
        {
            bool canQueueContainer = node.Loaded && !node.IsSidOnly && !string.IsNullOrEmpty(node.Cid);
            AddContainerButton.IsEnabled = canQueueContainer;
            ReplaceAndPlayButton.IsEnabled = canQueueContainer;
        }
        else
        {
            AddContainerButton.IsEnabled = false;
            ReplaceAndPlayButton.IsEnabled = false;
        }

        AddTrackButton.IsEnabled = ItemsList.SelectedItem is MediaRow mr && !string.IsNullOrEmpty(mr.ContainerCid);
    }

    private void ItemsList_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateButtons();

    private async void AddContainerButton_Click(object sender, RoutedEventArgs e)
    {
        if (FoldersTree.SelectedItem is not TreeViewItem tvi || tvi.Tag is not Node node) return;
        if (node.IsSidOnly || string.IsNullOrEmpty(node.Cid))
        {
            StatusText.Text = "Select a folder to queue.";
            return;
        }
        await QueueContainerAsync(node.EffectiveSid, node.Cid, HeosQueueAddAction.Add);
    }

    private async void ReplaceAndPlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (FoldersTree.SelectedItem is not TreeViewItem tvi || tvi.Tag is not Node node) return;
        if (node.IsSidOnly || string.IsNullOrEmpty(node.Cid))
        {
            StatusText.Text = "Select a folder to replace & play.";
            return;
        }
        await QueueContainerAsync(node.EffectiveSid, node.Cid, HeosQueueAddAction.ReplaceAndPlay);
    }

    private async void AddTrackButton_Click(object sender, RoutedEventArgs e)
    {
        if (ItemsList.SelectedItem is not MediaRow row) return;
        if (string.IsNullOrEmpty(row.ContainerCid))
        {
            StatusText.Text = "Select a folder (not root) to add tracks from.";
            return;
        }
        await QueueTrackAsync(row.Sid, row.ContainerCid, row.Mid, HeosQueueAddAction.Add);
    }

    private async Task QueueContainerAsync(int sid, string cid, HeosQueueAddAction action)
    {
        try
        {
            StatusText.Text = "Queueing container...";
            HeosPlayer player = new(_client);
            var playersResp = await player.GetPlayersAsync();
            int? pid = ExtractFirstPid(playersResp);
            if (pid == null) { StatusText.Text = "No player PID."; return; }
            var resp = await _browse.AddContainerToQueueAsync(pid.Value, sid, cid, action);
            StatusText.Text = resp.Result?.Equals("success", StringComparison.OrdinalIgnoreCase) == true
                ? "Queued container."
                : $"Failed: {resp.Message}";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Error: " + ex.Message;
        }
    }

    private async Task QueueTrackAsync(int sid, string cid, string mid, HeosQueueAddAction action)
    {
        try
        {
            StatusText.Text = "Queueing track...";
            HeosPlayer player = new(_client);
            var playersResp = await player.GetPlayersAsync();
            int? pid = ExtractFirstPid(playersResp);
            if (pid == null) { StatusText.Text = "No player PID."; return; }

            var resp = await _browse.AddMediaToQueueAsync(pid.Value, sid, cid, mid, action);
            StatusText.Text = resp.Result?.Equals("success", StringComparison.OrdinalIgnoreCase) == true
                ? "Queued track."
                : $"Failed: {resp.Message}";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Error: " + ex.Message;
        }
    }

    private static int? ExtractFirstPid(HeosResponse resp)
    {
        if (!resp.Payload.HasValue) return null;
        try
        {
            foreach (var el in resp.Payload.Value.EnumerateArray())
                if (el.TryGetProperty("pid", out var pidProp) && pidProp.TryGetInt32(out int pid))
                    return pid;
        }
        catch { }
        return null;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
