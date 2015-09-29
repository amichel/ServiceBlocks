using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using QuotesContracts;
using ServiceBlocks.Common.Serializers;
using ServiceBlocks.Messaging.NetMq;

namespace QuotesPusherConsole
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            string address = ConfigurationManager.AppSettings["GatewayServerAddress"];

            var pusher = new NetMqPusher(address);
            pusher.Start();

            for (int i = 0; i < 100; i++)
            {
                int i1 = i;
                Task.Factory.StartNew(() =>
                {
                    int a = i1;
                    int b = i1;
                    while (true)
                    {
                        Thread.Sleep(100);
                        try
                        {
                            var q = new Quote
                            {
                                InstrumentId = (ushort) (313*i1),
                                Bid = a++,
                                Ask = b++,
                                TradeTime = DateTime.Now,
                                LastUpdate = DateTime.Now
                            };

                            pusher.Publish("q", q, BinarySerializer<Quote>.SerializeToByteArray);
                        }
                        catch (Exception ex)
                        {
                            Thread.Sleep(1000);
                        }
                    }
                }, TaskCreationOptions.LongRunning);
            }
            Console.ReadKey();
        }
    }
}