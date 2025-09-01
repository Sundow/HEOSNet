namespace HEOSNet
{
    public class HeosSystem(HeosClient client)
    {
        private readonly HeosClient _client = client;

        public async Task<HeosResponse> HeartbeatAsync()
        {
            HeosCommand command = new("system", "heart_beat");
            var responseString = await _client.SendCommandAsync(command.ToString());
            return new HeosResponse(responseString);
        }

        public async Task<HeosResponse> RebootAsync()
        {
            HeosCommand command = new("system", "reboot");
            var responseString = await _client.SendCommandAsync(command.ToString());
            return new HeosResponse(responseString);
        }

        public async Task<HeosResponse> GetPlayersAsync()
        {
            HeosCommand command = new("player", "get_players");
            var responseString = await _client.SendCommandAsync(command.ToString());
            return new HeosResponse(responseString);
        }

        public async Task<HeosResponse> GetAccountStateAsync()
        {
            HeosCommand command = new("system", "check_account");
            var responseString = await _client.SendCommandAsync(command.ToString());
            return new HeosResponse(responseString);
        }
    }
}
