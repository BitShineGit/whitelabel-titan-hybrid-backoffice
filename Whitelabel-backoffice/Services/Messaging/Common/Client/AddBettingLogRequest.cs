using Whitelabel_backoffice.Constants.Enums;

namespace Whitelabel_backoffice.Services.Messaging.Common.Client
{
    public class AddBettingLogRequest
    {
        public int CustomerId { get; set; }
        public string VendorCode { get; set; }
        public string GameCode { get; set; }
        public Decimal BetAmount { get; set; }
        public Decimal WinAmount { get; set; }
        public Decimal BeforeBalance { get; set; }
        public Decimal AfterBalance { get; set; }
        public string RoundId { get; set; }
        public int WagerId { get; set; }
        public string Detail { get; set; }
        public BettingLogType Status { get; set; }
    }
}
