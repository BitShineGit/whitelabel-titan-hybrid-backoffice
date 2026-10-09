using MyStake.Constants.Enums;
using MyStake.Services.Interfaces;
using MyStake.Services.Messaging.Common.Client;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using MyStake.Services.Messaging.IntegratedApi;
using Titan.Repository;
using Titan.Repository.Database;

namespace MyStake.Services.Implementations
{
	public class BettingLogService : IBettingLogService
	{
		private readonly IWhitelabelDBStorage _dbStorage;
		private readonly Global.Logging.ILogger _logger;
		private readonly IClientService _clientService;
		public BettingLogService(IWhitelabelDBStorage dbStorage, Global.Logging.ILogger logger, IClientService clientService)
		{
			_dbStorage = dbStorage;
			_logger = logger;
			_clientService = clientService;
		}

		public async Task<BettingLog> GetByHistoryId(long historyId)
		{
			return await _dbStorage.Context.BettingLogs
				.Where(x => x.HistoryId == historyId)
				.OrderByDescending(x => x.Id)
				.FirstOrDefaultAsync();
		}

		public async Task<AddBettingLogResponse> AddDebitBettingLog(AddBettingLogRequest request)
		{
			var response = new AddBettingLogResponse();

			try
			{
				var updateResponse = _clientService.UpdateCustomerBalance(new UpdateCustomerBalanceRequest
				{
					Customer = request.User,
					ChangeAmount = request.AfterBalance - request.BeforeBalance,
					BetAmount = request.BetAmount,
					WinAmount  = request.WinAmount
				});

				if (!updateResponse.Success)
				{
					response.ErrorCode = ErrorCode.UNKNOWN_SERVER_ERROR;
					response.message = "Unkown server error.";
					return response;
				}

				await _dbStorage.Context.BettingLogs.AddAsync(new BettingLog
				{
					UserTableId = request.CustomerId,
					UserCode = request.UserCode,
					AgentTableId = request.AgentTableId,
					AgentCode = request.AgentCode,
					AgentPath = request.AgentPath,
					ProviderId = request.ProviderId,
					ProviderName = request.ProviderName,
					ProviderCode = request.VendorCode,
					GameName = request.GameName,
					GameCode = request.GameCode,
					RoundId = request.RoundId,
					HistoryId = request.HistoryId,
					BetAmount = request.BetAmount * (-1),
					WinAmount = request.WinAmount,
					BeforeBalance = request.BeforeBalance,
					AfterBalance = request.AfterBalance,
					Status = (byte)request.Status,
					CreatedAt = DateTime.UtcNow,
					UpdatedAt = DateTime.UtcNow
				});
				await _dbStorage.Context.SaveChangesAsync();
			}
			catch (Exception e)
			{
				_logger.Error($"AddDebitBettingLog: {e}");
				response.ErrorCode = ErrorCode.UNKNOWN_SERVER_ERROR;
				response.message = "Unkown server error.";
			}
			return response;
		}

		public async Task<UpdateCreditBettingLogResponse> UpdateCreditBettingLog(UpdateCreditBettingLogRequest request)
		{
			var response = new UpdateCreditBettingLogResponse();
			try
			{
				var updateResponse = _clientService.UpdateCustomerBalance(new UpdateCustomerBalanceRequest
				{
					Customer = request.User,
					ChangeAmount = request.Amount,
                    BetAmount = request.BetAmount,
					WinAmount = request.WinAmount
				});

				if (!updateResponse.Success)
				{
					response.ErrorCode = ErrorCode.UNKNOWN_SERVER_ERROR;
					response.message = "Unkown server error.";
					return response;
				}

				var bettingLog = await _dbStorage.Context.BettingLogs
					.Where(x => x.HistoryId == request.HistoryId && x.ProviderCode == request.VendorCode)
					.OrderByDescending(x => x.Id)
					.FirstOrDefaultAsync();

				if (bettingLog == null)
				{
					response.ErrorCode = ErrorCode.NO_BETTING_LOG_EXIST;
					response.message = "Betting log not found.";
					return response;
				}

				bettingLog.AfterBalance = request.AfterBalance;
				bettingLog.Status = (byte)BettingLogType.Credit;
				bettingLog.WinAmount = request.Amount;
				bettingLog.UpdatedAt = DateTime.UtcNow;

				_dbStorage.Context.Update(bettingLog);
				await _dbStorage.Context.SaveChangesAsync();
			}
			catch (Exception e)
			{
				_logger.Error($"UpdateCreditBettingLog: {e}");
				response.ErrorCode = ErrorCode.UNKNOWN_SERVER_ERROR;
				response.message = "Unkown server error.";
			}
			return response;
		}

