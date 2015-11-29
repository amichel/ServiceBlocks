using System;

namespace FeedEngine.Contracts
{
    [Serializable]
    public class Quote
    {
        public Quote()
        {

        }

        public string Instrument { get; set; }
        public double Bid { get; set; }
        public double Offer { get; set; }
        public bool IsValid { get; set; }
        public DateTime SentTime { get; set; }
        public DateTime ProcessedTime { get; set; }
        public long Version { get; set; }
    }
}
