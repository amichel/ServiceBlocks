using System;
using System.Configuration;
using System.Diagnostics;
using Topshelf;

namespace FeedEngine.Gateway.Host
{
    internal class FeedService : ServiceControl
    {
        private Server _server;
        public bool Start(HostControl hostControl)
        {
            string address = ConfigurationManager.AppSettings["ServerAddress"];
            string snapshotaddress = ConfigurationManager.AppSettings["SnapshotServerAddress"];
            string gatewayaddress = ConfigurationManager.AppSettings["GatewayServerAddress"];
            _server = new Server(address, snapshotaddress, gatewayaddress, LogError);
            _server.Start();
            return true;
        }

        private void LogError(Exception ex)
        {
            Debug.WriteLine(ex); //TODO: use Serilog
        }

        public bool Stop(HostControl hostControl)
        {
            _server.Stop();
            return true;
        }
    }
}