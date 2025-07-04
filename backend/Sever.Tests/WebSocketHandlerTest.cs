using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using RealTimeServerServer.Data;
using RealTimeServerServer.Websockets;
using RealTimeServerServer.Websockets.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace RealTimeServerServer.Tests.Websockets
{
    [TestClass]
    public class WebSocketHandlerTests
    {
        private Mock<WebSocket> _mockWebSocket = null!;
        private Mock<IWebSocketCommandProcessor> _mockCommandProcessor = null!;
        private Mock<IMemoryStore> _mockMemoryStore = null!;
        private readonly JsonSerializerOptions _options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };

        // SUT 
        private WebSocketHandler _webSocketHandler = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockWebSocket = new Mock<WebSocket>();
            _mockCommandProcessor = new Mock<IWebSocketCommandProcessor>();
            _mockMemoryStore = new Mock<IMemoryStore>();
            _webSocketHandler = new WebSocketHandler(_mockWebSocket.Object, _mockCommandProcessor.Object, _mockMemoryStore.Object, _options);

            // Open the websocket state
            _mockWebSocket.SetupGet(w => w.State).Returns(WebSocketState.Open);

            // Setup the ReceiveAsync method to handle incoming JSON messages
            byte[] buffer = Encoding.UTF8.GetBytes("{\"command\":\"test\"}");
            var stream = new MemoryStream(buffer);

            WebSocketReceiveResult result = new(buffer.Length, WebSocketMessageType.Text, true);

            _mockWebSocket.Setup(w => w.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result)
                .Callback((ArraySegment<byte> buffer, CancellationToken token)
                => stream.Read(buffer.Array!, buffer.Offset, buffer.Count));
        }

        [TestMethod]
        public async Task Handle_ShouldAddSocketConnectionToMemoryStore()
        {
            // Act
            await _webSocketHandler.Handle();

            // Assert
            _mockMemoryStore.Verify(m => m.AddSocketConnection(It.IsAny<string>(), _mockWebSocket.Object), Times.Once);
        }

        [TestMethod]
        public async Task Handle_ShouldProcessCommandAsync()
        {
            // Act
            await _webSocketHandler.Handle();

            // Assert
            _mockCommandProcessor.Verify(c => c.ProcessCommandAsync(It.IsAny<WebSocketCommand>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task Handle_ShouldRemoveSocketConnectionFromMemoryStoreOnClose()
        {
            // Arrange
            _mockWebSocket.SetupGet(w => w.State).Returns(WebSocketState.CloseReceived);

            // Act
            await _webSocketHandler.Handle();

            // Assert
            _mockMemoryStore.Verify(m => m.RemoveSocketConnection(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task Handle_ShouldCloseWebSocketOnClientClose()
        {
            // Arrange
            WebSocketReceiveResult closeResult = new(0, WebSocketMessageType.Close, true);

            _mockWebSocket.SetupSequence(w => w.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(closeResult);

            _mockWebSocket.SetupSequence(w => w.State)
                .Returns(WebSocketState.Open)
                .Returns(WebSocketState.CloseReceived);

            // Act
            await _webSocketHandler.Handle();

            // Assert
            _mockCommandProcessor.Verify(c => c.UserDisconnected(It.IsAny<string>()), Times.Once);
            _mockWebSocket.Verify(w => w.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Handle_ShouldCatchExceptions()
        {
            // Arrange
            _mockCommandProcessor.Setup(c => c.ProcessCommandAsync(It.IsAny<WebSocketCommand>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            await _webSocketHandler.Handle();

            // Assert
            _mockMemoryStore.Verify(m => m.RemoveSocketConnection(It.IsAny<string>()), Times.Once);
        }	
    }
}
