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
    // Map to HEOS 'aid' values (HEOS spec: aid=add_to_end|add_next|play_now|replace_and_play)
    public static string ToAid(this HeosQueueAddAction v) => v switch
    {
        HeosQueueAddAction.Add            => "add_to_end",
        HeosQueueAddAction.PlayNow        => "play_now",
        HeosQueueAddAction.PlayNext       => "add_next",
        HeosQueueAddAction.ReplaceAndPlay => "replace_and_play",
        _ => "add_to_end"
    };
}

public partial class HeosBrowse
{
    // Add entire container (folder)
    public async Task<HeosResponse> AddContainerToQueueAsync(int pid, int sid, string cid, HeosQueueAddAction action)
    {
        Dictionary<string,string> parameters = new()
        {
            { "pid", pid.ToString() },
            { "sid", sid.ToString() },
            { "cid", cid },
            { "aid", action.ToAid() }
        };
        HeosCommand cmd = new("browse","add_to_queue", parameters);
        string raw = await _client.SendCommandAsync(cmd.ToString());
        return new HeosResponse(raw);
    }

    // Add single media item (track) – use mid only (NO cid simultaneously)
    public async Task<HeosResponse> AddMediaToQueueAsync(int pid, int sid, string mid, HeosQueueAddAction action)
    {
        Dictionary<string,string> parameters = new()
        {
            { "pid", pid.ToString() },
            { "sid", sid.ToString() },
            { "mid", mid },
            { "aid", action.ToAid() }
        };
        HeosCommand cmd = new("browse","add_to_queue", parameters);
        string raw = await _client.SendCommandAsync(cmd.ToString());
        return new HeosResponse(raw);
    }
}
