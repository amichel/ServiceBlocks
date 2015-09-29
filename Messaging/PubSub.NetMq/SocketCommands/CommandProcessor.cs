using System.Collections.Concurrent;
using NetMQ;

namespace ServiceBlocks.Messaging.NetMq.SocketCommands
{
    public class CommandProcessor : ICommandProcessor
    {
        private readonly ConcurrentQueue<ISocketCommand> _commands = new ConcurrentQueue<ISocketCommand>();

        public void Add(ISocketCommand command)
        {
            _commands.Enqueue(command);
        }

        public void ExecuteAll(NetMQSocket socket)
        {
            ISocketCommand command;
            while (_commands.TryDequeue(out command))
                command.Execute(socket);
        }
    }
}