using NetMQ;

namespace ServiceBlocks.Messaging.NetMq.SocketCommands
{
    public class SubscribeCommand : ISocketCommand
    {
        private readonly string _topic;

        public SubscribeCommand(string topic)
        {
            _topic = topic;
        }

        public void Execute(NetMQSocket socket)
        {
#pragma warning disable 618
            socket.Subscribe(_topic);
#pragma warning restore 618
        }
    }
}