namespace HEOSNet;

public enum HeosQueueAddAction
{
    Add,
    PlayNow,
    PlayNext,
    ReplaceAndPlay
}

public static class HeosQueueAddActionExtensions
{
    public static string ToAid(this HeosQueueAddAction v) => v switch
    {
        HeosQueueAddAction.PlayNow        => "1",
        HeosQueueAddAction.PlayNext       => "2",
        HeosQueueAddAction.Add            => "3",
        HeosQueueAddAction.ReplaceAndPlay => "4",
        _                                 => "3"
    };
}

public partial class HeosBrowse
{
    public async Task<HeosResponse> AddContainerToQueueAsync(int pid, int sid, string cid, HeosQueueAddAction action)
    {
        if (string.IsNullOrWhiteSpace(cid))
            throw new ArgumentException("cid is required for container queueing.", nameof(cid));

        Dictionary<string,string> parameters = new()
        {
            { "pid", pid.ToString() },
            { "sid", sid.ToString() },
            { "cid", cid },
            { "aid", action.ToAid() }
        };
        HeosCommand cmd = new("browse","add_to_queue", parameters);
        string raw = await _client.SendCommandWithCompletionAsync(cmd); // container add usually quick but safe
        return new HeosResponse(raw);
    }

    // Track add: may emit intermediate frame -> use completion-aware send
    public async Task<HeosResponse> AddMediaToQueueAsync(int pid, int sid, string cid, string mid, HeosQueueAddAction action)
    {
        if (string.IsNullOrWhiteSpace(cid))
            throw new ArgumentException("cid (container id) is required for adding a track.", nameof(cid));
        if (string.IsNullOrWhiteSpace(mid))
            throw new ArgumentException("mid (media id) is required for adding a track.", nameof(mid));

        Dictionary<string,string> parameters = new()
        {
            { "pid", pid.ToString() },
            { "sid", sid.ToString() },
            { "cid", cid },
            { "mid", mid },
            { "aid", action.ToAid() }
        };
        HeosCommand cmd = new("browse","add_to_queue", parameters);
        string raw = await _client.SendCommandWithCompletionAsync(cmd);
        return new HeosResponse(raw);
    }
}
