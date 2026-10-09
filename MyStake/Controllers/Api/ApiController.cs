using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyStake.Constants.Enums;
using MyStake.Services.Messaging.IntegratedApi;
using MyStake.Services.Interfaces;
using MyStake.Services.Messaging.Common.Client;
using Newtonsoft.Json;
using Titan.Repository;
using Titan.Repository.Database;

namespace MyStake.Controllers.Api
{
    [Route("[controller]")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly WebSocketHandler _handler;
        private readonly IWhitelabelDBStorage _dbStorage;
        private readonly IClientService _clientService;
        private readonly IBettingLogService _bettingLogService;
        public Global.Logging.ILogger _logger;

        public ApiController(
            WebSocketHandler handler,
            IWhitelabelDBStorage dbStorage,
            IClientService clientService,
            IBettingLogService bettingLogService,
            Global.Logging.ILogger logger
            )
        {
            _handler = handler;
            _dbStorage = dbStorage;
            _clientService = clientService;
            _logger = logger;
            _bettingLogService = bettingLogService;
        }

        [HttpPost("Balance")]
        public async Task<IActionResult> Balance(GetBalanceRequest request)
        {
            if (string.IsNullOrEmpty(request.UserCode))
            {
                _logger.Error($"[ApiController->Balance] [empty user code]");

                return Ok(new
                {
                    success = false,
                    errorCode = ErrorCode.BAD_REQUEST,
                    message = "Invalid user code."
                });
            }

            var user = await _dbStorage.Context.Users.AsNoTracking().Where(s => s.UserCode == request.UserCode).FirstOrDefaultAsync();
            if (user == null)
            {
                _logger.Error($"[ApiController->Balance] [user not found] userCde: {request.UserCode}");

                return Ok(new
                {
                    success = false,
                    errorCode = ErrorCode.USER_DOES_NOT_EXIST,
                    message = "User not found."
                });
            }

            return Ok(new
            {
                success = true,
                errorCode = ErrorCode.NO_ERROR,
                message = user.CurrentBalance
            });
        }

        [HttpPost("transaction")]
        public async Task<IActionResult> Transaction(GameTransactionRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.UserCode))
                {
                    _logger.Error($"[ApiController->Transaction] [empty user code] request:{JsonConvert.SerializeObject(request)}");
                    return Ok(new
                    {
                        success = false,
                        errorCode = ErrorCode.BAD_REQUEST,
                        message = "Invalid user code."
                    });
                }

                // check user
                var user = await _clientService.GetCustomerFromUserCode(request.UserCode);
                if (user == null)
                {
                    _logger.Error($"[ApiController->Transaction] User not found request:{JsonConvert.SerializeObject(request)}");
                    return Ok(new
                    {
                        success = false,
                        errorCode = ErrorCode.USER_DOES_NOT_EXIST,
                        message = "User not found."
                    });
                }
                // check game
                //var game = await _dbStorage.Context.Games.AsNoTracking().Where(s => s.GameCode == request.GameCode).FirstOrDefaultAsync();
                //if (game == null)
                //{
                //    _logger.Error($"[ApiController->Transaction] Game not found request:{JsonConvert.SerializeObject(request)}");
                //    return Ok(new
                //    {
                //        success = false,
                //        errorCode = ErrorCode.BAD_REQUEST,
                //        message = "Game not found."
                //    });
                //}
                // check provider
                var provider = await _dbStorage.Context.Providers.AsNoTracking().Where(s => s.ProviderCode == request.VendorCode).FirstOrDefaultAsync();
                if (provider == null)
                {
                    _logger.Error($"[ApiController->Transaction] Provider not found request:{JsonConvert.SerializeObject(request)}");
                    return Ok(new
                    {
                        success = false,
                        errorCode = ErrorCode.BAD_REQUEST,
                        message = "Provider not found."
                    });
                }

