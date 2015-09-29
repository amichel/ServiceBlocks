using System;
using System.Collections.Generic;
using System.Diagnostics;
using QuotesContracts;
using ServiceBlocks.Common.Serializers;
using ServiceBlocks.Common.Threading;
using ServiceBlocks.Messaging.Common;
using ServiceBlocks.Messaging.NetMq;

namespace QuoteSubscriberConsole
{
    public class QuoteSubscriptionManager : ITaskWorker
    {
        private readonly string _pubsubServerAddress;
        private readonly Action<Quote> _quoteReceivedAction;
        private readonly DefaultLastValueCache<ushort, Quote> _quotesCache = new DefaultLastValueCache<ushort, Quote>();
        private readonly string _snapshotServerAddress;

        private readonly List<TopicSynchronizer<ushort, Quote, IEnumerable<Quote>>> _synchronizers =
            new List<TopicSynchronizer<ushort, Quote, IEnumerable<Quote>>>();

        private NetMqSubscriber _subscriber;

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
                Topic = "q",
                Deserializer = BinarySerializer<Quote>.DeSerializeFromByteArray,
                MessageHandler = _quoteReceivedAction
            };

            _synchronizers.Add(new TopicSynchronizer<ushort, Quote, IEnumerable<Quote>>(_subscriber,
                quotesTopic,
                _quotesCache,
                CreateSnapshotClient,
                (q1, q2) => q2.LastUpdate > q1.LastUpdate,
                q => true, q => q.InstrumentId, x => x));

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
                case "q":
                    return new QuoteSnapshotClient(_snapshotServerAddress);
                default:
                    throw new NotImplementedException();
            }
        }
    }
}