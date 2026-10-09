namespace Whitelabel_backoffice.Services.Messaging.IntegratedApi
{
    public class GameTransactionRequest
    {
        public string UserCode { get; set; }
        public string VendorCode { get; set; }
        public string GameCode { get; set; }
        public decimal Amount { get; set; }
        public string Detail { get; set; }
    }
    public class BatchGameTransactionRequest
    {
        public string UserCode { get; set; }
        public List<GameTransactionRequest> Transactions { get; set; }
    }
}
