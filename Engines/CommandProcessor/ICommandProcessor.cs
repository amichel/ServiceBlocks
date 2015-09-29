using System;
using System.Threading.Tasks;

namespace ServiceBlocks.Engines.CommandProcessor
{
    public interface ICommandProcessor<TState, in TKey, in TCommand> where TCommand : Command<TState>
    {
        void Init(Action<Exception> errorAction);
        void Execute(TKey key, TCommand command, Action<TState> commandCompletedAction);
        TState Execute(TKey key, TCommand command);
        Task<TState> ExecuteAsync(TKey key, TCommand command);
        void ExecuteAndForget(TKey key, TCommand command);
    }
}