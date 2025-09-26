using System.Text.Json;

namespace HEOSNet
{
    public class HeosBrowse(HeosClient client)
    {
        private readonly HeosClient _client = client;

        // Gets top-level music sources (streaming services, inputs, etc.)
        public async Task<HeosResponse> GetMusicSourcesAsync()
        {
            HeosCommand cmd = new("browse", "get_music_sources");
            string raw = await _client.SendCommandAsync(cmd.ToString());
            return new HeosResponse(raw);
        }

        // Generic browse call. cid is optional (null => root of the source).
        // range format: "start,count" (e.g. "0,49")
        public async Task<HeosResponse> BrowseAsync(int sid, string? cid = null, string? range = null)
        {
            Dictionary<string, string> parameters = new() { { "sid", sid.ToString() } };
            if (!string.IsNullOrWhiteSpace(cid)) parameters["cid"] = cid;
            if (!string.IsNullOrWhiteSpace(range)) parameters["range"] = range;

            HeosCommand cmd = new("browse", "browse", parameters);
            string raw = await _client.SendCommandAsync(cmd.ToString());
            return new HeosResponse(raw);
        }

        // Retrieve detailed metadata for a single container/item
        public async Task<HeosResponse> RetrieveMetadataAsync(int sid, string cid)
        {
            Dictionary<string, string> parameters = new() { { "sid", sid.ToString() }, { "cid", cid } };
            HeosCommand cmd = new("browse", "retrieve_metadata", parameters);
            string raw = await _client.SendCommandAsync(cmd.ToString());
            return new HeosResponse(raw);
        }

        // Get available search criteria for a source
        public async Task<HeosResponse> GetSearchCriteriaAsync(int sid)
        {
            Dictionary<string, string> parameters = new() { { "sid", sid.ToString() } };
            HeosCommand cmd = new("browse", "get_search_criteria", parameters);
            string raw = await _client.SendCommandAsync(cmd.ToString());
            return new HeosResponse(raw);
        }

        // Perform a search within a source for a given criteria and search string.
        public async Task<HeosResponse> SearchAsync(int sid, string search, string? scid = null, string? range = null)
        {
            Dictionary<string, string> parameters = scid switch
            {
                null when range is null => new() { { "sid", sid.ToString() }, { "search", search } },
                null => new() { { "sid", sid.ToString() }, { "search", search }, { "range", range! } },
                _ when range is null => new() { { "sid", sid.ToString() }, { "search", search }, { "scid", scid! } },
                _ => new() { { "sid", sid.ToString() }, { "search", search }, { "scid", scid! }, { "range", range! } }
            };

            HeosCommand cmd = new("browse", "search", parameters);
            string raw = await _client.SendCommandAsync(cmd.ToString());
            return new HeosResponse(raw);
        }

        // Only numeric 'sid' entries are collected.
        public static IReadOnlyList<HeosBrowseItem> ParseBrowseItems(HeosResponse response)
        {
            if (!response.Payload.HasValue)
                return [];

            List<HeosBrowseItem> items = [];
            try
            {
                foreach (JsonElement el in response.Payload.Value.EnumerateArray())
                {
                    string? name = el.TryGetProperty("name", out var n) ? n.GetString() : null;
                    string? type = el.TryGetProperty("type", out var t) ? t.GetString() : null;
                    int? sid = el.TryGetProperty("sid", out var sidProp) && sidProp.TryGetInt32(out int sidVal) ? sidVal : null;
                    string? image = el.TryGetProperty("image_url", out var i) ? i.GetString() : null;
                    bool playable = el.TryGetProperty("playable", out var p) && p.ValueKind == JsonValueKind.String && string.Equals(p.GetString(), "yes", StringComparison.OrdinalIgnoreCase);

                    if (name != null && sid.HasValue)
                        items.Add(new HeosBrowseItem(name, sid.Value, type ?? string.Empty, playable, image ?? string.Empty));
                }
            }
            catch
            {
                // ignore parse errors
            }
            return items;
        }
    }

    public record HeosBrowseItem(
        string Name,
        int Sid,
        string Type,
        bool Playable,
        string ImageUrl);
}
