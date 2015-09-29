using ServiceBlocks.Engines.CommandProcessor;

namespace FeedEngine.Gateway
{
    internal abstract class FeedProcessorCommand : Command<InstrumentFeedState>
    {
        public FeedProcessorCommand(InstrumentFeedState state)
            : base(state)
        {
        }
    }
}