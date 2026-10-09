using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Titan.Repository;
using Microsoft.Extensions.Options;
using MyStake.Models;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http;
using MyStake.Services;

namespace BlockPayGateway.Api.Controllers
{
    [Authorize]
    public class PayController : Controller
    {
        private readonly IWhitelabelDBStorage _dbStorage;
        private readonly IConfiguration _configuration;
        private readonly Global.Logging.ILogger _logger;
        private readonly HttpClient _httpClient;
        private readonly AppSettings _appSettings;
        private readonly ISettingService _settingService;

        public PayController(IWhitelabelDBStorage dbStorage, IConfiguration configuration, Global.Logging.ILogger logger, HttpClient httpClient, IOptions<AppSettings> appSettings, ISettingService settingService)
        {
            _dbStorage = dbStorage;
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
            _appSettings = appSettings.Value;
            _settingService = settingService;

        }

        private static string BuildCallbackPayload(string orderId, string status, decimal amount, string currency, long timestamp)
            => $"orderId={orderId}&status={status}&amount={amount.ToString("G29")}&currency={currency}&timestamp={timestamp}";

        private static bool IsTimestampFresh(long timestamp)
        {
            var age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - timestamp;
            return Math.Abs(age) <= 300; // 5-minute window
        }

        private static string ComputeHmac(string payload, string secret)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            var dataBytes = Encoding.UTF8.GetBytes(payload);
            var hash = HMACSHA256.HashData(keyBytes, dataBytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public static bool Verify(string orderId, string status, decimal amount, string currency, long timestamp, string sig, string merchantApiKey)
        {
            if (!IsTimestampFresh(timestamp)) return false;

            var payload = BuildCallbackPayload(orderId, status, amount, currency, timestamp);
            var expected = ComputeHmac(payload, merchantApiKey);

            // Constant-time comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(sig));
        }

        [HttpGet("pay/cancel")]
        public async Task<IActionResult> Cancel(string orderId, string status, decimal amount, string currency, long timestamp, string sig)
        {
            try
            {
                string merchantApiKey = _configuration.GetValue<string>("MerchantApiKey");
                if (string.IsNullOrWhiteSpace(merchantApiKey))
                {
                    _logger.Error("Cancel: MerchantApiKey is not configured.");
                    return StatusCode(StatusCodes.Status500InternalServerError, "Payment verification is temporarily unavailable.");
                }

                bool isTest = _configuration.GetValue<bool>("AppSettings:IsTest");
                if (!isTest)
                {
                    if (!Verify(orderId, status, amount, currency, timestamp, sig, merchantApiKey))
                    {
                        _logger.Error($"Success(orderId={orderId}): signature verification failed.");
                        return Unauthorized("Invalid signature");
                    }
                }

                var financeLog = await _dbStorage.Context.FinanceLogs.FirstOrDefaultAsync(x => x.OrderId == orderId);

                if (financeLog == null)
                {
                    _logger.Error($"Cancel(orderId={orderId}): FinanceLog not found.");
                    return RedirectToAction("Deposit", "User");
                }

                if (financeLog.Status != 0) // only cancel if still Pending
                {
                    _logger.Error($"Cancel(orderId={orderId}): FinanceLog status is not Pending (Status={financeLog.Status}); skipping update.");
                    return RedirectToAction("Deposit", "User");
                }

                var player = await _dbStorage.Context.Users
                    .FirstOrDefaultAsync(x => x.Id == financeLog.TargetTableId);

                if (player == null)
                {
                    _logger.Error($"Success(orderId={orderId}): Player not found. TargetTableId={financeLog.TargetTableId}");
                    return StatusCode(StatusCodes.Status500InternalServerError, "Player not found.");
                }

                var agent = await _dbStorage.Context.Agents
                    .FirstOrDefaultAsync(x => x.Id == financeLog.AgentTableId);

                if (agent == null)
                {
                    _logger.Error($"Success(orderId={orderId}): Agent not found. AgentTableId={financeLog.AgentTableId}");
                    return StatusCode(StatusCodes.Status500InternalServerError, "Agent not found.");
                }

                financeLog.TargetAfterBalance = player.CurrentBalance;
                financeLog.AgentAfterBalance = agent.Balance;

                financeLog.Status = 5; // Cancelled

                await _dbStorage.Context.SaveChangesAsync();

                // Notify BackOffice after cancellation was successfully saved
                var notification = new
                {
                    type = "deposit_cancelled",
                    orderId = financeLog.OrderId,
                    userCode = player.UserCode,
                    amount = financeLog.Amount
                };

                try
                {
                    await _httpClient.PostAsJsonAsync(
                        $"{_appSettings.BackOfficeDomain.TrimEnd('/')}/api/notification/deposit",
                        notification);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Cancel(orderId={orderId}): Failed to send back-office deposit notification. {ex}");
                }

                return RedirectToAction("Deposit", "User");
            }
            catch (Exception ex)
            {
                _logger.Error($"Cancel(orderId={orderId}): {ex}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while cancelling the order.");
            }
        }

        [HttpGet("pay/success")]
        public async Task<IActionResult> Success(
            string orderId,
            string status,
            decimal amount,
            string currency,
            long timestamp,
            string sig)
        {
            try
            {
                string merchantApiKey =
                    _configuration.GetValue<string>("MerchantApiKey");

                if (string.IsNullOrWhiteSpace(merchantApiKey))
                {
                    _logger.Error(
                        "Success: MerchantApiKey is not configured.");

                    return StatusCode(
                        StatusCodes.Status500InternalServerError,
                        "Payment verification is temporarily unavailable.");
                }

                bool isTest =
                    _configuration.GetValue<bool>("AppSettings:IsTest");

                if (!isTest)
                {
                    if (!Verify(
                        orderId,
                        status,
                        amount,
                        currency,
                        timestamp,
                        sig,
                        merchantApiKey))
                    {
                        _logger.Error(
                            $"Success(orderId={orderId}): signature verification failed.");

                        return Unauthorized("Invalid signature");
                    }
                }

                var financeLog =
                    await _dbStorage.Context.FinanceLogs
                        .FirstOrDefaultAsync(x => x.OrderId == orderId);

                if (financeLog == null)
                {
                    _logger.Error(
                        $"Success(orderId={orderId}): FinanceLog not found.");

                    return StatusCode(
                        StatusCodes.Status500InternalServerError,
                        "Order not found.");
                }

                if (financeLog.Status != 0)
                {
                    _logger.Error(
                        $"Success(orderId={orderId}): FinanceLog status is not Pending " +
                        $"(Status={financeLog.Status}); skipping balance update.");

                    return RedirectToAction("Deposit", "User");
                }

                var player =
                    await _dbStorage.Context.Users
                        .FirstOrDefaultAsync(x => x.Id == financeLog.TargetTableId);

                if (player == null)
                {
                    _logger.Error(
                        $"Success(orderId={orderId}): Player not found. " +
                        $"TargetTableId={financeLog.TargetTableId}");

                    return StatusCode(
                        StatusCodes.Status500InternalServerError,
                        "Player not found.");
                }

                var agent =
                    await _dbStorage.Context.Agents
                        .FirstOrDefaultAsync(x => x.Id == financeLog.AgentTableId);

                if (agent == null)
                {
                    _logger.Error(
                        $"Success(orderId={orderId}): Agent not found. " +
                        $"AgentTableId={financeLog.AgentTableId}");

                    return StatusCode(
                        StatusCodes.Status500InternalServerError,
                        "Agent not found.");
                }

                // ============================================================
                // Deposit
                // ============================================================

                decimal depositAmount = financeLog.Amount;

                // Player receives the actual deposit.
                // IMPORTANT: TotalBalance is the only balance field used.
                player.CurrentBalance += depositAmount;

                // Keep deposit statistics updated.
                player.TotalDepositAmount += depositAmount;
                player.LastDepositAt = DateTime.UtcNow;

                // Agent is charged only for the actual deposit amount.
                agent.Balance -= depositAmount;

                // ============================================================
                // Welcome Bonus
                // ============================================================

                decimal welcomeBonus = 0m;

                if (!player.IsFinishedFirstDeposit)
                {
                    double welcomeBonusRate =
                        await _settingService.GetDoubleValueAsync(
                            "Bonus",
                            "WelcomeBonusAmount");

                    double maxWelcomeBonusAmount =
                        await _settingService.GetDoubleValueAsync(
                            "Bonus",
                            "MaxWelcomeBonusAmount");

                    if (welcomeBonusRate > 0)
                    {
                        welcomeBonus =
                            depositAmount *
                            Convert.ToDecimal(welcomeBonusRate);

                        decimal maxBonus =
                            Convert.ToDecimal(maxWelcomeBonusAmount);

                        if (maxBonus > 0 &&
                            welcomeBonus > maxBonus)
                        {
                            welcomeBonus = maxBonus;
                        }

                        if (welcomeBonus > 0)
                        {
                            player.CurrentBalance += welcomeBonus;

                            player.WelcomeBonusAmount = welcomeBonus;
                            player.TotalReceivedBonusAmount += welcomeBonus;
                        }
                    }

                    player.IsFinishedFirstDeposit = true;
                    player.FirstDepositAt = DateTime.UtcNow;
                }

                // ============================================================
                // Finance Log
                // ============================================================

                financeLog.TargetAfterBalance =
                    player.CurrentBalance;

                financeLog.AgentAfterBalance =
                    agent.Balance;

                financeLog.Status = 2; // Completed

                await _dbStorage.Context.SaveChangesAsync();

                // ============================================================
                // Notify BackOffice
                // ============================================================

                var notification = new
                {
                    type = "deposit_success",
                    orderId = financeLog.OrderId,
                    userCode = player.UserCode,
                    amount = financeLog.Amount
                };

                try
                {
                    await _httpClient.PostAsJsonAsync(
                        $"{_appSettings.BackOfficeDomain.TrimEnd('/')}/api/notification/deposit",
                        notification);
                }
                catch (Exception ex)
                {
                    _logger.Error(
                        $"Success(orderId={orderId}): " +
                        $"Failed to send back-office deposit notification. {ex}");
                }

                return RedirectToAction("Deposit", "User");
            }
            catch (Exception ex)
            {
                _logger.Error(
                    $"Success(orderId={orderId}): {ex}");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "An error occurred while processing the successful payment.");
            }
        }
    }
}
