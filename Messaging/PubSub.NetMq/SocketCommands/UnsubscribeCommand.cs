using NetMQ;

namespace ServiceBlocks.Messaging.NetMq.SocketCommands
{
    public class UnsubscribeCommand : ISocketCommand
    {
        private readonly string _topic;

        public UnsubscribeCommand(string topic)
        {
            _topic = topic;
        }

        public void Execute(NetMQSocket socket)
        {
#pragma warning disable 618
            socket.Unsubscribe(_topic);
#pragma warning restore 618
        }
    }
}