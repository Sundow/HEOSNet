using System.Text.Json;

namespace HEOSNet
{
    public enum HeosBrowseItemKind
    {
        Source,
        Container,
        Media
    }

    public record HeosBrowseItem(
        int SourceSid,      // Effective sid to use for further browse calls at this node
        string Id,          // cid or mid. Empty string => sid-only container (DLNA server root)
        HeosBrowseItemKind Kind,
        string Name,
        bool Playable,
        string RawType,
        string ImageUrl
    )
    {
        public bool IsContainer => Kind == HeosBrowseItemKind.Container;
        public bool IsMedia => Kind == HeosBrowseItemKind.Media;
        public bool IsSidOnlyContainer => IsContainer && string.IsNullOrEmpty(Id);
    }

    public partial class HeosBrowse
    {
        private readonly HeosClient _client;
        public HeosBrowse(HeosClient client) => _client = client;

        public async Task<HeosResponse> GetMusicSourcesAsync()
        {
            HeosCommand cmd = new("browse", "get_music_sources");
            string raw = await _client.SendCommandAsync(cmd.ToString());
            return new HeosResponse(raw);
        }

        public async Task<HeosResponse> BrowseAsync(int sid, string? cid = null, string? range = null)
        {
            Dictionary<string, string> parameters = new() { { "sid", sid.ToString() } };
            if (!string.IsNullOrWhiteSpace(cid)) parameters["cid"] = cid;
            if (!string.IsNullOrWhiteSpace(range)) parameters["range"] = range;
            HeosCommand cmd = new("browse", "browse", parameters);

            string raw = await _client.SendBrowseWithCompletionAsync(cmd);
   
            return new HeosResponse(raw);
        }

        public async Task<HeosResponse> RetrieveMetadataAsync(int sid, string cid)
        {
            Dictionary<string, string> parameters = new() { { "sid", sid.ToString() }, { "cid", cid } };
            HeosCommand cmd = new("browse", "retrieve_metadata", parameters);
            string raw = await _client.SendCommandAsync(cmd.ToString());
            return new HeosResponse(raw);
        }

        public async Task<HeosResponse> GetSearchCriteriaAsync(int sid)
        {
            Dictionary<string, string> parameters = new() { { "sid", sid.ToString() } };
            HeosCommand cmd = new("browse", "get_search_criteria", parameters);
            string raw = await _client.SendCommandAsync(cmd.ToString());
            return new HeosResponse(raw);
        }

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

        public static IReadOnlyList<HeosBrowseItem> ParseSources(HeosResponse response)
        {
            if (!response.Payload.HasValue) return [];
            List<HeosBrowseItem> items = [];
            foreach (var el in response.Payload.Value.EnumerateArray())
            {
                if (!el.TryGetProperty("sid", out var sidProp) || !sidProp.TryGetInt32(out int sid)) continue;
                string name = el.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                string rawType = el.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
                string image = el.TryGetProperty("image_url", out var i) ? i.GetString() ?? "" : "";
                bool playable = el.TryGetProperty("playable", out var p) &&
                                p.ValueKind == JsonValueKind.String &&
                                (p.GetString() ?? "").Equals("yes", StringComparison.OrdinalIgnoreCase);
                items.Add(new HeosBrowseItem(sid, sid.ToString(), HeosBrowseItemKind.Source, name, playable, rawType, image));
            }
            return items;
        }

        // Handles:
        //  - Containers: cid present
        //  - Media: mid present
        //  - DLNA server roots under Local Music: only nested sid (different from sourceSid) and no cid/mid
        public static IReadOnlyList<HeosBrowseItem> ParseBrowseChildren(int sourceSid, HeosResponse response)
        {
            if (!response.Payload.HasValue) return [];
            List<HeosBrowseItem> items = [];
            foreach (var el in response.Payload.Value.EnumerateArray())
            {
                string name = el.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                string rawType = el.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
                string image = el.TryGetProperty("image_url", out var i) ? i.GetString() ?? "" : "";
                bool playable = el.TryGetProperty("playable", out var p) &&
                                p.ValueKind == JsonValueKind.String &&
                                (p.GetString() ?? "").Equals("yes", StringComparison.OrdinalIgnoreCase);

                if (el.TryGetProperty("cid", out var cidProp))
                {
                    string cid = cidProp.GetString() ?? "";
                    if (cid.Length == 0) continue;
                    items.Add(new HeosBrowseItem(sourceSid, cid, HeosBrowseItemKind.Container, name, playable, rawType, image));
                    continue;
                }

                if (el.TryGetProperty("mid", out var midProp))
                {
                    string mid = midProp.GetString() ?? "";
                    if (mid.Length == 0) continue;
                    items.Add(new HeosBrowseItem(sourceSid, mid, HeosBrowseItemKind.Media, name, playable, rawType, image));
                    continue;
                }

                // Nested DLNA server root: only 'sid' present and different from current source sid.
                if (el.TryGetProperty("sid", out var nestedSidProp) &&
                    nestedSidProp.TryGetInt32(out int nestedSid) &&
                    nestedSid != sourceSid)
                {
                    // Represent as container with empty Id; next browse uses new SourceSid (nestedSid) and null cid.
                    items.Add(new HeosBrowseItem(nestedSid, string.Empty, HeosBrowseItemKind.Container, name, playable, rawType, image));
                }
            }
            return items;
        }
    }
}
