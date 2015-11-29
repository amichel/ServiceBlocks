using System;
using System.Collections.Generic;
using System.Diagnostics;
using FeedEngine.Contracts;
using ServiceBlocks.Common.Serializers;
using ServiceBlocks.Common.Threading;
using ServiceBlocks.Messaging.Common;
using ServiceBlocks.Messaging.NetMq;

namespace FeedEngine.Client
{
    public class QuoteSubscriptionManager : ITaskWorker
    {
        private readonly string _pubsubServerAddress;
        private readonly Action<Quote> _quoteReceivedAction;
        private readonly DefaultLastValueCache<string, Quote> _quotesCache = new DefaultLastValueCache<string, Quote>();
        private readonly string _snapshotServerAddress;

        private readonly List<TopicSynchronizer<string, Quote, IEnumerable<Quote>>> _synchronizers =
            new List<TopicSynchronizer<string, Quote, IEnumerable<Quote>>>();

        private NetMqSubscriber _subscriber;

        public ILastValueCache<string, Quote> Cache { get { return _quotesCache; } }

        public QuoteSubscriptionManager(string pubsubServerAddress, string snapshotServerAddress,
            Action<Quote> quoteReceivedAction)
        {
            _quoteReceivedAction = quoteReceivedAction;
            _snapshotServerAddress = snapshotServerAddress;
            _pubsubServerAddress = pubsubServerAddress;
        }

        public void Start()
        {
            _subscriber = new NetMqSubscriber(_pubsubServerAddress,
                new DefaultConnectionMonitor(OnConnectionStateChanged));

            var quotesTopic = new TopicSubscription<Quote>
            {
                Topic = Constants.QuotesTopicName,
                Deserializer = BinarySerializer<Quote>.DeSerializeFromByteArray,
                MessageHandler = _quoteReceivedAction
            };

            _synchronizers.Add(new TopicSynchronizer<string, Quote, IEnumerable<Quote>>(_subscriber,
                quotesTopic,
                _quotesCache,
                CreateSnapshotClient,
                (q1, q2) => q2.Version == long.MinValue || q2.Version > q1.Version,
                q => true, q => q.Instrument, x => x));

            if (!_subscriber.StartProducer(60000))
            {
                Debug.WriteLine("Starting Producer FAILED- connecting subscriber - TIMEDOUT");
                return;
            }

            _subscriber.StartConsumer();
        }

        public void Stop(int timeout = -1)
        {
            foreach (var synchronizer in _synchronizers)
                synchronizer.Unsubscribe();

            _subscriber.Stop(timeout);
        }

        private void OnConnectionStateChanged(IConnectionMonitor source, bool state)
        {
            if (state)
            {
                foreach (var synchronizer in _synchronizers)
                    synchronizer.Init();
            }
        }


        private ISnapshotClient<IEnumerable<Quote>> CreateSnapshotClient(string topic) //TODO: resolve from factory
        {
            switch (topic)
            {
                case Constants.QuotesTopicName:
                    return new QuoteSnapshotClient(_snapshotServerAddress);
                default:
                    throw new NotImplementedException();
            }
        }
    }
}