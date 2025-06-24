using RealTimeServerServer.Models;

namespace RealTimeServerServer.Websockets.Interfaces;

public interface IMessageCommandProcessor
{
    Task BroadcastMessage(Message message, string socketId);
    Task GetMessages(int channelId, string socketId);
}
