namespace HEOSNet
{
    public class HeosPlayer(HeosClient client)
    {
        private readonly HeosClient _client = client;

        public async Task<HeosResponse> GetPlayersAsync()
        {
            HeosCommand command = new("player", "get_players");
            var responseString = await _client.SendCommandAsync(command.ToString());
            return new HeosResponse(responseString);
        }

        public async Task<HeosResponse> GetPlayStateAsync(int pid)
        {
            HeosCommand command = new("player", "get_play_state", new Dictionary<string, string> { { "pid", pid.ToString() } });
            var responseString = await _client.SendCommandAsync(command.ToString());
            return new HeosResponse(responseString);
        }

        public async Task<HeosResponse> SetPlayStateAsync(int pid, string state)
        {
            HeosCommand command = new("player", "set_play_state", new Dictionary<string, string> { { "pid", pid.ToString() }, { "state", state } });
            var responseString = await _client.SendCommandAsync(command.ToString());
            return new HeosResponse(responseString);
        }

        public Task<HeosResponse> PlayAsync(int pid) => SetPlayStateAsync(pid, "play");
        public Task<HeosResponse> PauseAsync(int pid) => SetPlayStateAsync(pid, "pause");
        public Task<HeosResponse> StopAsync(int pid) => SetPlayStateAsync(pid, "stop");

        public async Task<HeosResponse> GetVolumeAsync(int pid)
        {
            HeosCommand command = new("player", "get_volume", new Dictionary<string, string> { { "pid", pid.ToString() } });
            var responseString = await _client.SendCommandAsync(command.ToString());
            return new HeosResponse(responseString);
        }

        public async Task<HeosResponse> SetVolumeAsync(int pid, int volume)
        {
            HeosCommand command = new("player", "set_volume", new Dictionary<string, string> { { "pid", pid.ToString() }, { "level", volume.ToString() } });
            var responseString = await _client.SendCommandAsync(command.ToString());
            try
            {
                return new HeosResponse(responseString);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set volume to {volume} for player {pid}. Response: {responseString}", ex);
            }
        }

        public async Task<HeosResponse> SetMuteAsync(int pid, bool mute)
        {
            HeosCommand command = new("player", "set_mute", new Dictionary<string, string> { { "pid", pid.ToString() }, { "state", mute ? "on" : "off" } });
            var responseString = await _client.SendCommandAsync(command.ToString());
            return new HeosResponse(responseString);
        }

        public async Task<HeosResponse> GetMuteAsync(int pid)
        {
            HeosCommand command = new("player", "get_mute", new Dictionary<string, string> { { "pid", pid.ToString() } });
            var responseString = await _client.SendCommandAsync(command.ToString());
            return new HeosResponse(responseString);
        }

        public Task PowerOnAsync() => _client.SendTelnetCommandAsync("PWON");

        public Task PowerStandbyAsync() => _client.SendTelnetCommandAsync("PWSTANDBY");

        public Task VolumeUpAsync() => _client.SendTelnetCommandAsync("MVUP");

        public Task VolumeDownAsync() => _client.SendTelnetCommandAsync("MVDOWN");

        public Task MuteOnAsync() => _client.SendTelnetCommandAsync("MUON");

        public Task MuteOffAsync() => _client.SendTelnetCommandAsync("MUOFF");
    }
}