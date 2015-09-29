using System;
using FeedEngine.Contracts;

namespace FeedEngine.Gateway
{
    internal class ProcessNewQuoteCommand : FeedProcessorCommand
    {
        private readonly Quote _quote;
        private readonly Action<Quote> _onValidQuoteAction;
        private readonly Action<Quote> _onInValidQuoteAction;

        public ProcessNewQuoteCommand(InstrumentFeedState state, Quote quote, Action<Quote> onValidQuoteAction, Action<Quote> onInValidQuoteAction)
            : base(state)
        {
            _quote = quote;
            _onValidQuoteAction = onValidQuoteAction;
            _onInValidQuoteAction = onInValidQuoteAction;
        }

        protected override void ExecuteCommand()
        {
            State.LastQuote = _quote;
            var isValid = IsValid(_quote);
            _quote.ProcessedTime = DateTime.UtcNow;
            if (isValid)
            {
                State.LastValidQuote = _quote;
                if (_onInValidQuoteAction != null)
                    _onValidQuoteAction(_quote);
            }
            else
            {
                if (_onInValidQuoteAction != null)
                    _onInValidQuoteAction(_quote);
            }
        }

        private bool IsValid(Quote quote)
        {
            return 0.00001 < quote.Bid && quote.Bid < quote.Offer && quote.Offer < 100000000;
        }
    }
}