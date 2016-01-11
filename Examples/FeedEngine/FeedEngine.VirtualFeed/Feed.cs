using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using FeedEngine.Contracts;
using Newtonsoft.Json;
using ServiceBlocks.Common.Serializers;
using ServiceBlocks.Common.Threading;
using ServiceBlocks.Failover.FailoverCluster;
using ServiceBlocks.Failover.FailoverCluster.Monitor;
using ServiceBlocks.Messaging.NetMq;
using Topshelf;

namespace FeedEngine.VirtualFeed
{
    public class Feed : ServiceControl
    {
        private readonly IEnumerable<NetMqPusher> _pushers;
        private readonly ITaskWorker _worker;
        private readonly ClusterMonitor _clusterMonitor;
        private readonly string[] _instruments = { "EURUSD", "GBPUSD", "USDJPY", "USDCHF", "AUDUSD", "NZDUSD" };

        public Feed()
        {
            string address = ConfigurationManager.AppSettings["GatewayServerAddress"];
            _pushers = address.Split(';').Select(adr => new NetMqPusher(adr)).ToArray();
            _worker = new TaskWorker(GenerateFeed);
            _clusterMonitor = CreateCluster();
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

                    foreach (var pusher in _pushers)
                        pusher.Publish(Constants.QuotesTopicName, quotes, BinarySerializer<IEnumerable<Quote>>.SerializeToByteArray);
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
            if (_clusterMonitor.Role == NodeRole.StandAlone)
                StartFeed();
            else
                _clusterMonitor.Start();
            return true;
        }

        private ClusterMonitor CreateCluster()
        {
            var nodeRole = (NodeRole)Enum.Parse(typeof(NodeRole), ConfigurationManager.AppSettings["ClusterNodeRole"]);
            return new ClusterMonitorBuilder()
                    .ListenOn(ConfigurationManager.AppSettings["ClusterSelfAddress"])
                    .ConnectTo(ConfigurationManager.AppSettings["ClusterPartnerAddress"])
                    .WithRole(nodeRole)
                    .TimeoutAfter(5000)
                    .WaitForConnection(0)
                    .BecomeActiveWhenPrimaryOnInitialConnectionTimeout()
                    .WhenConnecting(() => Console.WriteLine("{0}: Connecting", nodeRole))
                    .WhenActive(() => { StartFeed(); Console.WriteLine("{0}: Active", nodeRole); })
                    .WhenPassive(() => Console.WriteLine("{0}: Passive", nodeRole))
                    .WhenStopped(() => { StopFeed(); Console.WriteLine("{0}: Stopped", nodeRole); })
                    .OnClusterException(HandleClusterException)
                    .Create(false);
        }

        private void StartFeed()
        {
            _pushers.All(x =>
            {
                x.Start();
                return true;
            });
            _worker.Start();
        }

        public bool Stop(HostControl hostControl)
        {
            if (_clusterMonitor.Role == NodeRole.StandAlone)
                StopFeed();
            else
                _clusterMonitor.Stop();
            return true;
        }

        private void StopFeed()
        {
            _worker.Stop();
            _pushers.All(x =>
            {
                x.Stop();
                return true;
            });
        }
        private static void HandleClusterException(ClusterException exception)
        {
            Console.WriteLine("Cluster Failure. Reason:{0} Local:{1} Remote:{2}", exception.Reason,
                JsonConvert.SerializeObject(exception.LocalState), JsonConvert.SerializeObject(exception.RemoteState));
        }
    }
}
