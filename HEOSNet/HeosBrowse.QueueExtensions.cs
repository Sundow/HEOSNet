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
        HeosQueueAddAction.Add            => "add",
        HeosQueueAddAction.PlayNow        => "play_now",
        HeosQueueAddAction.PlayNext       => "play_next",
        HeosQueueAddAction.ReplaceAndPlay => "replace_and_play",
        _ => "add"
    };
}

public partial class HeosBrowse
{
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
