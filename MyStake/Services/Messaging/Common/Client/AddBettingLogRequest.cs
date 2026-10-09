using MyStake.Constants.Enums;
using Titan.Repository;
using Titan.Repository.Database;

namespace MyStake.Services.Messaging.Common.Client
{
    public class AddBettingLogRequest
    {
		public int CustomerId { get; set; }        // -> UserTableId
		public string UserCode { get; set; } = string.Empty;
		public int AgentTableId { get; set; }
		public string AgentCode {  get; set; }
		public string AgentPath { get; set; }
		public int ProviderId { get; set; }
		public string ProviderName { get; set; }
		public string VendorCode { get; set; } = string.Empty; // -> ProviderCode
		public string GameName {  get; set; }
		public string GameCode { get; set; } = string.Empty;
		public string RoundId { get; set; } = string.Empty;
		public long HistoryId { get; set; }
		public decimal BetAmount { get; set; }
		public decimal WinAmount { get; set; }
		public decimal BeforeBalance { get; set; }
		public decimal AfterBalance { get; set; }
		public string Detail { get; set; } = string.Empty;
		public BettingLogType Status { get; set; }
		public User User { get; set; }
	}
}
