using Titan.Repository;
using MyStake.Services.Messaging.Common.Client;
using Titan.Repository.Database;

namespace MyStake.Services.Interfaces
{
	public interface IBettingLogService
	{
		Task<BettingLog> GetByHistoryId(long historyId);

		Task<AddBettingLogResponse> AddDebitBettingLog(AddBettingLogRequest request);

		Task<UpdateCreditBettingLogResponse> UpdateCreditBettingLog(UpdateCreditBettingLogRequest request);

		Task<AddBettingLogResponse> AddCreditBettingLog(AddBettingLogRequest request);

		Task<UpdateCancelBettingLogResponse> UpdateCancelBettingLog(UpdateCancelBettingLogRequest request);
	}
}
