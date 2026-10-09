using Titan.Repository;
using Titan.Repository.Database;

namespace MyStake.Services.Messaging.Common.Client
{
	public class UpdateCancelBettingLogRequest
	{
		public long HistoryId { get; set; }
		public string VendorCode { get; set; } = string.Empty; // ProviderCode
		public decimal Amount { get; set; }
		public decimal AfterBalance { get; set; }
		public string Detail { get; set; } = string.Empty;
		public decimal BetAmount { get; set; }
		public decimal WinAmount { get; set; }
		public User User { get; set; }
	}
}