		public async Task<AddBettingLogResponse> AddCreditBettingLog(AddBettingLogRequest request)
		{
			var response = new AddBettingLogResponse();

			try
			{
				var updateResponse = _clientService.UpdateCustomerBalance(new UpdateCustomerBalanceRequest
				{
					Customer = request.User,
					ChangeAmount = request.AfterBalance - request.BeforeBalance,
                    BetAmount = request.BetAmount,
                    WinAmount = request.WinAmount

                });

				if (!updateResponse.Success)
				{
					response.ErrorCode = ErrorCode.UNKNOWN_SERVER_ERROR;
					response.message = "Unkown server error.";
					return response;
				}

				await _dbStorage.Context.BettingLogs.AddAsync(new BettingLog
				{
					UserTableId = request.CustomerId,
					UserCode = request.UserCode,
					AgentTableId = request.AgentTableId,
					AgentCode = request.AgentCode,
					AgentPath = request.AgentPath,
					ProviderId = request.ProviderId,
					ProviderName = request.ProviderName,
					ProviderCode = request.VendorCode,
					GameName = request.GameName,
					GameCode = request.GameCode,
					RoundId = request.RoundId,
					HistoryId = request.HistoryId,
					BetAmount = request.BetAmount * (-1),
					WinAmount = 0,
					BeforeBalance = request.BeforeBalance,
					AfterBalance = request.AfterBalance,
					Status = (byte)BettingLogType.Credit,
					CreatedAt = DateTime.UtcNow,
					UpdatedAt = DateTime.UtcNow
				});
				await _dbStorage.Context.SaveChangesAsync();
			}
			catch (Exception e)
			{
				_logger.Error($"AddCreditBettingLog: {e}");
				response.ErrorCode = ErrorCode.UNKNOWN_SERVER_ERROR;
				response.message = "Unkown server error.";
			}
			return response;
		}

		public async Task<UpdateCancelBettingLogResponse> UpdateCancelBettingLog(UpdateCancelBettingLogRequest request)
		{
			var response = new UpdateCancelBettingLogResponse();
			try
			{
				var updateResponse = _clientService.UpdateCustomerBalance(new UpdateCustomerBalanceRequest
				{
					Customer = request.User,
					ChangeAmount = request.Amount,
                    BetAmount = request.BetAmount,
                    WinAmount = request.WinAmount
                });

				if (!updateResponse.Success)
				{
					response.ErrorCode = ErrorCode.UNKNOWN_SERVER_ERROR;
					response.message = "Unkown server error.";
					return response;
				}

				var bettingLog = await _dbStorage.Context.BettingLogs
					.Where(x => x.HistoryId == request.HistoryId && x.ProviderCode == request.VendorCode)
					.OrderByDescending(x => x.Id)
					.FirstOrDefaultAsync();

				if (bettingLog == null)
				{
					response.ErrorCode = ErrorCode.NO_BETTING_LOG_EXIST;
					response.message = "Betting log not found.";
					return response;
				}

				bettingLog.AfterBalance = request.AfterBalance;
				bettingLog.Status = (byte)BettingLogType.Cancel;
				bettingLog.UpdatedAt = DateTime.UtcNow;

				_dbStorage.Context.Update(bettingLog);
				await _dbStorage.Context.SaveChangesAsync();
			}
			catch (Exception e)
			{
				_logger.Error($"UpdateCancelBettingLog: {e}");
				response.ErrorCode = ErrorCode.UNKNOWN_SERVER_ERROR;
				response.message = "Unkown server error.";
			}
			return response;
		}
	}
}
