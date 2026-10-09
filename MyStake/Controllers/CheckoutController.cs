using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MyStake.Models;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Titan.Repository;
using Titan.Repository.Database;

namespace MarketSite.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly IWhitelabelDBStorage _dbStorage;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly Global.Logging.ILogger _logger;
        private readonly AppSettings _appSettings;


        public CheckoutController(IWhitelabelDBStorage dbStorage, HttpClient httpClient, IConfiguration configuration, Global.Logging.ILogger logger, IOptions<AppSettings> appSettings)
        {
            _dbStorage = dbStorage;
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
            _appSettings = appSettings.Value;
        }

        [HttpPost]
        public async Task<IActionResult> GenerateCheckoutUrl([FromBody] GenerateCheckoutUrlRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var orderId = "ORD-" + Guid.NewGuid().ToString();

                if (request.Amount < 5)
                {
                    _logger.Error($"GenerateCheckoutUrl: Invalid amount {request.Amount}.");
                    return StatusCode(StatusCodes.Status500InternalServerError, new
                    {
                        success = false,
                        message = "Invalid Amount."
                    });
                }

                string MainDomain = _configuration.GetValue<string>("MainDomain");
                string merchantId = _configuration.GetValue<string>("MerchantId");
                string merchantApiKey = _configuration.GetValue<string>("MerchantApiKey");

                if (string.IsNullOrWhiteSpace(MainDomain) ||
                    string.IsNullOrWhiteSpace(merchantId) ||
                    string.IsNullOrWhiteSpace(merchantApiKey))
                {
                    _logger.Error("GenerateCheckoutUrl: missing required configuration (MainDomain/MerchantId/MerchantApiKey).");
                    return StatusCode(StatusCodes.Status500InternalServerError, new
                    {
                        success = false,
                        message = "Checkout is temporarily unavailable."
                    });
                }

                var thisUser = await _dbStorage.Context.Users.FirstOrDefaultAsync(u => u.UserCode == request.UserCode);
                if (thisUser == null)
                {
                    _logger.Error($"GenerateCheckoutUrl: Current user not found. Identity: {request.UserCode}");
                    return StatusCode(StatusCodes.Status401Unauthorized, new
                    {
                        success = false,
                        message = "Checkout is temporarily unavailable."
                    });
                }

                var uplineAgent = await _dbStorage.Context.Agents.FirstOrDefaultAsync(a => a.AgentCode == thisUser.AgentCode);

                var financeLog = new FinanceLog
                {
                    AgentCode = uplineAgent?.AgentCode ?? thisUser.AgentCode,
                    AgentPath = uplineAgent?.AgentPath ?? thisUser.AgentPath,
                    AgentTableId = uplineAgent?.Id ?? 0,
                    TargetCode = thisUser.UserCode,
                    TargetPath = thisUser.AgentPath,
                    TargetTableId = thisUser.Id,
                    TargetAccountType = 0,
                    FinanceType = 0,
                    Amount = request.Amount,
                    CreatedAt = DateTime.UtcNow,
                    AgentBeforeBalance = uplineAgent?.Balance ?? 0,
                    AgentAfterBalance = uplineAgent?.Balance ?? 0,
                    TargetBeforeBalance = thisUser.CurrentBalance,
                    TargetAfterBalance = thisUser.CurrentBalance,
                    Status = 0,
                    OrderId = orderId,
                };

                await _dbStorage.Context.FinanceLogs.AddAsync(financeLog);
                await _dbStorage.Context.SaveChangesAsync();
                // --- end FinanceLog creation ---

                var checkoutUrl = BuildSignedCheckoutUrl(
                    baseUrl: "https://trustcoinpay.com/checkout",
                    merchantId: Guid.Parse(merchantId),
                    orderId: orderId,
                    amount: request.Amount,
                    currency: "USD",
                    merchantApiKey: merchantApiKey,
                    callbackUrl: "https://" + MainDomain);

                // Notify BackOffice after checkout was successfully created
                var notification = new
                {
                    type = "deposit_created",
                    orderId = financeLog.OrderId,
                    userCode = thisUser.UserCode,
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
                    _logger.Error($"GenerateCheckoutUrl: Failed to send back-office deposit notification. {ex}");
                }

                return Ok(new
                {
                    success = true,
                    checkoutUrl
                });

            }
            catch (Exception ex)
            {
                _logger.Error($"GenerateCheckoutUrl(OrderId): {ex}");
                return BadRequest(new
                {
                    success = false,
                    message = "An error occurred while generating the checkout URL."
                });
            }
        }

        /// <summary>
        /// Builds a signed SuccessUrl / CancelUrl for invoice redirect callbacks.
        /// Payload = "invoiceId={id}&status={status}&amount={amount}&currency={currency}&timestamp={ts}"
        /// Called by PayController after invoice reaches a terminal state.
        /// </summary>
        public static string BuildSignedUrl(
            string baseUrl,
            Guid invoiceId,
            string status,
            decimal amount,
            string currency,
            string merchantApiKey)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var payload = BuildInvoicePayload(invoiceId, status, amount, currency, timestamp);
            var sig = ComputeHmac(payload, merchantApiKey);

            return $"{baseUrl}?{payload}&sig={sig}";
        }

        /// <summary>
        /// Verifies a signed invoice redirect callback (SuccessUrl / CancelUrl).
        /// Returns false if signature is invalid or timestamp is stale (> 5 min).
        /// </summary>
        public static bool Verify(
            string orderId,
            string status,
            decimal amount,
            string currency,
            long timestamp,
            string sig,
            string merchantApiKey)
        {
            if (!IsTimestampFresh(timestamp)) return false;

            var payload = BuildCallbackPayload(orderId, status, amount, currency, timestamp);
            var expected = ComputeHmac(payload, merchantApiKey);

            // Constant-time comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(sig));
        }

        /// <summary>
        /// Builds a signed checkout URL for the hosted checkout entry point.
        /// Payload = "merchantId={id}&orderId={orderId}&amount={amount}&currency={currency}&timestamp={ts}"
        /// Called by the merchant backend when redirecting a customer to /checkout.
        /// </summary>
        public static string BuildSignedCheckoutUrl(
            string baseUrl,
            Guid merchantId,
            string orderId,
            decimal amount,
            string currency,
            string merchantApiKey,
            string? callbackUrl = null,
            int expiresInMinutes = 30)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var payload = BuildCheckoutPayload(merchantId, orderId, amount, currency, timestamp);
            var sig = ComputeHmac(payload, merchantApiKey);
            var amountStr = amount.ToString("G29");

            var url = $"{baseUrl}?merchantId={merchantId}&orderId={orderId}&amount={amountStr}" +
                      $"&currency={currency}&timestamp={timestamp}&sig={sig}" +
                      $"&expiresInMinutes={expiresInMinutes}";

            if (!string.IsNullOrWhiteSpace(callbackUrl))
                url += $"&callbackUrl={Uri.EscapeDataString(callbackUrl)}";

            return url;
        }

        /// <summary>
        /// Verifies a signed checkout request from the merchant.
        /// Returns false if signature is invalid or timestamp is stale (> 5 min).
        /// </summary>
        public static bool VerifyCheckout(
            Guid merchantId,
            string orderId,
            decimal amount,
            string currency,
            long timestamp,
            string sig,
            string merchantApiKey)
        {
            if (!IsTimestampFresh(timestamp)) return false;

            var payload = BuildCheckoutPayload(merchantId, orderId, amount, currency, timestamp);
            var expected = ComputeHmac(payload, merchantApiKey);

            // Constant-time comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(expected),
                Convert.FromBase64String(sig));
        }

        // -- Private helpers --

        private static string BuildInvoicePayload(Guid invoiceId, string status, decimal amount, string currency, long timestamp)
            => $"invoiceId={invoiceId}&status={status}&amount={amount.ToString("G29")}&currency={currency}&timestamp={timestamp}";

        private static string BuildCallbackPayload(string orderId, string status, decimal amount, string currency, long timestamp)
            => $"orderId={orderId}&status={status}&amount={amount.ToString("G29")}&currency={currency}&timestamp={timestamp}";

        private static string BuildCheckoutPayload(Guid merchantId, string orderId, decimal amount, string currency, long timestamp)
            => $"merchantId={merchantId}&orderId={orderId}&amount={amount.ToString("G29")}&currency={currency}&timestamp={timestamp}";

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
    }
}