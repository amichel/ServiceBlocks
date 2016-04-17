using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ServiceBlocks.Engines.CommandProcessor;

namespace ServiceBlocks.DistributedCache.Notifications
{
    public class PublishCommand : Command<SubscriberState>
    {
        private readonly string _key;

        public PublishCommand(SubscriberState state, string key) : base(state)
        {
            _key = key;
        }

        protected override void ExecuteCommand()
        {
            State.Invoke(_key);
        }
    }
}
