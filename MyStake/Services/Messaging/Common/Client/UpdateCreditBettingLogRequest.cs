using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyStake.Constants.Enums;
using Titan.Repository;
using Titan.Repository.Database;

namespace MyStake.Services.Messaging.Common.Client
{
    public class UpdateCreditBettingLogRequest
    {
		public long HistoryId { get; set; }
		public string VendorCode { get; set; } = string.Empty; // ProviderCode, kept as extra safety filter
		public decimal Amount { get; set; }
		public decimal AfterBalance { get; set; }
		public string Detail { get; set; } = string.Empty;
		public User User { get; set; }
		public decimal BetAmount { get; set; }
		public decimal WinAmount { get; set; }
	}
}
