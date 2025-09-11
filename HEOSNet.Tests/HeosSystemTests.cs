using Moq;

namespace HEOSNet.Tests
{
    [TestClass]
    public class HeosSystemTests
    {
        [TestMethod]
        public async Task HeartbeatAsync_SendsCorrectCommand()
        {
            // Arrange
            var mockClient = new Mock<HeosClient>("127.0.0.1", 1255);
            mockClient.Setup(c => c.SendCommandAsync(It.IsAny<string>()))
                      .ReturnsAsync("{\"heos\": {\"command\": \"system/heart_beat\", \"result\": \"success\", \"message\": \"\"}}");

            var system = new HeosSystem(mockClient.Object);

            // Act
            var response = await system.HeartbeatAsync();

            // Assert
            mockClient.Verify(c => c.SendCommandAsync("heos://system/heart_beat"), Times.Once);
            Assert.AreEqual("success", response.Result);
        }

        [TestMethod]
        public async Task RebootAsync_SendsCorrectCommand()
        {
            // Arrange
            var mockClient = new Mock<HeosClient>("127.0.0.1", 1255);
            mockClient.Setup(c => c.SendCommandAsync(It.IsAny<string>()))
                      .ReturnsAsync("{\"heos\": {\"command\": \"system/reboot\", \"result\": \"success\", \"message\": \"\"}}");

            var system = new HeosSystem(mockClient.Object);

            // Act
            var response = await system.RebootAsync();

            // Assert
            mockClient.Verify(c => c.SendCommandAsync("heos://system/reboot"), Times.Once);
            Assert.AreEqual("success", response.Result);
        }

        [TestMethod]
        public async Task GetPlayersAsync_SendsCorrectCommand()
        {
            // Arrange
            var mockClient = new Mock<HeosClient>("127.0.0.1", 1255);
            mockClient.Setup(c => c.SendCommandAsync(It.IsAny<string>()))
                      .ReturnsAsync("{\"heos\": {\"command\": \"player/get_players\", \"result\": \"success\", \"message\": \"\"}, \"payload\": []}");

            var system = new HeosSystem(mockClient.Object);

            // Act
            var response = await system.GetPlayersAsync();

            // Assert
            mockClient.Verify(c => c.SendCommandAsync("heos://player/get_players"), Times.Once);
            Assert.AreEqual("success", response.Result);
        }

        [TestMethod]
        public async Task GetAccountStateAsync_SendsCorrectCommand()
        {
            // Arrange
            var mockClient = new Mock<HeosClient>("127.0.0.1", 1255);
            mockClient.Setup(c => c.SendCommandAsync(It.IsAny<string>()))
                      .ReturnsAsync("{\"heos\": {\"command\": \"system/check_account\", \"result\": \"success\", \"message\": \"\"}}");

            var system = new HeosSystem(mockClient.Object);

            // Act
            var response = await system.GetAccountStateAsync();

            // Assert
            mockClient.Verify(c => c.SendCommandAsync("heos://system/check_account"), Times.Once);
            Assert.AreEqual("success", response.Result);
        }
    }
}