namespace HEOSNet
{
    public class HeosPlayer(HeosClient client)
    {
        private readonly HeosClient _client = client;

        public async Task<HeosResponse> GetPlayersAsync()
        {
            var command = new HeosCommand("player", "get_players");
            var responseString = await _client.SendCommandAsync(command.ToString());
            return new HeosResponse(responseString);
        }

        public async Task<HeosResponse> GetPlayStateAsync(string pid)
        {
            var command = new HeosCommand("player", "get_play_state", new Dictionary<string, string> { { "pid", pid } });
            var responseString = await _client.SendCommandAsync(command.ToString());
            return new HeosResponse(responseString);
        }

        public async Task<HeosResponse> SetPlayStateAsync(string pid, string state)
        {
            var command = new HeosCommand("player", "set_play_state", new Dictionary<string, string> { { "pid", pid }, { "state", state } });
            var responseString = await _client.SendCommandAsync(command.ToString());
            return new HeosResponse(responseString);
        }

        public async Task<HeosResponse> GetVolumeAsync(string pid)
        {
            var command = new HeosCommand("player", "get_volume", new Dictionary<string, string> { { "pid", pid } });
            var responseString = await _client.SendCommandAsync(command.ToString());
            return new HeosResponse(responseString);
        }

        public async Task<HeosResponse> SetVolumeAsync(string pid, int volume)
        {
            var command = new HeosCommand("player", "set_volume", new Dictionary<string, string> { { "pid", pid }, { "level", volume.ToString() } });
            var responseString = await _client.SendCommandAsync(command.ToString());
            return new HeosResponse(responseString);
        }

        public async Task<HeosResponse> SetMuteAsync(string pid, bool mute)
        {
            var command = new HeosCommand("player", "set_mute", new Dictionary<string, string> { { "pid", pid }, { "state", mute ? "on" : "off" } });
            var responseString = await _client.SendCommandAsync(command.ToString());
            return new HeosResponse(responseString);
        }

        public async Task<HeosResponse> GetMuteAsync(string pid)
        {
            var command = new HeosCommand("player", "get_mute", new Dictionary<string, string> { { "pid", pid } });
            var responseString = await _client.SendCommandAsync(command.ToString());
            return new HeosResponse(responseString);
        }
    }
}
