using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Titan.Repository;
using MyStake.Models;
using Titan.Repository.Database;

namespace MyStake.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        public Global.Logging.ILogger _logger;
        private IWhitelabelDBStorage _dbStorage;
        private readonly IConfiguration _configuration;
        public UserController(
        Global.Logging.ILogger logger,
        IWhitelabelDBStorage dbStorage,
        IConfiguration configuration
        )
        {
            _logger = logger;
            _dbStorage = dbStorage;
            _configuration = configuration;
        }
       
        public async Task<IActionResult> Deposit()
        {
            var currencyList = await _dbStorage.Context.Currencies.ToListAsync();
            ViewData["currencylist"] = currencyList;
            ViewData["btnid"] = "deposit-btn";
            var userAgent = Request.Headers["User-Agent"].ToString();
            bool isMobile = userAgent.Contains("Android") || userAgent.Contains("iPhone") || userAgent.Contains("iPad");

            if (isMobile) return View("MobileDeposit");
            return View();
        }
        public async Task<IActionResult> Dashboard()
        {
            var currencyList = await _dbStorage.Context.Currencies.ToListAsync();
            var player = await _dbStorage.Context.Users.Where(s => s.UserCode == User.Identity.Name).FirstOrDefaultAsync();
            ViewData["currencylist"] = currencyList;
            ViewData["btnid"] = "dashboard-btn";
            ViewData["player"] = player;
            var userAgent = Request.Headers["User-Agent"].ToString();
            bool isMobile = userAgent.Contains("Android") || userAgent.Contains("iPhone") || userAgent.Contains("iPad");

            if (isMobile) return View("MobileDashboard");
            return View();
        }

        [Route("user/personal-information")]
        public async Task<IActionResult> PersonalInformation()
        {
            _logger.Error("[AccountController->PersonalInformation] [start]");

            var currencyList = await _dbStorage.Context.Currencies.ToListAsync();

            var player = await _dbStorage.Context.Users
                .Where(s => s.UserCode == User.Identity.Name)
                .FirstOrDefaultAsync();

            if (player == null)
            {
                _logger.Error("[AccountController->PersonalInformation] [player not found]");
            }

            ViewData["currencylist"] = currencyList;
            ViewData["btnid"] = "personalinfo-btn";
            ViewData["player"] = player;

            if (player != null)
            {
                var kycRequest = await _dbStorage.Context.KycRequestLogs
                    .Where(s => s.UserId == player.Id)
                    .OrderByDescending(s => s.RequestTime)
                    .FirstOrDefaultAsync();

                ViewData["kycRequest"] = kycRequest;

                _logger.Error($"[AccountController->PersonalInformation] [KYC request loaded] found: {kycRequest != null}");
            }
            else
            {
                ViewData["kycRequest"] = null;
            }

            var userAgent = Request.Headers["User-Agent"].ToString();

            bool isMobile =
                userAgent.Contains("Android") ||
                userAgent.Contains("iPhone") ||
                userAgent.Contains("iPad");

            _logger.Error($"[AccountController->PersonalInformation] [device detected] isMobile: {isMobile}");

            if (isMobile)
            {
                _logger.Error("[AccountController->PersonalInformation] [return MobilePersonalInformation]");
                return View("MobilePersonalInformation");
            }

            _logger.Error("[AccountController->PersonalInformation] [return default view]");

            return View();
        }

        [HttpPost]
        [Route("user/submit-kyc")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitKyc(
            string Country,
            string Region,
            string Birthday,
            string Gender)
        {
            _logger.Error("[AccountController->SubmitKyc] [start]");

            var player = await _dbStorage.Context.Users
                .Where(x => x.UserCode == User.Identity.Name)
                .FirstOrDefaultAsync();

            if (player == null)
            {
                _logger.Error("[AccountController->SubmitKyc] [user not found]");

                return Json(new
                {
                    success = false,
                    message = "User not found."
                });
            }

            /*
             * KYC STATUS
             *
             * 0 = Request
             * 1 = Verified
             * 2 = Rejected
             * 3 = NotSubmitted
             */

            _logger.Error($"[AccountController->SubmitKyc] [current KYC status] status: {player.KycStatus}");

            // Request or Verified users cannot submit again.
            if (player.KycStatus == 0 || player.KycStatus == 1)
            {
                _logger.Error("[AccountController->SubmitKyc] [submission rejected] KYC already requested or verified");

                return Json(new
                {
                    success = false,
                    message = "Your KYC request cannot be submitted again."
                });
            }

            player.Country = string.IsNullOrWhiteSpace(Country)
                ? null
                : Country.Trim();

            player.Region = string.IsNullOrWhiteSpace(Region)
                ? null
                : Region.Trim();

            player.Birthday = string.IsNullOrWhiteSpace(Birthday)
                ? null
                : Birthday.Trim();

            player.Gender = string.IsNullOrWhiteSpace(Gender)
                ? null
                : Gender.Trim();

            // 0 = Request
            player.KycStatus = 0;

            player.UpdatedAt = DateTime.UtcNow;

            var now = DateTime.UtcNow;

            var kycRequest = new KycRequestLog
            {
                UserId = player.Id,
                RequestTime = now,
                CreatedAt = now,
                UpdatedAt = now,

                // 0 = Request
                Status = 0
            };

            _dbStorage.Context.KycRequestLogs.Add(kycRequest);

            _logger.Error("[AccountController->SubmitKyc] [KYC request added] saving database changes");

            await _dbStorage.Context.SaveChangesAsync();

            _logger.Error("[AccountController->SubmitKyc] [success] KYC request submitted successfully");

            return Json(new
            {
                success = true,
                message = "Your KYC request has been submitted successfully."
            });
        }

        public async Task<IActionResult> Withdraw()
        {
            var currencyList = await _dbStorage.Context.Currencies.ToListAsync();
            ViewData["currencylist"] = currencyList;
            ViewData["btnid"] = "withdraw-btn";
            var userAgent = Request.Headers["User-Agent"].ToString();
            bool isMobile = userAgent.Contains("Android") || userAgent.Contains("iPhone") || userAgent.Contains("iPad");

            if (isMobile) return View("MobileWithdraw");
            return View();
        }
        public async Task<IActionResult> Transactions()
        {
            var currencyList = await _dbStorage.Context.Currencies.ToListAsync();
            ViewBag.LoadListJs = false;
            ViewData["currencylist"] = currencyList;
            ViewData["btnid"] = "transaction-btn";
            var userAgent = Request.Headers["User-Agent"].ToString();
            bool isMobile = userAgent.Contains("Android") || userAgent.Contains("iPhone") || userAgent.Contains("iPad");

            if (isMobile) return View("MobileTransaction");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetTransactions(
            string activityType = "",
            string status = "",
            string fromDate = "",
            string toDate = "",
            string timeZone = "")
        {
            _logger.Error($"[AccountController->GetTransactions] [start] activityType: {activityType}, status: {status}");

            try
            {
                var player = await _dbStorage.Context.Users
                    .Where(s => s.UserCode == User.Identity.Name)
                    .FirstOrDefaultAsync();

                if (player == null)
                {
                    _logger.Error("[AccountController->GetTransactions] [player not found]");

                    return Json(new
                    {
                        success = false,
                        message = "Player not found."
                    });
                }

                DateTime? fromUtc = null;
                DateTime? toUtc = null;

                if (!string.IsNullOrWhiteSpace(timeZone))
                {
                    try
                    {
                        var browserTimeZone =
                            TimeZoneInfo.FindSystemTimeZoneById(timeZone);

                        if (DateTime.TryParse(fromDate, out var fromLocal))
                        {
                            fromLocal = DateTime.SpecifyKind(
                                fromLocal,
                                DateTimeKind.Unspecified);

                            fromUtc = TimeZoneInfo.ConvertTimeToUtc(
                                fromLocal,
                                browserTimeZone);
                        }

                        if (DateTime.TryParse(toDate, out var toLocal))
                        {
                            toLocal = DateTime.SpecifyKind(
                                toLocal,
                                DateTimeKind.Unspecified);

                            toUtc = TimeZoneInfo.ConvertTimeToUtc(
                                toLocal,
                                browserTimeZone);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"[AccountController->GetTransactions] [timezone/date conversion failed] error: {ex.Message}");
                        // Ignore invalid timezone/date values.
                    }
                }
                else
                {
                    if (DateTime.TryParse(fromDate, out var parsedFrom))
                    {
                        fromUtc = parsedFrom;
                    }

                    if (DateTime.TryParse(toDate, out var parsedTo))
                    {
                        toUtc = parsedTo;
                    }
                }

                var bettingQuery = _dbStorage.Context.BettingLogs
                    .Where(x => x.UserCode == player.UserCode);

                if (activityType == "Deposit" ||
                    activityType == "Withdraw")
                {
                    bettingQuery = bettingQuery.Where(x => false);
                }

                if (activityType == "Bet/Win" &&
                    !string.IsNullOrWhiteSpace(status) &&
                    status != "Status" &&
                    status != "All")
                {
                    byte? bettingStatus = status switch
                    {
                        "Outstanding" => 0,
                        "Success" => 1,
                        "Canceled" => 2,
                        _ => null
                    };

                    if (bettingStatus.HasValue)
                    {
                        bettingQuery = bettingQuery.Where(
                            x => x.Status == bettingStatus.Value);
                    }
                }

                if (fromUtc.HasValue)
                {
                    bettingQuery = bettingQuery.Where(
                        x => x.CreatedAt >= fromUtc.Value);
                }

                if (toUtc.HasValue)
                {
                    bettingQuery = bettingQuery.Where(
                        x => x.CreatedAt <= toUtc.Value);
                }

                var bettingLogs = await bettingQuery.ToListAsync();

                _logger.Error($"[AccountController->GetTransactions] [betting logs retrieved] count: {bettingLogs.Count}");

                var financeQuery = _dbStorage.Context.FinanceLogs
                    .Where(x => x.TargetCode == player.UserCode);

                if (activityType == "Deposit")
                {
                    financeQuery = financeQuery.Where(
                        x => x.FinanceType == 0 ||
                             x.FinanceType == 1);
                }
                else if (activityType == "Withdraw")
                {
                    financeQuery = financeQuery.Where(
                        x => x.FinanceType == 2 ||
                             x.FinanceType == 3);
                }
                else if (activityType == "Bet/Win")
                {
                    financeQuery = financeQuery.Where(x => false);
                }

                if (activityType != "Bet/Win")
                {
                    if (string.IsNullOrWhiteSpace(status) ||
                        status == "Status" ||
                        status == "All")
                    {
                        financeQuery = financeQuery.Where(
                            x => x.Status != 6);
                    }
                    else
                    {
                        byte? financeStatus = status switch
                        {
                            "Pending" => 0,
                            "Processing" => 1,
                            "Completed" => 2,
                            "Rejected" => 3,
                            "Failed" => 4,
                            "Canceled" => 5,
                            _ => null
                        };

                        if (financeStatus.HasValue)
                        {
                            financeQuery = financeQuery.Where(
                                x => x.Status == financeStatus.Value);
                        }
                    }
                }

                if (fromUtc.HasValue)
                {
                    financeQuery = financeQuery.Where(
                        x => x.CreatedAt >= fromUtc.Value);
                }

                if (toUtc.HasValue)
                {
                    financeQuery = financeQuery.Where(
                        x => x.CreatedAt <= toUtc.Value);
                }

                var financeLogs = await financeQuery.ToListAsync();

                _logger.Error($"[AccountController->GetTransactions] [finance logs retrieved] count: {financeLogs.Count}");

                var transactions = new List<object>();

                foreach (var log in financeLogs)
                {
                    string type = log.FinanceType switch
                    {
                        0 => "Self-deposit",
                        1 => "Manual-deposit",
                        2 => "Self-withdraw",
                        3 => "Manual-withdraw",
                        _ => "Unknown"
                    };

                    string result = log.Status switch
                    {
                        0 => "Pending",
                        1 => "Processing",
                        2 => "Completed",
                        3 => "Rejected",
                        4 => "Failed",
                        5 => "Canceled",
                        _ => "Unknown"
                    };

                    transactions.Add(new
                    {
                        id = log.Id,
                        dateTime = log.CreatedAt,
                        type = type,
                        name = player.UserNickName,
                        amount = log.Amount,
                        result = result,
                        canCancel =
                            (log.FinanceType == 2 ||
                             log.FinanceType == 3) &&
                            log.Status == 0
                    });
                }

                foreach (var log in bettingLogs)
                {
                    string result = log.Status switch
                    {
                        0 => "Outstanding",
                        1 => "Success",
                        2 => "Canceled",
                        _ => "Unknown"
                    };

                    transactions.Add(new
                    {
                        id = log.Id,
                        dateTime = log.CreatedAt,
                        type = "Bet/Win",
                        name = log.GameName,
                        amount = log.BetAmount,
                        winAmount = log.WinAmount,
                        amountDisplay =
                            $"{log.BetAmount} / {log.WinAmount:+0.##;-0.##;0}",
                        result = result,
                        canCancel = false
                    });
                }

                transactions = transactions
                    .OrderByDescending(x => ((dynamic)x).dateTime)
                    .ToList();

                _logger.Error($"[AccountController->GetTransactions] [success] total transactions: {transactions.Count}");

                return Json(new
                {
                    success = true,
                    userCode = player.UserCode,
                    transactions = transactions
                });
            }
            catch (Exception ex)
            {
                _logger.Error(
                    ex,
                    "[AccountController->GetTransactions] [error getting transactions]");

                return Json(new
                {
                    success = false,
                    message = "An error occurred while loading transactions."
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CancelWithdrawal(int id)
        {
            _logger.Error($"[AccountController->CancelWithdrawal] [start] transaction ID: {id}");

            try
            {
                var player = await _dbStorage.Context.Users
                    .Where(s => s.UserCode == User.Identity.Name)
                    .FirstOrDefaultAsync();

                if (player == null)
                {
                    _logger.Error("[AccountController->CancelWithdrawal] [player not found]");

                    return Json(new
                    {
                        success = false,
                        message = "Player not found."
                    });
                }

                var financeLog = await _dbStorage.Context.FinanceLogs
                    .Where(x =>
                        x.Id == id &&
                        x.TargetCode == player.UserCode)
                    .FirstOrDefaultAsync();

                if (financeLog == null)
                {
                    _logger.Error($"[AccountController->CancelWithdrawal] [transaction not found] transaction ID: {id}");

                    return Json(new
                    {
                        success = false,
                        message = "Transaction not found."
                    });
                }

                // Only withdrawals can be canceled.

                if (financeLog.FinanceType != 2 &&
                    financeLog.FinanceType != 3)
                {
                    _logger.Error($"[AccountController->CancelWithdrawal] [rejected: not a withdrawal] transaction ID: {id}, FinanceType: {financeLog.FinanceType}");

                    return Json(new
                    {
                        success = false,
                        message = "Only withdrawal transactions can be canceled."
                    });
                }

                // Only Pending transactions can be canceled.

                if (financeLog.Status != 0)
                {
                    _logger.Error($"[AccountController->CancelWithdrawal] [rejected: transaction not pending] transaction ID: {id}, Status: {financeLog.Status}");

                    return Json(new
                    {
                        success = false,
                        message = "Only pending withdrawals can be canceled."
                    });
                }

                // 5 = Canceled

                financeLog.Status = 5;

                await _dbStorage.Context.SaveChangesAsync();

                _logger.Error($"[AccountController->CancelWithdrawal] [success] withdrawal canceled, transaction ID: {id}");

                return Json(new
                {
                    success = true,
                    message = "Withdrawal canceled successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.Error(
                    ex,
                    $"[AccountController->CancelWithdrawal] [error canceling withdrawal] transaction ID: {id}");

                return Json(new
                {
                    success = false,
                    message = "An error occurred while canceling the withdrawal."
                });
            }
        }
        //[Route("user/account-verification")]
        //public async Task<IActionResult> Verification()
        //{
        //    var currencyList = await _dbStorage.Context.Currencies.ToListAsync();
        //    ViewData["currencylist"] = currencyList;
        //    return View();
        //}



        //[Route("user/gifts/free-spins")]
        //public async Task<IActionResult> FreeSpins()
        //{
        //    var currencyList = await _dbStorage.Context.Currencies.ToListAsync();
        //    ViewData["currencylist"] = currencyList;
        //    return View();
        //}
        //[Route("user/gifts/free-bets")]
        //public async Task<IActionResult> FreeBets()
        //{
        //    var currencyList = await _dbStorage.Context.Currencies.ToListAsync();
        //    ViewData["currencylist"] = currencyList;
        //    return View();
        //}

        //[Route("user/gifts/mini-games")]
        //public async Task<IActionResult> MiniGames()
        //{
        //    var currencyList = await _dbStorage.Context.Currencies.ToListAsync();
        //    ViewData["currencylist"] = currencyList;
        //    return View("MiniGames");
        //}

        //[Route("user/gifts/bonuses")]
        //public async Task<IActionResult> Bonuses()
        //{
        //    var currencyList = await _dbStorage.Context.Currencies.ToListAsync();
        //    ViewData["currencylist"] = currencyList;
        //    return View("Bonuses");
        //}

        //[Route("user/gifts/sports-bonus")]
        //public async Task<IActionResult> SportsBonus()
        //{
        //    var currencyList = await _dbStorage.Context.Currencies.ToListAsync();
        //    ViewData["currencylist"] = currencyList;
        //    return View("SportsBonus");
        //}

        //[Route("user/gifts/crypto-cashback")]
        //public async Task<IActionResult> CryptoCashback()
        //{
        //    var currencyList = await _dbStorage.Context.Currencies.ToListAsync();
        //    ViewData["currencylist"] = currencyList;
        //    return View("CryptoCashback");
        //}

        //[Route("user/freespins-by-promo")]
        //public async Task<IActionResult> FreeSpinsByPromo()
        //{
        //    var currencyList = await _dbStorage.Context.Currencies.ToListAsync();
        //    ViewData["currencylist"] = currencyList;
        //    return View();
        //}

        [Route("user/Notifications")]
        public async Task<IActionResult> Notifications()
        {
            var currencyList = await _dbStorage.Context.Currencies.ToListAsync();
            ViewData["currencylist"] = currencyList;
            ViewData["btnid"] = "notification-btn";
            var userAgent = Request.Headers["User-Agent"].ToString();
            bool isMobile = userAgent.Contains("Android") || userAgent.Contains("iPhone") || userAgent.Contains("iPad");

            if (isMobile) return View("MobileNotification");
            return View();
        }
    }
}
