using System.Net.WebSockets;

namespace RealTimeServerServer.Websockets.Interfaces;

public interface IWebSocketHandler
{
    public Task Handle();
}