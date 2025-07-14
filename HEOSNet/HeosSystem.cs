using System.Net;

namespace HEOSNet
{
    public class HeosSystem(HeosClient client)
    {
        private readonly HeosClient _client = client;

        public async Task<HeosResponse> HeartbeatAsync()
        {
            var command = new HeosCommand("system", "heart_beat");
            var responseString = await _client.SendCommandAsync(command.ToString());
            return new HeosResponse(responseString);
        }

        public async Task<HeosResponse> RebootAsync()
        {
            var command = new HeosCommand("system", "reboot");
            var responseString = await _client.SendCommandAsync(command.ToString());
            return new HeosResponse(responseString);
        }

        public async Task<HeosResponse> GetPlayersAsync()
        {
            var command = new HeosCommand("player", "get_players");
            var responseString = await _client.SendCommandAsync(command.ToString());
            return new HeosResponse(responseString);
        }

        public async Task<HeosResponse> GetAccountStateAsync()
        {
            var command = new HeosCommand("system", "check_account");
            var responseString = await _client.SendCommandAsync(command.ToString());
            return new HeosResponse(responseString);
        }

        
    }
}