                var log = await _dbStorage.Context.BettingLogs.Where(s => s.HistoryId == request.HistoryId).FirstOrDefaultAsync();
                if (log == null)
                {
                    if ((request.IsFinished && !request.IsCanceled && request.Amount > 0) ||  // credit transaction
                        (request.IsFinished && request.IsCanceled)) // cancel transaction
                    {
                        _logger.Error($"[ApiController->Transaction] Transaction not found. request:{JsonConvert.SerializeObject(request)}");
                        return Ok(new
                        {
                            success = false,
                            errorCode = ErrorCode.BALANCE_LOG_DOES_NOT_EXIST,
                            message = "Transaction not found."
                        });
                    }
                }
                else
                {
                    if ((!request.IsFinished && !request.IsCanceled && request.Amount < 0) ||  // debit transaction
                        (request.IsFinished && !request.IsCanceled && request.Amount < 0)) // debit-credit transaction
                    {
                        _logger.Error($"[ApiController->Transaction] Duplicate transaction. request:{JsonConvert.SerializeObject(request)}");
                        return Ok(new
                        {
                            success = false,
                            errorCode = ErrorCode.DUPLICATE_TRANSACTION,
                            message = "Duplicate transaction."
                        });
                    }
                }
                var existingLog = await _bettingLogService.GetByHistoryId(request.HistoryId);
                var player = await _dbStorage.Context.Users.Where(s => s.UserCode == request.UserCode).FirstOrDefaultAsync();
                var currency = await _dbStorage.Context.Currencies.Where(s => s.CurrencyCode == player.UserCode).FirstOrDefaultAsync();

                // ----- Cancel: works whether the log is currently Debit(0) or Credit(1) -----
                if (request.IsCanceled)
                {
                    if (existingLog == null)
                    {
                        _logger.Error($"[ApiController->Transaction] Cancel requested but no log exists. request:{JsonConvert.SerializeObject(request)}");
                        return Ok(new
                        {
                            success = false,
                            errorCode = ErrorCode.BALANCE_LOG_DOES_NOT_EXIST,
                            message = "Transaction not found."
                        });
                    }

                    if (existingLog.Status == (byte)BettingLogType.Cancel)
                    {
                        _logger.Error($"[ApiController->Transaction] Already cancelled. request:{JsonConvert.SerializeObject(request)}");
                        return Ok(new
                        {
                            success = false,
                            errorCode = ErrorCode.DUPLICATE_TRANSACTION,
                            message = "Duplicate transaction."
                        });
                    }

                    decimal refundAmount = existingLog.BetAmount;
                    decimal cancelAfterBalance = user.CurrentBalance + refundAmount;

                    var cancelResult = await _bettingLogService.UpdateCancelBettingLog(new UpdateCancelBettingLogRequest
                    {
                        HistoryId = request.HistoryId,
                        VendorCode = request.VendorCode,
                        Amount = refundAmount,
                        AfterBalance = cancelAfterBalance,
                        Detail = request.Detail,
                        User = user
                    });

                    if (!cancelResult.Success)
                    {
                        return Ok(new
                        {
                            success = false,
                            message = cancelResult.message,
                            errorCode = cancelResult.ErrorCode
                        });
                    }

                    try
                    {
                        await _handler.SendToUserAsync(request.UserCode, new
                        {
                            type = "balance",
                            message = $"{cancelAfterBalance:#,##0.00}"
                        });
                    }
                    catch (Exception notifyEx)
                    {
                        // Log but don't fail the transaction — the balance/log write already committed.
                        _logger.Error($"[GgxApiController->Transaction] Notification failed after successful transaction. historyId:{request.HistoryId}, Ex:{notifyEx}");
                    }







                    return Ok(new
                    {
                        success = true,
                        message = cancelAfterBalance,
                        errorCode = ErrorCode.NO_ERROR
                    });
                }

                // ---- Duplicate / already-finalized guard ----
                bool isNewBet = !request.IsFinished && !request.IsCanceled;
                if (isNewBet)
                {
                    // Type 1: a fresh debit should never reuse a HistoryId already logged
                    if (existingLog != null)
                    {
                        _logger.Error($"[ApiController->Transaction] Duplicate HistoryId on new bet. request:{JsonConvert.SerializeObject(request)}");
                        return Ok(new
                        {
                            success = false,
                            errorCode = ErrorCode.DUPLICATE_TRANSACTION,
                            message = "Duplicate transaction."
                        });
                    }
                }
                else
                {
                    // Types 2/3/4: reject if this round is already finalized (Credit or Cancelled)
                    if (existingLog != null &&
                        (existingLog.Status == (byte)BettingLogType.Credit ||
                         existingLog.Status == (byte)BettingLogType.Cancel))
                    {
                        _logger.Error($"[ApiController->Transaction] Round already finalized. request:{JsonConvert.SerializeObject(request)}");
                        return Ok(new
                        {
                            success = false,
                            errorCode = ErrorCode.INVALID_TRANSACTION,
                            message = "Invalid transaction."
                        });
                    }
                }

