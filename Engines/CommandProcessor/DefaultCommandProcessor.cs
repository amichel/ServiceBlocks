using System;
using System.Threading.Tasks;
using ServiceBlocks.Engines.QueuedTaskPool;

namespace ServiceBlocks.Engines.CommandProcessor
{
    public class DefaultCommandProcessor<TState, TKey, TCommand> : ICommandProcessor<TState, TKey, TCommand>
        where TCommand : Command<TState>
    {
        private TaskPool<TKey, TCommand> _taskPool;
        //TODO: add overload with pool config settings/builder
        public void Init(Action<Exception> errorAction)
        {
            _taskPool = new TaskPool<TKey, TCommand>(() => (key, command) => command.Execute(),
                onErrorAction: errorAction);
        }

        public async void Execute(TKey key, TCommand command, Action<TState> commandCompletedAction)
        {
            TState state = await ExecuteAsync(key, command);

            if (commandCompletedAction != null)
                commandCompletedAction(state);
        }

        public TState Execute(TKey key, TCommand command)
        {
            TState newState = default(TState);
            Execute(key, command, s => newState = s);
            return newState;
        }

        public Task<TState> ExecuteAsync(TKey key, TCommand command)
        {
            Task<TState> task = command.Completed();
            _taskPool.Add(key, command);
            return task;
        }

        public void ExecuteAndForget(TKey key, TCommand command)
        {
            _taskPool.Add(key, command);
        }
    }
}