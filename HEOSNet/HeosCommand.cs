namespace HEOSNet
{
    public class HeosCommand(string commandGroup, string command, Dictionary<string, string>? parameters = null)
    {
        public string CommandGroup { get; } = commandGroup;
        public string Command { get; } = command;
        public Dictionary<string, string> Parameters { get; } = parameters ?? [];

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
