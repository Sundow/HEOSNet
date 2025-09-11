namespace HEOSNet.Tests
{
    [TestClass]
    public class HeosCommandTests
    {
        [TestMethod]
        public void ToString_WithoutParameters_FormatsCorrectly()
        {
            // Arrange
            HeosCommand command = new("system", "heart_beat");

            // Act
            _ = command.ToString();
        }

        [TestMethod]
        public void ToString_WithParameters_FormatsCorrectly()
        {
            // Arrange
            Dictionary<string, string> parameters = new()
            {
                { "pid", "-1508848122" },
                { "name", "My Room" }
            };
            HeosCommand command = new ("player", "set_player_name", parameters);

            // Act
            string commandString = command.ToString();

            // Assert
            Assert.AreEqual("heos://player/set_player_name?pid=-1508848122&name=My Room", commandString);
        }
    }
}
