using System;
using System.Configuration;

namespace FeedEngine.Client.Console
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            string address = ConfigurationManager.AppSettings["ServerAddress"];
            string snapshotaddress = ConfigurationManager.AppSettings["SnapshotServerAddress"];

            var manager = new QuoteSubscriptionManager(address, snapshotaddress,
                q => System.Console.WriteLine("InstrumentId={0} Bid={1} Offer={2} Time={3:HH:mm:ss.fff} V={4}", q.Instrument, q.Bid, q.Offer, q.ProcessedTime, q.Version));

            manager.Start();
            System.Console.ReadKey();
            manager.Stop();
        }

    }
}