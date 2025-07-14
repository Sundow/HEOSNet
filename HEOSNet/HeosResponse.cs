
using System.Text.Json;

namespace HEOSNet
{
    public class HeosResponse
    {
        public HeosCommand? ParsedCommand { get; }
        public JsonElement Heos { get; }
        public JsonElement? Payload { get; }
        public string? Result { get; }
        public string? Message { get; }

        public HeosResponse(string responseString)
        {
            JsonElement json = JsonDocument.Parse(responseString).RootElement;
            Heos = json.GetProperty("heos");
            Result = Heos.GetProperty("result").GetString();

            if (Heos.TryGetProperty("command", out JsonElement commandElement))
            {
                string? commandString = commandElement.GetString();
                if (commandString != null)
                {
                    string[] parts = commandString.Split(['/'], 2);
                    string commandGroup = parts[0];
                    string[] commandAndParams = parts[1].Split('?');
                    string command = commandAndParams[0];
                    Dictionary<string, string> parameters = [];
                    if (commandAndParams.Length > 1)
                    {
                        string[] paramPairs = commandAndParams[1].Split('&');
                        foreach (var pair in paramPairs)
                        {
                            string[] keyValue = pair.Split('=');
                            parameters.Add(keyValue[0], Uri.UnescapeDataString(keyValue[1]));
                        }
                    }
                    ParsedCommand = new HeosCommand(commandGroup, command, parameters);
                }
            }

            if (json.TryGetProperty("payload", out var payloadElement))
            {
                Payload = payloadElement;
            }

            if (Heos.TryGetProperty("message", out var messageElement))
            {
                Message = messageElement.GetString();
            }
        }
    }
}
