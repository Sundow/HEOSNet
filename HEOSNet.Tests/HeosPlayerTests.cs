using Moq;

namespace HEOSNet.Tests
{
    [TestClass]
    public class HeosPlayerTests
    {
        private Mock<HeosClient>? _mockClient;
        private HeosPlayer? _heosPlayer;

        [TestInitialize]
        public void Setup()
        {
            _mockClient = new Mock<HeosClient>("127.0.0.1", 1255);
            _heosPlayer = new HeosPlayer(_mockClient.Object);
        }

        [TestMethod]
        public async Task GetPlayersAsync_SendsCorrectCommand()
        {
            // Arrange
            _mockClient!.Setup(c => c.SendCommandAsync(It.IsAny<string>()))
                       .ReturnsAsync("{\"heos\": {\"command\": \"player/get_players\", \"result\": \"success\", \"message\": \"\"}, \"payload\": []}");

            // Act
            var response = await _heosPlayer!.GetPlayersAsync();

            // Assert
            _mockClient!.Verify(c => c.SendCommandAsync("heos://player/get_players"), Times.Once);
            Assert.AreEqual("success", response.Result);
        }

        [TestMethod]
        public async Task GetPlayStateAsync_SendsCorrectCommand()
        {
            // Arrange
            int pid = 12345;
            _mockClient!.Setup(c => c.SendCommandAsync(It.IsAny<string>()))
                       .ReturnsAsync($"{{\"heos\": {{\"command\": \"player/get_play_state?pid={pid}\", \"result\": \"success\", \"message\": \"\"}}, \"payload\": {{\"state\": \"play\"}}}}");

            // Act
            var response = await _heosPlayer!.GetPlayStateAsync(pid);

            // Assert
            _mockClient!.Verify(c => c.SendCommandAsync($"heos://player/get_play_state?pid={pid}"), Times.Once);
            Assert.AreEqual("success", response.Result);
        }

        [TestMethod]
        public async Task SetPlayStateAsync_SendsCorrectCommand()
        {
            // Arrange
            int pid = 12345;
            string state = "play";
            _mockClient!.Setup(c => c.SendCommandAsync(It.IsAny<string>()))
                       .ReturnsAsync($"{{\"heos\": {{\"command\": \"player/set_play_state?pid={pid}&state={state}\", \"result\": \"success\", \"message\": \"\"}}}}");

            // Act
            var response = await _heosPlayer!.SetPlayStateAsync(pid, state);

            // Assert
            _mockClient!.Verify(c => c.SendCommandAsync($"heos://player/set_play_state?pid={pid}&state={state}"), Times.Once);
            Assert.AreEqual("success", response.Result);
        }

        [TestMethod]
        public async Task GetVolumeAsync_SendsCorrectCommand()
        {
            // Arrange
            int pid = 12345;
            _mockClient!.Setup(c => c.SendCommandAsync(It.IsAny<string>()))
                       .ReturnsAsync($"{{\"heos\": {{\"command\": \"player/get_volume?pid={pid}\", \"result\": \"success\", \"message\": \"\"}}, \"payload\": {{\"level\": 50}}}}");

            // Act
            var response = await _heosPlayer!.GetVolumeAsync(pid);

            // Assert
            _mockClient!.Verify(c => c.SendCommandAsync($"heos://player/get_volume?pid={pid}"), Times.Once);
            Assert.AreEqual("success", response.Result);
        }

        [TestMethod]
        public async Task SetVolumeAsync_SendsCorrectCommand()
        {
            // Arrange
            int pid = 12345;
            int volume = 60;
            _mockClient!.Setup(c => c.SendCommandAsync(It.IsAny<string>()))
                       .ReturnsAsync($"{{\"heos\": {{\"command\": \"player/set_volume?pid={pid}&level={volume}\", \"result\": \"success\", \"message\": \"\"}}}}");

            // Act
            var response = await _heosPlayer!.SetVolumeAsync(pid, volume);

            // Assert
            _mockClient!.Verify(c => c.SendCommandAsync($"heos://player/set_volume?pid={pid}&level={volume}"), Times.Once);
            Assert.AreEqual("success", response.Result);
        }

        [TestMethod]
        public async Task SetMuteAsync_SendsCorrectCommand()
        {
            // Arrange
            int pid = 12345;
            bool mute = true;
            _mockClient!.Setup(c => c.SendCommandAsync(It.IsAny<string>()))
                       .ReturnsAsync($"{{\"heos\": {{\"command\": \"player/set_mute?pid={pid}&state=on\", \"result\": \"success\", \"message\": \"\"}}}}");

            // Act
            var response = await _heosPlayer!.SetMuteAsync(pid, mute);

            // Assert
            _mockClient!.Verify(c => c.SendCommandAsync($"heos://player/set_mute?pid={pid}&state=on"), Times.Once);
            Assert.AreEqual("success", response.Result);
        }

        [TestMethod]
        public async Task GetMuteAsync_SendsCorrectCommand()
        {
            // Arrange
            int pid = 12345;
            _mockClient!.Setup(c => c.SendCommandAsync(It.IsAny<string>()))
                       .ReturnsAsync($"{{\"heos\": {{\"command\": \"player/get_mute?pid={pid}\", \"result\": \"success\", \"message\": \"\"}}, \"payload\": {{\"state\": \"on\"}}}}");

            // Act
            var response = await _heosPlayer!.GetMuteAsync(pid);

            // Assert
            _mockClient!.Verify(c => c.SendCommandAsync($"heos://player/get_mute?pid={pid}"), Times.Once);
            Assert.AreEqual("success", response.Result);
        }
    }
}