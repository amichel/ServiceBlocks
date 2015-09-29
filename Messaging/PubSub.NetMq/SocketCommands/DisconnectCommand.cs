using NetMQ;

namespace ServiceBlocks.Messaging.NetMq.SocketCommands
{
    public class DisconnectCommand : ISocketCommand
    {
        private readonly string _address;

        public DisconnectCommand(string address)
        {
            _address = address;
        }

        public void Execute(NetMQSocket socket)
        {
            socket.Disconnect(_address);
        }
    }
}