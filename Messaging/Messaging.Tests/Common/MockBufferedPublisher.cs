using System;
using ServiceBlocks.Messaging.Common;

namespace ServiceBlocks.Messaging.Tests.Common
{
    public class MockBufferedPublisher : BufferedPublisher<MockMessage>
    {
        private readonly Action _consumerAction;

        public MockBufferedPublisher()
        {
        }

        public MockBufferedPublisher(Action consumerAction)
        {
            _consumerAction = consumerAction;
        }

        public int Count
        {
            get { return Queue.Count; }
        }

        protected override MockMessage CreateMessage(string topic, byte[] data)
        {
            return new MockMessage {Topic = topic, Data = data};
        }

        protected override void ConsumerAction()
        {
            if (_consumerAction != null)
                _consumerAction();
        }
    }
}