                decimal beforeBalance = user.CurrentBalance;
                decimal afterBalance;

                // ----- Type 2: existing bet resolves to a win -----
                if (existingLog != null && request.IsFinished && !request.IsCanceled)
                {
                    if (user.CurrentBalance + request.Amount < 0)
                    {
                        _logger.Error($"[ApiController->Transaction] Insufficient user balance. request:{JsonConvert.SerializeObject(request)}");
                        return Ok(new
                        {
                            success = false,
                            errorCode = ErrorCode.INSUFFICIENT_USER_BALANCE,
                            message = "Insufficient user ballance."
                        });
                    }

                    afterBalance = user.CurrentBalance + request.Amount;

                    var creditResult = await _bettingLogService.UpdateCreditBettingLog(new UpdateCreditBettingLogRequest
                    {
                        HistoryId = request.HistoryId,
                        VendorCode = request.VendorCode,
                        Amount = request.Amount,
                        AfterBalance = afterBalance,
                        Detail = request.Detail,
                        User = user
                    });

                    if (!creditResult.Success)
                    {
                        return Ok(new
                        {
                            success = false,
                            message = creditResult.message,
                            errorCode = creditResult.ErrorCode
                        });
                    }
                }
                // ----- Type 1: brand-new bet (debit) -----
                else if (existingLog == null && !request.IsFinished && !request.IsCanceled)
                {
                    if (user.CurrentBalance + request.Amount < 0)
                    {
                        _logger.Error($"[ApiController->Transaction] Insufficient user balance. request:{JsonConvert.SerializeObject(request)}");
                        return Ok(new
                        {
                            success = false,
                            errorCode = ErrorCode.INSUFFICIENT_USER_BALANCE,
                            message = "Insufficient user ballance."
                        });
                    }

                    afterBalance = user.CurrentBalance + request.Amount;

                    var debitResult = await _bettingLogService.AddDebitBettingLog(new AddBettingLogRequest
                    {
                        CustomerId = user.Id,
                        UserCode = request.UserCode,
                        AgentTableId = user.AgentTableId,
                        AgentCode = user.AgentCode,
                        AgentPath = user.AgentPath,
                        ProviderId = provider.Id,
                        ProviderName = provider.ProviderName,
                        VendorCode = request.VendorCode,
                        GameName = "",
                        GameCode = request.GameCode,
                        RoundId = request.RoundId,
                        HistoryId = request.HistoryId,
                        BetAmount = request.Amount,
                        WinAmount = 0,
                        BeforeBalance = beforeBalance,
                        AfterBalance = afterBalance,
                        Detail = request.Detail,
                        Status = BettingLogType.Debit,
                        User = user,
                    });

                    if (!debitResult.Success)
                    {
                        return Ok(new
                        {
                            success = false,
                            message = debitResult.message,
                            errorCode = debitResult.ErrorCode
                        });
                    }
                }
                // ----- Type 3: single-shot win with no prior debit row -----
                else if (existingLog == null && request.IsFinished && !request.IsCanceled)
                {
                    if (user.CurrentBalance + request.Amount < 0)
                    {
                        _logger.Error($"[ApiController->Transaction] Insufficient user balance. request: {JsonConvert.SerializeObject(request)}");
                        return Ok(new
                        {
                            success = false,
                            errorCode = ErrorCode.INSUFFICIENT_USER_BALANCE,
                            message = "Insufficient user ballance."
                        });
                    }

                    afterBalance = user.CurrentBalance + request.Amount;

                    var creditResult = await _bettingLogService.AddCreditBettingLog(new AddBettingLogRequest
                    {
                        CustomerId = user.Id,
                        UserCode = request.UserCode,
                        AgentTableId = user.AgentTableId,
                        AgentCode = user.AgentCode,
                        AgentPath = user.AgentPath,
                        ProviderId = provider.Id,
                        ProviderName = provider.ProviderName,
                        VendorCode = request.VendorCode,
                        GameName = "",
                        GameCode = request.GameCode,
                        RoundId = request.RoundId,
                        HistoryId = request.HistoryId,
                        BetAmount = request.Amount,
                        BeforeBalance = beforeBalance,
                        AfterBalance = afterBalance,
                        Detail = request.Detail,
                        User = user,
                    });

                    if (!creditResult.Success)
                    {
                        return Ok(new
                        {
                            success = false,
                            errorCode = creditResult.ErrorCode,
                            message = creditResult.message
                        });
                    }
                }
                else
                {
                    _logger.Error($"[ApiController->Transaction] Invalid state combo req:{JsonConvert.SerializeObject(request)}");
                    return Ok(new
                    {
                        success = false,
                        errorCode = ErrorCode.UNKNOWN_SERVER_ERROR,
                        message = "Unknown server error."
                    });
                }

