
using HEOSNet;

namespace HEOSNet.Tests
{
    [TestClass]
    public class HeosCommandTests
    {
        [TestMethod]
        public void ToString_WithoutParameters_FormatsCorrectly()
        {
            // Arrange
            var command = new HeosCommand("system", "heart_beat");

            // Act
            var commandString = command.ToString();

            // Assert
            Assert.AreEqual("heos://system/heart_beat", commandString);
        }

        [TestMethod]
        public void ToString_WithParameters_FormatsCorrectly()
        {
            // Arrange
            var parameters = new Dictionary<string, string>
            {
                { "pid", "-1508848122" },
                { "name", "My Room" }
            };
            var command = new HeosCommand("player", "set_player_name", parameters);

            // Act
            var commandString = command.ToString();

            // Assert
            Assert.AreEqual("heos://player/set_player_name?pid=-1508848122&name=My Room", commandString);
        }
    }
}
