using System.Net.WebSockets;

namespace RealTimeServerServer.Websockets.Interfaces;

public interface IWebSocketSender
{
    Task SendAsync(WebSocket socket, string message);
    Task SendErrorAsync(string socketId, string errorMessage);
}