                var currencyId = user.CurrencyId;
                var currencysymbol = await _dbStorage.Context.Currencies.FirstOrDefaultAsync(c => c.Id == currencyId);
                var currencyCode = currency?.CurrencyCode;
                var currencySymbol = currency?.CurrencySymbol;



                try
                {
                    await _handler.SendToUserAsync(request.UserCode, new
                    {
                        type = "balance",
                        message = $"{currencySymbol} {afterBalance:#,##0.00}"
                    });
                }
                catch (Exception notifyEx)
                {
                    // Log but don't fail the transaction — the balance/log write already committed.
                    _logger.Error($"[GgxApiController->Transaction] Notification failed after successful transaction. historyId:{request.HistoryId}, Ex:{notifyEx}");
                }







                return Ok(new
                {
                    success = true,
                    message = afterBalance,
                    errorCode = ErrorCode.NO_ERROR
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"[Transaction3] Req: {request}");
                _logger.Error($"[Transaction3] Ex: {ex}");
            }

            _logger.Error($"[ApiController->Transaction] Invalid transaction type. request:{JsonConvert.SerializeObject(request)}");
            return Ok(new
            {
                success = false,
                errorCode = ErrorCode.UNKNOWN_SERVER_ERROR,
                message = "Unknown server error."
            });
        }

        [HttpPost("batch-transactions")]
        public async Task<IActionResult> BatchTransaction(BatchGameTransactionRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.UserCode) ||
                    request.Transactions == null || request.Transactions.Count == 0)
                {
                    throw new ArgumentNullException(nameof(request));
                }

                var user = await _clientService.GetCustomerFromUserCode(request.UserCode);
                if (user == null)
                {
                    _logger.Error($"[ApiController->BatchTransaction] User not found. req:{request}");
                    return Ok(new
                    {
                        success = false,
                        errorCode = ErrorCode.USER_DOES_NOT_EXIST,
                        message = "User not found."
                    });
                }

                // ---------- PASS 1: validate everything, no writes ----------
                var batchStatus = new Dictionary<long, byte>();
                var batchBetAmount = new Dictionary<long, decimal>();
                var plannedLogs = new List<BettingLog>();
                decimal runningBalance = user.CurrentBalance;

                foreach (var item in request.Transactions)
                {
                    //var game = await _dbStorage.Context.Games.FirstOrDefaultAsync(g => g.GameCode == item.GameCode);
                    //if (game == null)
                    //    return Ok(new
                    //    {
                    //        success = false,
                    //        errorCode = ErrorCode.BAD_REQUEST,
                    //        message = "Game not found."
                    //    });

                    var provider = await _dbStorage.Context.Providers.FirstOrDefaultAsync(p => p.ProviderCode == item.VendorCode);
                    if (provider == null)
                    {
                        _logger.Error($"[ApiController->BatchTransaction] Provider not found. req:{item}");
                        return Ok(new
                        {
                            success = false,
                            errorCode = ErrorCode.BAD_REQUEST,
                            message = "Provider not found."
                        });
                    }

                    var existingLog = await _bettingLogService.GetByHistoryId(item.HistoryId);
                    byte? currentStatus = batchStatus.TryGetValue(item.HistoryId, out var st) ? st : existingLog?.Status;
                    bool hasLog = currentStatus.HasValue;
                    bool isNewBet = !item.IsFinished && !item.IsCanceled;

                    if (isNewBet && hasLog)
                    {
                        _logger.Error($"[ApiController->BatchTransaction] Duplicate transaction. req:{item}");
                        return Ok(new
                        {
                            success = false,
                            errorCode = ErrorCode.DUPLICATE_TRANSACTION,
                            message = "Duplicate transaction."
                        });
                    }

                    if (!isNewBet && hasLog && (currentStatus == (byte)BettingLogType.Credit || currentStatus == (byte)BettingLogType.Cancel))
                    {
                        _logger.Error($"[ApiController->BatchTransaction] Invalid transaction. req:{item}");
                        return Ok(new
                        {
                            success = false,
                            errorCode = ErrorCode.INVALID_TRANSACTION,
                            message = "Invalid transaction. "
                        });
                    }

                    decimal beforeBalance = runningBalance;
                    decimal changeAmount;
                    byte newStatus;

                    if (!hasLog && isNewBet)
                    {
                        changeAmount = item.Amount;
                        newStatus = (byte)BettingLogType.Debit;
                        batchBetAmount[item.HistoryId] = item.Amount;
                    }
                    else if (hasLog && item.IsFinished && !item.IsCanceled)
                    {
                        changeAmount = item.Amount;
                        newStatus = (byte)BettingLogType.Credit;
                    }
                    else if (!hasLog && item.IsFinished && !item.IsCanceled)
                    {
                        changeAmount = item.Amount;
                        newStatus = (byte)BettingLogType.Credit;
                    }
                    else if (hasLog && item.IsFinished && item.IsCanceled)
                    {
                        decimal betAmount = batchBetAmount.TryGetValue(item.HistoryId, out var amt) ? amt : existingLog!.BetAmount;
                        changeAmount = Math.Abs(betAmount);
                        newStatus = (byte)BettingLogType.Cancel;
                    }
                    else
                    {
                        _logger.Error($"[ApiController->BatchTransaction] Invalid transaction. req:{item}");
                        return Ok(new
                        {
                            success = false,
                            errorCode = ErrorCode.INVALID_TRANSACTION,
                            message = "Invalid transaction."
                        });
                    }

                    if (runningBalance + changeAmount < 0)
                    {
                        _logger.Error($"[ApiController->BatchTransaction] Insufficient user balance. req:{item}");
                        return Ok(new
                        {
                            success = false,
                            errorCode = ErrorCode.INSUFFICIENT_USER_BALANCE,
                            message = "Insufficient user balance."
                        });
                    }

                    decimal afterBalance = runningBalance + changeAmount;
                    runningBalance = afterBalance;
                    batchStatus[item.HistoryId] = newStatus;

                    plannedLogs.Add(new BettingLog
                    {
                        UserTableId = user.Id,
                        UserCode = item.UserCode,
                        AgentTableId = user.AgentTableId,
                        AgentCode = user.AgentCode,
                        AgentPath = user.AgentPath,
                        ProviderId = provider.Id,
                        ProviderName = provider.ProviderName,
                        ProviderCode = item.VendorCode,
                        GameName = "",
                        GameCode = item.GameCode,
                        RoundId = item.RoundId,
                        HistoryId = item.HistoryId,
                        BetAmount = newStatus == (byte)BettingLogType.Debit ? item.Amount * (-1) : 0,
                        WinAmount = newStatus == (byte)BettingLogType.Credit ? item.Amount : 0,
                        BeforeBalance = beforeBalance,
                        AfterBalance = afterBalance,
                        Status = newStatus,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                // ---------- PASS 2: apply in memory, save once ----------
                user.CurrentBalance = runningBalance;
                _dbStorage.Context.Users.Update(user);
                await _dbStorage.Context.BettingLogs.AddRangeAsync(plannedLogs);
                await _dbStorage.Context.SaveChangesAsync(); // single atomic commit

                var currencyId = user.CurrencyId;
                var currency = await _dbStorage.Context.Currencies.FirstOrDefaultAsync(c => c.Id == currencyId);
                var currencyCode = currency?.CurrencyCode;
                var currencySymbol = currency?.CurrencySymbol;


                try
                {
                    await _handler.SendToUserAsync(request.UserCode, new
                    {
                        type = "balance",
                        message = $"{currencySymbol} {runningBalance:#,##0.00}"
                    });
                }
                catch (Exception notifyEx)
                {
                    // Log but don't fail the transaction — the balance/log write already committed.
                    _logger.Error($"[GgxApiController->Transaction] Notification failed after successful transaction. historyId: Ex:{notifyEx}");
                }

                return Ok(new
                {
                    success = true,
                    message = runningBalance,
                    errorCode = ErrorCode.NO_ERROR
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"[BatchTransaction] req: {ex}");
            }

            return Ok(new
            {
                success = false,
                errorCode = ErrorCode.UNKNOWN_SERVER_ERROR,
                message = "Unknown server error."
            });
        }
    }

}