using System.Text.Json;

namespace HEOSNet
{
    public class HeosQueue(HeosClient client)
    {
        private readonly HeosClient _client = client;

        // Raw queue retrieval (range optional "start,count")
        public async Task<HeosResponse> GetQueueAsync(int pid, string? range = null)
        {
            Dictionary<string, string> parameters = range is null
                ? new() { { "pid", pid.ToString() } }
                : new() { { "pid", pid.ToString() }, { "range", range } };

            HeosCommand cmd = new("player", "get_queue", parameters);
            string raw = await _client.SendCommandAsync(cmd.ToString());
            return new HeosResponse(raw);
        }

        public async Task<IReadOnlyList<HeosQueueItem>> GetQueueItemsAsync(int pid, string? range = null)
        {
            HeosResponse resp = await GetQueueAsync(pid, range);
            if (!resp.Payload.HasValue)
                return [];

            List<HeosQueueItem> result = [];
            try
            {
                foreach (JsonElement el in resp.Payload.Value.EnumerateArray())
                {
                    int? qid = el.TryGetProperty("qid", out var qidProp) && qidProp.TryGetInt32(out int qidVal) ? qidVal : null;
                    if (!qid.HasValue) continue;

                    string title = el.TryGetProperty("song", out var s) ? s.GetString() ?? string.Empty : string.Empty;
                    string artist = el.TryGetProperty("artist", out var a) ? a.GetString() ?? string.Empty : string.Empty;
                    string album = el.TryGetProperty("album", out var al) ? al.GetString() ?? string.Empty : string.Empty;
                    string image = el.TryGetProperty("image_url", out var im) ? im.GetString() ?? string.Empty : string.Empty;
                    string mediaId = el.TryGetProperty("mid", out var mid) ? mid.GetString() ?? string.Empty : string.Empty;

                    result.Add(new HeosQueueItem(qid.Value, title, artist, album, image, mediaId));
                }
            }
            catch
            {
                // ignore malformed entries
            }
            return result;
        }

        public async Task<HeosResponse> ClearQueueAsync(int pid)
        {
            Dictionary<string, string> parameters = new() { { "pid", pid.ToString() } };
            HeosCommand cmd = new("player", "clear_queue", parameters);
            string raw = await _client.SendCommandAsync(cmd.ToString());
            return new HeosResponse(raw);
        }

        public async Task<HeosResponse> RemoveFromQueueAsync(int pid, int qid)
        {
            Dictionary<string, string> parameters = new() { { "pid", pid.ToString() }, { "qid", qid.ToString() } };
            HeosCommand cmd = new("player", "remove_from_queue", parameters);
            string raw = await _client.SendCommandAsync(cmd.ToString());
            return new HeosResponse(raw);
        }

        // Move a queue item: source qid to destination qid position
        public async Task<HeosResponse> MoveQueueItemAsync(int pid, int sqid, int dqid)
        {
            Dictionary<string, string> parameters = new() { { "pid", pid.ToString() }, { "sqid", sqid.ToString() }, { "dqid", dqid.ToString() } };
            HeosCommand cmd = new("player", "move_queue_item", parameters);
            string raw = await _client.SendCommandAsync(cmd.ToString());
            return new HeosResponse(raw);
        }

        // Add to queue (example for "play_next" or "add_next"); options: add_next, add_to_end, play_now
        public async Task<HeosResponse> AddToQueueAsync(int pid, string sourceId, string containerId, string addMode = "add_to_end")
        {
            Dictionary<string, string> parameters = new()
            {
                { "pid", pid.ToString() },
                { "sid", sourceId },
                { "cid", containerId },
                { "add_queue", addMode }
            };
            HeosCommand cmd = new("browse", "add_to_queue", parameters);
            string raw = await _client.SendCommandAsync(cmd.ToString());
            return new HeosResponse(raw);
        }
    }

    public record HeosQueueItem(
        int Qid,
        string Title,
        string Artist,
        string Album,
        string ImageUrl,
        string MediaId);
}
