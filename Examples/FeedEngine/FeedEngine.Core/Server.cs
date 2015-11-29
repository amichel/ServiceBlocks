using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using FeedEngine.Contracts;
using ServiceBlocks.Common.Serializers;
using ServiceBlocks.Common.Threading;
using ServiceBlocks.Common.Utilities;
using ServiceBlocks.Messaging.Common;
using ServiceBlocks.Messaging.NetMq;

namespace FeedEngine.Gateway
{
    public class Server : IDisposable, ITaskWorker
    {
        private readonly string _publisherAddress;
        private readonly string _snapshotAddress;
        private readonly string _gatewayAddress;
        private readonly Action<Exception> _errorLoggerAction;

        private static readonly DefaultLastValueCache<string, Quote> QuotesCache =
           new DefaultLastValueCache<string, Quote>();

        private FeedProcessor _processor;
        private NetMqPublisher _publisher;
        private NetMqSnapshotServer _snapshotServer;
        private NetMqPushAcceptor _gateway;

        public Server(string publisherAddress, string snapshotAddress, string gatewayAddress, Action<Exception> errorLoggerAction)
        {
            _publisherAddress = publisherAddress;
            _snapshotAddress = snapshotAddress;
            _gatewayAddress = gatewayAddress;

            //TODO: use Serilog
            _errorLoggerAction = errorLoggerAction ?? (ex => Debug.WriteLine(ex));
        }

        public void Start()
        {
            //Debug.WriteLine(TimerResolution.SetResolution(500));

            _processor = new FeedProcessor(OnValidQuote, OnInvalidQuote);
            _processor.Start();

            _snapshotServer = new NetMqSnapshotServer(_snapshotAddress, CreateSnapshot);
            _snapshotServer.Start();

            _publisher = new NetMqPublisher(_publisherAddress);
            _publisher.Start();

            _gateway = new NetMqPushAcceptor(_gatewayAddress);
            _gateway.Subscribe(Constants.QuotesTopicName, message =>
            {
                //TODO: try use protobuff
                var rawQuotes = BinarySerializer<IEnumerable<Quote>>.DeSerializeFromByteArray(message);
                _processor.ProcessQuotes(rawQuotes);
            });
            _gateway.Start();
        }

        public void Stop(int timeout = -1)
        {
            _gateway.Stop(timeout);
            _processor.Stop(timeout);
            _publisher.Stop(timeout);
            _snapshotServer.Stop(timeout);
        }

        private void OnValidQuote(Quote quote)
        {
            QuotesCache[quote.Instrument] = quote;

            //TODO: can be throttled to publish bulks of quotes - currently let ZMQ do the job
            _publisher.Publish(Constants.QuotesTopicName, BinarySerializer<Quote>.SerializeToByteArray(quote));
        }

        private void OnInvalidQuote(Quote quote)
        {
            LogInvalidQuote(quote);
        }

        private void LogInvalidQuote(Quote quote)
        {
            //TODO: Use Serilog
            Debug.WriteLine("Invalid Quote. Instrument={0}", quote.Instrument);
        }

        private byte[] CreateSnapshot(string topic)
        {
            switch (topic)
            {
                case Constants.QuotesTopicName:
                    IEnumerable<Quote> quotes = QuotesCache.Values;
                    return BinarySerializer<IEnumerable<Quote>>.SerializeToByteArray(quotes);
                default:
                    _errorLoggerAction(new NotImplementedException(string.Format("Snapshot server not implemented for topic {0}", topic)));
                    break;
            }
            return null;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}