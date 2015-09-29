using FeedEngine.Contracts;

namespace FeedEngine.Gateway
{
    internal class InstrumentFeedState
    {
        public string Instrument { get; private set; }
        public Quote LastQuote { get; set; }
        public Quote LastValidQuote { get; set; }
        public InstrumentFeedState(string instrument)
        {
            Instrument = instrument;
        }
    }
}