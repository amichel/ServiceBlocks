using NetMQ;

namespace ServiceBlocks.Messaging.NetMq.SocketCommands
{
    public interface ISocketCommand
    {
        void Execute(NetMQSocket socket);
    }
}