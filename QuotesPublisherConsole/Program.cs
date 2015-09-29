using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using QuotesContracts;
using ServiceBlocks.Common.Serializers;
using ServiceBlocks.Messaging.NetMq;

namespace QuotesPublisherConsole
{
    internal class Program
    {
        private static readonly ConcurrentDictionary<ushort, Quote> QuotesCache =
            new ConcurrentDictionary<ushort, Quote>();

        private static void Main(string[] args)
        {
            string address = ConfigurationManager.AppSettings["ServerAddress"];
            string snapshotaddress = ConfigurationManager.AppSettings["SnapshotServerAddress"];
            string gatewayaddress = ConfigurationManager.AppSettings["GatewayServerAddress"];

            var snapshotServer = new NetMqSnapshotServer(snapshotaddress, CreateSnapshot);
            snapshotServer.Start();

            var publisher = new NetMqPublisher(address);
            publisher.Start();

            var gateway = new NetMqPushAcceptor(gatewayaddress);
            gateway.Subscribe("q", message =>
            {
                Quote q = BinarySerializer<Quote>.DeSerializeFromByteArray(message);
                QuotesCache[q.InstrumentId] = q;
                publisher.Publish("q", message);
            });
            gateway.Start();

            Console.ReadKey();
        }

        private static byte[] CreateSnapshot(string topic)
        {
            if (topic == "q")
            {
                IEnumerable<Quote> quotes = QuotesCache.Values;
                return BinarySerializer<IEnumerable<Quote>>.SerializeToByteArray(quotes);
            }
            return null;
        }
    }
}