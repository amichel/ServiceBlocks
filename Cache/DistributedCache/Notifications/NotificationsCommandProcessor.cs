using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ServiceBlocks.Engines.CommandProcessor;

namespace ServiceBlocks.DistributedCache.Notifications
{
    public class NotificationsCommandProcessor : DefaultCommandProcessor<SubscriberState, Type, Command<SubscriberState>>
    {
    }
}
