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

        public async Task<HeosResponse> GetPlayStateAsync(int pid)
        {
            var command = new HeosCommand("player", "get_play_state", new Dictionary<string, string> { { "pid", pid.ToString() } });
            var responseString = await _client.SendCommandAsync(command.ToString());
            return new HeosResponse(responseString);
        }

        public async Task<HeosResponse> SetPlayStateAsync(int pid, string state)
        {
            var command = new HeosCommand("player", "set_play_state", new Dictionary<string, string> { { "pid", pid.ToString() }, { "state", state } });
            var responseString = await _client.SendCommandAsync(command.ToString());
            return new HeosResponse(responseString);
        }

        public async Task<HeosResponse> GetVolumeAsync(int pid)
        {
            var command = new HeosCommand("player", "get_volume", new Dictionary<string, string> { { "pid", pid.ToString() } });
            var responseString = await _client.SendCommandAsync(command.ToString());
            return new HeosResponse(responseString);
        }

        public async Task<HeosResponse> SetVolumeAsync(int pid, int volume)
        {
            var command = new HeosCommand("player", "set_volume", new Dictionary<string, string> { { "pid", pid.ToString() }, { "level", volume.ToString() } });
            var responseString = await _client.SendCommandAsync(command.ToString());
            return new HeosResponse(responseString);
        }

        public async Task<HeosResponse> SetMuteAsync(int pid, bool mute)
        {
            var command = new HeosCommand("player", "set_mute", new Dictionary<string, string> { { "pid", pid.ToString() }, { "state", mute ? "on" : "off" } });
            var responseString = await _client.SendCommandAsync(command.ToString());
            return new HeosResponse(responseString);
        }

        public async Task<HeosResponse> GetMuteAsync(int pid)
        {
            var command = new HeosCommand("player", "get_mute", new Dictionary<string, string> { { "pid", pid.ToString() } });
            var responseString = await _client.SendCommandAsync(command.ToString());
            return new HeosResponse(responseString);
        }
    }
}
