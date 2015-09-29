using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FeedEngine.Contracts;
using ServiceBlocks.Common.Serializers;
using ServiceBlocks.Messaging.NetMq;
using Topshelf;

namespace FeedEngine.VirtualFeed
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x => x.Service<Feed>());
        }
    }
}
