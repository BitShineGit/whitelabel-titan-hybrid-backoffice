namespace MyStake.Services.Messaging.IntegratedApi
{
    public class GameTransactionRequest
    {
        public string UserCode { get; set; }

        public string VendorCode { get; set; }

        public string GameCode { get; set; }

        public decimal Amount { get; set; }

        public long HistoryId { get; set; }

        public string RoundId { get; set; }

        public string Detail { get; set; }

        public bool IsFinished { get; set; }

        public bool IsCanceled { get; set; }
    }
    public class BatchGameTransactionRequest
    {
        public string UserCode { get; set; }
        public List<GameTransactionRequest> Transactions { get; set; }
    }
}
