using System;
using System.Text;
using Newtonsoft.Json;
using ServiceBlocks.Messaging.Common;
using ServiceBlocks.Messaging.NetMq;

namespace ServiceBlocks.Failover.FailoverCluster.Monitor
{
    public class PubSubProxy
    {
        private readonly int _connectTimeout;
        private readonly Action<bool> _connectionStateAction;
        private readonly string _localAddress;
        private readonly string _partnerAddress;
        private readonly Action<NodeState> _updateAction;
        private NetMqPublisher _publisher;
        private NetMqSubscriber _subscriber;

        public PubSubProxy(string localAddress, string partnerAddress, Action<NodeState> updateAction,
            Action<bool> connectionStateAction, int connectTimeout = 0)
        {
            _localAddress = localAddress;
            _partnerAddress = partnerAddress;

            if (updateAction == null) throw new ArgumentNullException("updateAction");
            if (connectionStateAction == null) throw new ArgumentNullException("connectionStateAction");

            _updateAction = updateAction;
            _connectionStateAction = connectionStateAction;

            _connectTimeout = connectTimeout;
        }

        public IPublisher Publisher
        {
            get { return _publisher; }
        }

        public ISubscriber Subscriber
        {
            get { return _subscriber; }
        }

        public bool Start()
        {
            _publisher = new NetMqPublisher(_localAddress);
            _publisher.Start();

            _subscriber = new NetMqSubscriber(_partnerAddress, new DefaultConnectionMonitor(OnConnectionStateChanged));
            if (!_subscriber.StartProducer(_connectTimeout))
                return false;
            _subscriber.Subscribe(new TopicSubscription<NodeState>
            {
                Topic = "Cluster",
                MessageHandler = _updateAction,
                Deserializer = DeserializeState
            });
            _subscriber.StartConsumer();
            return true;
        }

        public void Publish(NodeState state)
        {
            _publisher.Publish("Cluster", state, SerializeState);
        }

        public void Stop()
        {
            _publisher.Stop();
            _subscriber.Stop();
        }

        private byte[] SerializeState(NodeState state)
        {
            return Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(state));
        }

        private NodeState DeserializeState(byte[] message)
        {
            return JsonConvert.DeserializeObject<NodeState>(Encoding.ASCII.GetString(message));
        }

        private void OnConnectionStateChanged(IConnectionMonitor source, bool state)
        {
            _connectionStateAction(state);
        }
    }
}