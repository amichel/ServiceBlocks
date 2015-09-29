using NetMQ;

namespace ServiceBlocks.Messaging.NetMq.SocketCommands
{
    public interface ICommandProcessor
    {
        void Add(ISocketCommand command);
        void ExecuteAll(NetMQSocket socket);
    }
}