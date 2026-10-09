using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Whitelabel_backoffice.Controllers
{
    public class NotificationController : ControllerBase
    {
        private readonly WebSocketHandler _handler;
        private readonly Global.Logging.ILogger _logger;

        public NotificationController(
            WebSocketHandler handler,
            Global.Logging.ILogger logger)
        {
            _handler = handler;
            _logger = logger;
        }

        [HttpPost]
        [Route("api/notification/withdraw")]
        public async Task<IActionResult> WithdrawNotification(
            [FromBody] WithdrawNotificationRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid notification request."
                    });
                }

                var message = JsonSerializer.Serialize(new
                {
                    type = "withdraw",
                    orderId = request.OrderId,
                    userCode = request.UserCode,
                    amount = request.Amount
                });

                await _handler.SendToUserAsync(
                    "one",
                    message);

                return Ok(new
                {
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.Error(
                    $"WithdrawNotification: Failed to send notification. {ex}");

                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to send withdrawal notification."
                });
            }
        }

        [HttpPost]
        [Route("api/notification/deposit")]
        public async Task<IActionResult> DepositNotification(
            [FromBody] DepositNotificationRequest request)
                {
                    try
                    {
                        if (request == null)
                        {
                            return BadRequest(new
                            {
                                success = false,
                                message = "Invalid notification request."
                            });
                        }

                        var message = JsonSerializer.Serialize(new
                        {
                            type = request.Type,
                            orderId = request.OrderId,
                            userCode = request.UserCode,
                            amount = request.Amount
                        });

                        await _handler.SendToUserAsync(
                            "one",
                            message);

                        return Ok(new
                        {
                            success = true
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(
                            $"DepositNotification: Failed to send notification. {ex}");

                        return StatusCode(500, new
                        {
                            success = false,
                            message = "Failed to send deposit notification."
                        });
                    }
        }
    }

    public class WithdrawNotificationRequest
    {
        public string OrderId { get; set; } = string.Empty;

        public string UserCode { get; set; } = string.Empty;

        public decimal Amount { get; set; }
    }
    public class DepositNotificationRequest
    {
        public string Type { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string UserCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}