
namespace HEOSNet
{
    public class HeosCommand
    {
        public string CommandGroup { get; }
        public string Command { get; }
        public Dictionary<string, string> Parameters { get; }

        public HeosCommand(string commandGroup, string command, Dictionary<string, string>? parameters = null)
        {
            CommandGroup = commandGroup;
            Command = command;
            Parameters = parameters ?? [];
        }

        public override string ToString()
        {
            var commandString = $"heos://{CommandGroup}/{Command}";
            if (Parameters.Count > 0)
            {
                commandString += "?" + string.Join("&", Parameters.Select(p => $"{p.Key}={p.Value}"));
            }
            return commandString;
        }
    }
}
