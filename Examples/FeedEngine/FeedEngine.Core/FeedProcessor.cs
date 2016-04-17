using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FeedEngine.Contracts;
using ServiceBlocks.Common.Threading;
using ServiceBlocks.Engines.CommandProcessor;
using ServiceBlocks.Engines.QueuedTaskPool;

namespace FeedEngine.Gateway
{
    internal class FeedProcessor : IDisposable, ITaskWorker
    {
        private readonly Action<Quote> _onValidQuoteAction;
        private readonly Action<Quote> _onInValidQuoteAction;
        readonly DefaultCommandProcessor<InstrumentFeedState, string, FeedProcessorCommand> _processor = new DefaultCommandProcessor<InstrumentFeedState, string, FeedProcessorCommand>();
        readonly ConcurrentDictionary<string, InstrumentFeedState> _feedState = new ConcurrentDictionary<string, InstrumentFeedState>();

        public FeedProcessor(Action<Quote> onValidQuoteAction = null, Action<Quote> onInValidQuoteAction = null)
        {
            _onValidQuoteAction = onValidQuoteAction;
            _onInValidQuoteAction = onInValidQuoteAction;
        }

        public void ProcessQuotes(IEnumerable<Quote> rawQuotes)
        {
            var tasks = new List<Task<InstrumentFeedState>>();
            foreach (var quote in rawQuotes)
            {
                var state = _feedState.GetOrAdd(quote.Instrument, k => new InstrumentFeedState(k));
                _processor.ExecuteAndForget(quote.Instrument, new ProcessNewQuoteCommand(state, quote, _onValidQuoteAction, _onInValidQuoteAction));
            }
        }

        public void Start()
        {
            _processor.Init(ex => Debug.WriteLine(ex)); //TODO: inject Serilog
        }

        public void Stop(int timeout = -1)
        {
        }

        public void Dispose()
        {
            Stop();
        }
    }
}