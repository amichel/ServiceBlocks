using System;
using System.Threading.Tasks;

namespace ServiceBlocks.Engines.CommandProcessor
{
    public abstract class Command<TState>
    {
        protected Task<TState> CompletedTask;

        protected Command(TState state)
        {
            CreatedTime = DateTime.UtcNow;
            State = state;
        }

        protected TState State { get; set; }
        public DateTime CreatedTime { get; protected set; }
        public DateTime ExecuteStartedTime { get; protected set; }
        public DateTime ExecuteCompletedTime { get; protected set; }

        public void Execute()
        {
            ExecuteStartedTime = DateTime.UtcNow;
            ExecuteCommand();
            ExecuteCompletedTime = DateTime.UtcNow;
            if (CompletedTask != null)
                CompletedTask.Start();
        }

        public Task<TState> Completed()
        {
            CompletedTask = new Task<TState>(() => State);
            return CompletedTask;
        }

        protected abstract void ExecuteCommand();
    }
}