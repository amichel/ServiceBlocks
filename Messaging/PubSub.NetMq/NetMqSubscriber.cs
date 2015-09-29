using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using NetMQ.zmq;
using ServiceBlocks.Messaging.Common;
using ServiceBlocks.Messaging.NetMq.SocketCommands;

namespace ServiceBlocks.Messaging.NetMq
{
    public class NetMqSubscriber : BufferedSubscriber<NetMQMessage>
    {
        private readonly ICommandProcessor _commandProcessor = new CommandProcessor();
        private readonly ManualResetEventSlim _connectHandle = new ManualResetEventSlim();
        private readonly IConnectionMonitor _connectionMonitor;

        public NetMqSubscriber(string address, IConnectionMonitor connectionMonitor = null)
        {
            Address = address;
            _connectionMonitor = connectionMonitor ?? new DefaultConnectionMonitor();
        }

        private string Address { get; set; }

        protected override void ProducerAction()
        {
            using (NetMQContext ctx = NetMQContext.Create())
            {
                using (SubscriberSocket socket = ctx.CreateSubscriberSocket())
                {
                    using (
                        var monitor = new NetMQMonitor(ctx, socket, "inproc://monitor.sub/" + Guid.NewGuid(),
                            SocketEvent.Connected | SocketEvent.Disconnected))
                    {
                        monitor.Connected += monitor_Connected;
                        monitor.Disconnected += monitor_Disconnected;
                        monitor.Timeout = TimeSpan.FromMilliseconds(100);
                        Task.Factory.StartNew(monitor.Start);

                        socket.Connect(Address);
                        socket.Subscribe(string.Empty);
                        while (true)
                        {
                            _commandProcessor.ExecuteAll(socket);
                            NetMQMessage message = socket.ReceiveMessage();
                            if (message != null && !IsEmpty)
                                Queue.Add(message);
                        }
                    }
                }
            }
        }

        protected override void SubscribeInternally(string topic)
        {
            _commandProcessor.Add(new SubscribeCommand(topic));
        }


        public new void Unsubscribe(string topic)
        {
            _commandProcessor.Add(new UnsubscribeCommand(topic));
            base.Unsubscribe(topic);
        }

        private void monitor_Disconnected(object sender, NetMQMonitorSocketEventArgs e)
        {
            try
            {
                _connectionMonitor.IsConnected = false;
                if (_connectHandle.IsSet) _connectHandle.Reset();
            }
            catch
            {
            } //shallow catch to avoid monitor stop running
        }

        private void monitor_Connected(object sender, NetMQMonitorSocketEventArgs e)
        {
            try
            {
                _connectionMonitor.IsConnected = true;
                if (!_connectHandle.IsSet) _connectHandle.Set();
            }
            catch
            {
            } //shallow catch to avoid monitor stop running
        }

        public bool StartProducer(int waitConnectInterval = 5000)
        {
            base.StartProducer();

            if (waitConnectInterval > 0)
                return _connectHandle.Wait(waitConnectInterval);
            return true;
        }

        protected override void ConsumeMessage(NetMQMessage message)
        {
            byte[] body;
            string topic;
            message.Parse(out body, out topic);
            InvokeSubscription(topic, body);
        }

        protected override void ConsumeError(Exception ex)
        {
            //TODO: inject logger/error handler
            Debug.WriteLine(ex.ToString());
        }
    }
}