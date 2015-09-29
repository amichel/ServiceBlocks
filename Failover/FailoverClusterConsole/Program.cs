using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ServiceBlocks.Failover.FailoverCluster;
using ServiceBlocks.Failover.FailoverCluster.Monitor;

namespace FailoverClusterConsole
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Task.Run(() =>
            {
                new ClusterMonitorBuilder()
                    .ListenOn("tcp://localhost:7776")
                    .ConnectTo("tcp://localhost:7777")
                    .WithRole(NodeRole.Primary)
                    .TimeoutAfter(5000)
                    .WaitForConnection(0)
                    .WhenConnecting(() => Console.WriteLine("Primary: Connecting"))
                    .WhenActive(() => Console.WriteLine("Primary: I am Active"))
                    .WhenPassive(() => Console.WriteLine("Primary: I am Passive"))
                    .WhenStopped(() => Console.WriteLine("Primary: I stopped"))
                    .OnClusterException(HandleClusterException)
                    .Create();
            });

            Task.Run(() =>
            {
                new ClusterMonitorBuilder()
                    .ListenOn("tcp://localhost:7777")
                    .ConnectTo("tcp://localhost:7776")
                    .WithRole(NodeRole.Backup)
                    .TimeoutAfter(5000)
                    .WaitForConnection(0)
                    .WhenConnecting(() => Console.WriteLine("Backup: Connecting"))
                    .WhenActive(() => Console.WriteLine("Backup: I am Active"))
                    .WhenPassive(() => Console.WriteLine("Backup: I am Passive"))
                    .WhenStopped(() => Console.WriteLine("Backup: I stopped"))
                    .OnClusterException(HandleClusterException)
                    .Create();
            });

            Console.ReadKey();
        }

        private static void HandleClusterException(ClusterException exception)
        {
            Console.WriteLine("Cluster Failure. Reason:{0} Local:{1} Remote:{2}", exception.Reason,
                JsonConvert.SerializeObject(exception.LocalState), JsonConvert.SerializeObject(exception.RemoteState));
        }
    }
}