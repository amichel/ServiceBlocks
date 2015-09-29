using System;
using Topshelf;

namespace FeedEngine.Gateway.Host
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x => x.Service<FeedService>());
        }
    }
}
