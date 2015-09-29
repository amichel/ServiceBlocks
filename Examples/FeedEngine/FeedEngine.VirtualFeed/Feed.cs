using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FeedEngine.Contracts;
using ServiceBlocks.Common.Serializers;
using ServiceBlocks.Common.Threading;
using ServiceBlocks.Messaging.Common;
using ServiceBlocks.Messaging.NetMq;
using Topshelf;

namespace FeedEngine.VirtualFeed
{
    public class Feed : ServiceControl
    {
        private readonly NetMqPusher _pusher;
        private readonly ITaskWorker _worker;
        private string[] _instruments = { "EURUSD", "GBPUSD", "USDJPY", "USDCHF", "AUDUSD", "NZDUSD" };

        public Feed()
        {
            string address = ConfigurationManager.AppSettings["GatewayServerAddress"];
            _pusher = new NetMqPusher(address);
            _worker = new TaskWorker(GenerateFeed);
        }

        private void GenerateFeed()
        {
            int i = 0;
            int a = i;
            while (true)
            {
                Thread.Sleep(100);
                try
                {
                    var quotes = new List<Quote>();
                    for (int j = 0; j < _instruments.Length; j++)
                    {
                        quotes.Add(new Quote
                        {
                            Instrument = _instruments[j],
                            Bid = a++,
                            Offer = a + 1,
                            SentTime = DateTime.UtcNow
                        });
                    }

                    _pusher.Publish(Constants.QuotesTopicName, quotes, BinarySerializer<IEnumerable<Quote>>.SerializeToByteArray);
                }
                catch (Exception ex)
                {
                    Thread.Sleep(1000);
                }
                if (++i > 100) i = 0;
            }
        }

        public bool Start(HostControl hostControl)
        {
            _pusher.Start();
            _worker.Start();
            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            _worker.Stop();
            _pusher.Stop();
            return true;
        }
    }
}
