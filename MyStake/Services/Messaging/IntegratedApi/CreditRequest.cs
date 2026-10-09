
namespace MyStake.Services.Messaging.IntegratedApi
{
    public class CreditRequest
    {
        public string UserCode { get; set; }
        public string VendorCode { get; set; }
        public string GameCode { get; set; }
        public Decimal Amount { get; set; }
        public string RoundId { get; set; }
        public string Detail { get; set; }
        public int WagerId { get; set; }
    }
}
