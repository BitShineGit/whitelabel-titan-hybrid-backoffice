using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Titan.Repository;
using MyStake.Models;
using MyStake.Services;
using System.Net.Http;
using System.Net.Http.Json;
using Titan.Repository.Database;

namespace MyStake.Controllers
{
    public class WithdrawController : Controller
    {
        private readonly IWhitelabelDBStorage _dbStorage;
        private readonly Global.Logging.ILogger _logger;
        private readonly HttpClient _httpClient;
        private readonly AppSettings _appSettings;
        private readonly ISettingService _settingService;


        public WithdrawController( 
            IWhitelabelDBStorage dbStorage,
            Global.Logging.ILogger logger,
            HttpClient httpClient,
            IOptions<AppSettings> appSettings,
            ISettingService settingService)
        {
            _dbStorage = dbStorage;
            _logger = logger;
            _httpClient = httpClient;
            _appSettings = appSettings.Value;
            _settingService = settingService;


        }
        [HttpPost]
        public async Task<IActionResult> CreateWithdrawRequest(
            [FromBody] CreateWithdrawRequest request)
        {
            try
            {
                // ============================================================
                // 1. Validate Request
                // ============================================================

                if (request == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid withdraw request."
                    });
                }

                // ============================================================
                // 2. Validate Withdraw Amount
                // ============================================================

                if (request.Amount < 5 || request.Amount > 5000)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid withdraw amount."
                    });
                }

                // ============================================================
                // 3. Get Current User
                // ============================================================

                var user = await _dbStorage.Context.Users
                    .FirstOrDefaultAsync(
                        u => u.UserCode == User.Identity.Name);

                if (user == null)
                {
                    _logger.Error(
                        $"CreateWithdrawRequest: Current user not found. " +
                        $"Identity: {User.Identity?.Name}");

                    return Json(new
                    {
                        success = false,
                        message = "User not found."
                    });
                }

                // ============================================================
                // 4. Check KYC Status
                //
                // KycStatus:
                // 0 = Pending
                // 1 = Verified
                // 2 = Rejected
                // 3 = Not Submitted
                //
                // Only Verified users can withdraw.
                // ============================================================

                if (user.KycStatus != 1)
                {
                    return Json(new
                    {
                        success = false,
                        kycRequired = true,
                        message = "Please complete KYC verification before making a withdrawal."
                    });
                }

                // ============================================================
                // 5. Check Current Balance
                // ============================================================

                if (request.Amount > user.CurrentBalance)
                {
                    return Json(new
                    {
                        success = false,
                        message = "You don't have enough balance."
                    });
                }

                // ============================================================
                // 6. Check Welcome Bonus Wagering Requirement
                // ============================================================

                if (user.WelcomeBonusAmount > 0)
                {
                    int requiredBettingAmount =
                        await _settingService.GetIntValueAsync(
                            "Bonus",
                            "RequiredBettingAmount");

                    decimal requiredTotalBetAmount =
                        user.WelcomeBonusAmount *
                        requiredBettingAmount;

                    if (user.TotalBetAmount < requiredTotalBetAmount)
                    {
                        return Json(new
                        {
                            success = false,
                            message =
                                $"You need to complete the wagering requirement " +
                                $"before making a withdrawal. " +
                                $"Required betting amount: {requiredTotalBetAmount:0.##}, " +
                                $"Current betting amount: {user.TotalBetAmount:0.##}."
                        });
                    }
                }

                // ============================================================
                // 7. Check Withdraw Limit Time
                // ============================================================

                int withdrawLimitTime =
                    await _settingService.GetIntValueAsync(
                        "Withdraw",
                        "LimitTime");

                if (withdrawLimitTime > 0 &&
                    user.LastWithdrawAt != null)
                {
                    var nextAvailableTime =
                        user.LastWithdrawAt.Value.AddHours(withdrawLimitTime);

                    if (DateTime.UtcNow < nextAvailableTime)
                    {
                        var remainingTime =
                            nextAvailableTime - DateTime.UtcNow;

                        return Json(new
                        {
                            success = false,
                            message =
                                $"You can make another withdrawal after " +
                                $"{remainingTime.Hours} hour(s) and " +
                                $"{remainingTime.Minutes} minute(s)."
                        });
                    }
                }

                // ============================================================
                // 8. Find Upline Agent
                // ============================================================

                var uplineAgent = await _dbStorage.Context.Agents
                    .FirstOrDefaultAsync(
                        a => a.AgentCode == user.AgentCode);

                if (uplineAgent == null)
                {
                    _logger.Error(
                        $"CreateWithdrawRequest: Current user's agent not found. " +
                        $"Identity: {User.Identity?.Name}");

                    return Json(new
                    {
                        success = false,
                        message = "Upline agent not found."
                    });
                }

                // ============================================================
                // 9. Create Withdrawal Finance Log
                // ============================================================

                var financeLog = new FinanceLog
                {
                    OrderId = "WD-" + Guid.NewGuid().ToString(),

                    AgentCode = uplineAgent.AgentCode,
                    AgentPath = uplineAgent.AgentPath,
                    AgentTableId = uplineAgent.Id,

                    TargetCode = user.UserCode,
                    TargetPath = user.AgentPath,
                    TargetTableId = user.Id,

                    TargetAccountType = 0,
                    FinanceType = 2,

                    Amount = request.Amount,

                    CreatedAt = DateTime.UtcNow,

                    AgentBeforeBalance = uplineAgent.Balance,
                    AgentAfterBalance = uplineAgent.Balance,

                    TargetBeforeBalance = user.CurrentBalance,
                    TargetAfterBalance = user.CurrentBalance,

                    Status = 0
                };

                await _dbStorage.Context.FinanceLogs.AddAsync(financeLog);

                user.LastWithdrawAt = DateTime.UtcNow;

                await _dbStorage.Context.SaveChangesAsync();

                // ============================================================
                // 10. Notify BackOffice
                // ============================================================

                var notification = new
                {
                    orderId = financeLog.OrderId,
                    userCode = user.UserCode,
                    amount = financeLog.Amount
                };

                try
                {
                    await _httpClient.PostAsJsonAsync(
                        $"{_appSettings.BackOfficeDomain.TrimEnd('/')}/api/notification/withdraw",
                        notification);
                }
                catch (Exception ex)
                {
                    _logger.Error(
                        $"CreateWithdrawRequest: " +
                        $"Failed to send back-office notification. {ex}");
                }

                // ============================================================
                // 11. Success
                // ============================================================

                return Json(new
                {
                    success = true,
                    orderId = financeLog.OrderId
                });
            }
            catch (Exception ex)
            {
                _logger.Error(
                    $"CreateWithdrawRequest: {ex}");

                return Json(new
                {
                    success = false,
                    message =
                        "An error occurred while submitting the withdraw request."
                });
            }
        }
    }
}