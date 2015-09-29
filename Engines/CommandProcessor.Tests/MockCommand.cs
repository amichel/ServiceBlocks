using ServiceBlocks.Engines.CommandProcessor;

namespace ServiceBlocks.CommandProcessor.Tests
{
    public class MockCommand : Command<bool>
    {
        public MockCommand()
            : base(true)
        {
        }

        public MockCommand(bool state)
            : base(state)
        {
        }

        public bool CurrentState
        {
            get { return State; }
        }

        protected override void ExecuteCommand()
        {
            State = false;
        }
    }
}