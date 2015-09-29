using System;
using System.Collections.Concurrent;
using System.Configuration;
using QuotesContracts;

namespace QuoteSubscriberConsole
{
    internal class Program
    {
        private static readonly ConcurrentDictionary<ushort, Quote> QuotesCache =
            new ConcurrentDictionary<ushort, Quote>();

        private static void Main(string[] args)
        {
            string address = ConfigurationManager.AppSettings["ServerAddress"];
            string snapshotaddress = ConfigurationManager.AppSettings["SnapshotServerAddress"];

            var manager = new QuoteSubscriptionManager(address, snapshotaddress,
                q =>
                    Console.WriteLine(
                        string.Format("InstrumentId={0} SourceId={1} Bid={2} Ask={3} Time={4:HH:mm:ss.fff}",
                            q.InstrumentId, q.SourceId, q.Bid, q.Ask, q.TradeTime)));

            manager.Start();

            Console.ReadKey();
            manager.Stop();

            //var address = ConfigurationManager.AppSettings["ServerAddress"];
            //var subscriber = new NetMqSubscriber(address);
            //subscriber.Subscribe(new TopicSubscription<IEnumerable<Quote>>()
            //{
            //    Topic = "q",
            //    MessageHandler = qs => qs.All(q => { UpdateQuote(q); return true; }),
            //    Deserializer = BinarySerializer<IEnumerable<Quote>>.DeSerializeFromByteArray
            //});

            //Console.WriteLine("Starting Producer- connecting subscriber");
            //if (!subscriber.StartProducer(60000))
            //{
            //    Console.WriteLine("Starting Producer FAILED- connecting subscriber - TIMEDOUT");
            //    Console.ReadKey();
            //    return;
            //}

            //Console.WriteLine("Get snapshot");
            //var snapshotClient = new NetMqSnapshotClient("tcp://localhost:5202");
            //var quotes = snapshotClient.GetSnapshot("q", BinarySerializer<IEnumerable<Quote>>.DeSerializeFromByteArray);

            //foreach (var q in quotes)
            //    UpdateQuote(q);

            //Console.ReadKey();
            //Console.WriteLine("Starting Consumer");
            //subscriber.StartConsumer();

            //Console.ReadKey();
        }

        private static void UpdateQuote(Quote q)
        {
            bool isNewer = false;
            Quote cachedQuote = null;
            if (QuotesCache.TryGetValue(q.InstrumentId, out cachedQuote))
            {
                if (q.LastUpdate > cachedQuote.LastUpdate)
                    isNewer = true;
            }
            else
                isNewer = true;

            if (isNewer)
            {
                QuotesCache[q.InstrumentId] = q;
                Console.WriteLine("InstrumentId={0} SourceId={1} Bid={2} Ask={3} Time={4:HH:mm:ss.fff}", q.InstrumentId,
                    q.SourceId, q.Bid, q.Ask, q.TradeTime);
            }
            else
                Console.WriteLine("Older quote dissmissed");
        }
    }
}