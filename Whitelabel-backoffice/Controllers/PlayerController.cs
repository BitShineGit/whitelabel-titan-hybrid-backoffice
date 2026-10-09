using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Whitelabel_backoffice.Database;
using Whitelabel_backoffice.Models.Requests;
using Whitelabel_backoffice.Services;

namespace Whitelabel_backoffice.Controllers
{
    public class PlayerController : Controller
    {
        private IWhitelabelDBStorage _dbStorage;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<PlayerController> _logger;
        private readonly ISettingService _settingService;


        public PlayerController(IWhitelabelDBStorage dbStorage, UserManager<IdentityUser> userManager, ILogger<PlayerController> logger, ISettingService settingService)
        {
            _dbStorage = dbStorage;
            _userManager = userManager;
            _logger = logger;
            _settingService = settingService;
        }

        public async Task<IActionResult> Index()
        {
            var userCodeList = await _dbStorage.Context.Users
                .Select(x => x.UserCode)
                .ToListAsync();

            var agentCodeList = await _dbStorage.Context.Agents
                .Select(x => x.AgentCode)
                .ToListAsync();

            var currencyList = await _dbStorage.Context.Currencies
                .Select(x => x.CurrencyCode)
                .ToListAsync();


            ViewData["UserCodeList"] = userCodeList;
            ViewData["AgentCodeList"] = agentCodeList;
            ViewData["CurrencyCodeList"] = currencyList;


            // Manual transaction settings
            ViewData["EnableManualDeposit"] =
                await _settingService.GetBooleanValueAsync(
                    "Deposit",
                    "Manual");


            ViewData["EnableManualWithdraw"] =
                await _settingService.GetBooleanValueAsync(
                    "Withdraw",
                    "Manual");


            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetPlayers()
        {
            // ==========================================
            // DATATABLE PARAMETERS
            // ==========================================

            var draw =
                Request.Form["draw"].FirstOrDefault();

            var start = Convert.ToInt32(
                Request.Form["start"].FirstOrDefault() ?? "0");

            var length = Convert.ToInt32(
                Request.Form["length"].FirstOrDefault() ?? "10");


            // ==========================================
            // FILTER PARAMETERS
            // ==========================================

            var userCode =
                Request.Form["userCode"].FirstOrDefault();

            var uplineCode =
                Request.Form["uplineCode"].FirstOrDefault();

            var currency =
                Request.Form["currency"].FirstOrDefault();

            var status =
                Request.Form["status"].FirstOrDefault();

            var createdFrom =
                Request.Form["createdFrom"].FirstOrDefault();

            var createdTo =
                Request.Form["createdTo"].FirstOrDefault();


            // ==========================================
            // CURRENT AGENT'S TIMEZONE OFFSET
            // (used only for shaping the response, not filtering)
            // ==========================================

            var currentAgentOffset = await GetCurrentAgentTimeZoneOffsetAsync();


            // ==========================================
            // BASE QUERY
            // ==========================================

            var query = _dbStorage.Context.Users
                .AsNoTracking()
                .AsQueryable();


            // ==========================================
            // TOTAL RECORDS
            // ==========================================

            var recordsTotal =
                await query.CountAsync();


            // ==========================================
            // USER CODE FILTER
            // ==========================================

            if (!string.IsNullOrWhiteSpace(userCode))
            {
                query = query.Where(x =>
                    x.UserCode == userCode);
            }


            // ==========================================
            // UPLINE CODE FILTER
            // ==========================================

            if (!string.IsNullOrWhiteSpace(uplineCode))
            {
                query = query.Where(x =>
                    x.UplineCode == uplineCode);
            }


            // ==========================================
            // CURRENCY FILTER
            // ==========================================

            if (!string.IsNullOrWhiteSpace(currency))
            {
                query = query.Where(x =>
                    x.CurrencyCode == currency);
            }


            // ==========================================
            // STATUS FILTER
            // ==========================================

            if (!string.IsNullOrWhiteSpace(status) &&
                byte.TryParse(status, out var statusValue))
            {
                query = query.Where(x =>
                    x.Status == statusValue);
            }


            // ==========================================
            // CREATED AT FILTER
            //
            // createdFrom / createdTo are treated as UTC dates and
            // compared directly against CreatedAt (also UTC) — no
            // timezone conversion needed for filtering.
            // ==========================================

            DateTime? createdFromDate = null;

            DateTime? createdToDate = null;


            if (DateTime.TryParse(
                createdFrom,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal
                    | System.Globalization.DateTimeStyles.AdjustToUniversal,
                out var fromDate))
            {
                createdFromDate = fromDate.Date;
            }


            if (DateTime.TryParse(
                createdTo,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal
                    | System.Globalization.DateTimeStyles.AdjustToUniversal,
                out var toDate))
            {
                createdToDate = toDate.Date;
            }


            // Start date
            if (createdFromDate.HasValue)
            {
                query = query.Where(x =>
                    x.CreatedAt >= createdFromDate.Value);
            }


            // End date
            if (createdToDate.HasValue)
            {
                var nextDay =
                    createdToDate.Value.AddDays(1);

                query = query.Where(x =>
                    x.CreatedAt < nextDay);
            }


            // ==========================================
            // FILTERED RECORDS
            // ==========================================

            var recordsFiltered = await query.CountAsync();

            if (recordsFiltered == 0)
            {
                start = 0;
            }
            else if (start >= recordsFiltered)
            {
                start = Math.Max(
                    0,
                    ((recordsFiltered - 1) / length) * length
                );
            }


            // ==========================================
            // ORDERING
            // ==========================================

            var orderColumn =
                Request.Form["order[0][column]"].FirstOrDefault();

            var orderDirection =
                Request.Form["order[0][dir]"].FirstOrDefault();


            switch (orderColumn)
            {
                // --------------------------------------
                // ID
                // --------------------------------------

                case "0":

                    query = orderDirection == "desc"
                        ? query.OrderByDescending(x => x.Id)
                        : query.OrderBy(x => x.Id);

                    break;


                // --------------------------------------
                // TOTAL BALANCE
                // --------------------------------------

                case "3":

                    query = orderDirection == "desc"
                        ? query.OrderByDescending(x => x.CurrentBalance)
                        : query.OrderBy(x => x.CurrentBalance);

                    break;


                // --------------------------------------
                // CREATED AT
                // --------------------------------------

                case "6":

                    query = orderDirection == "desc"
                        ? query.OrderByDescending(x => x.CreatedAt)
                        : query.OrderBy(x => x.CreatedAt);

                    break;


                // --------------------------------------
                // DEFAULT
                // --------------------------------------

                default:

                    query = query.OrderByDescending(x => x.Id);

                    break;
            }


            // ==========================================
            // PAGING + SELECT
            // ==========================================

            var players = await query
                .Skip(start)
                .Take(length)
                .Select(x => new
                {
                    id = x.Id,
                    userCode = x.UserCode,
                    userEmail = x.UserEmail,
                    currentBalance = x.CurrentBalance,
                    currencyCode = x.CurrencyCode,
                    status = x.Status,
                    uplineCode = x.UplineCode,

                    // Shift UTC CreatedAt into the logged-in agent's local time
                    createdAt = x.CreatedAt + currentAgentOffset
                })
                .ToListAsync();


            // ==========================================
            // DATATABLE RESPONSE
            // ==========================================

            return Json(new
            {
                draw = draw,

                recordsTotal = recordsTotal,

                recordsFiltered = recordsFiltered,

                data = players
            });
        }

        [HttpPost]
        [Route("/player/deleteplayer")]
        public async Task<IActionResult> DeletePlayer(int id)
        {
            try
            {
                var player = await _dbStorage.Context.Users.FirstOrDefaultAsync(u => u.Id == id);

                if (player == null)
                {
                    return Json(new { success = false, message = "Player not found." });
                }

                IdentityUser identityUser = await _userManager.FindByNameAsync(player.UserCode);
                if (identityUser == null)
                {
                    return Json(new { success = false, message = "Identity User not found." });
                }

                var removeResult = await _userManager.DeleteAsync(identityUser);
                if (removeResult.Succeeded)
                {
                    _dbStorage.Context.Users.Remove(player);
                    await _dbStorage.Context.SaveChangesAsync();

                    return Json(new { success = true, message = $"Player \"{player.UserCode}\" was deleted successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to Remove User."});
                }
            }
            catch (Exception ex)
            {
                // log ex as needed
                return Json(new { success = false, message = "An unexpected error occurred while deleting the player." });
            }
        }

        [HttpGet]
        [Route("/player/detail/{id}")]
        public async Task<IActionResult> Detail(int id)
        {
            var user = await _dbStorage.Context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return Unauthorized();
            }

            var providerCodeList = await _dbStorage.Context.Providers.Select(p => p.ProviderCode).ToListAsync();

            var currentAgentOffset = await GetCurrentAgentTimeZoneOffsetAsync();

            // Shift UTC CreatedAt into the logged-in agent's local time
            user.CreatedAt = user.CreatedAt + currentAgentOffset;

            ViewData["UserCode"] = user.UserCode;
            ViewData["UserId"] = id;
            ViewData["User"] = user;
            ViewData["ProviderCodeList"] = providerCodeList;
            ViewData["UserCreatedAtLocal"] = user.CreatedAt + currentAgentOffset;

            return View();
        }

        [HttpGet]
        [Route("/player/edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["userId"] = id;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdatePlayerProfileRequest request)
        {
            var user = await _dbStorage.Context.Users.FirstOrDefaultAsync(x => x.UserCode == request.UserCode);

            if (user == null)
            {
                return Json(new
                {
                    success = false,
                    message = "User not found."
                });
            }

            user.UserNickName = request.NickName;
            user.UserEmail = request.UserEmail;
            user.PhoneNumber = request.PhoneNumber;
            user.Status = request.Status;

            var currency = await _dbStorage.Context.Currencies.FirstOrDefaultAsync(x => x.Id == request.CurrencyId);

            if (currency != null) user.CurrencyId = currency.Id;
            else return BadRequest();

            user.UpdatedAt = DateTime.UtcNow;

            await _dbStorage.Context.SaveChangesAsync();

            return Json(new
            {
                success = true
            });
        }

        [HttpPost]
        public async Task<IActionResult> GetBettingLogs()
        {
            var draw =
                Request.Form["draw"].FirstOrDefault();

            var start = Convert.ToInt32(
                Request.Form["start"].FirstOrDefault() ?? "0");

            var length = Convert.ToInt32(
                Request.Form["length"].FirstOrDefault() ?? "10");

            var playerCodeValue =
                Request.Form["playerCode"].FirstOrDefault();

            var providerCode =
                Request.Form["providerCode"].FirstOrDefault();

            var gameCode =
                Request.Form["gameCode"].FirstOrDefault();

            var roundId =
                Request.Form["roundId"].FirstOrDefault();

            var historyId =
                Request.Form["historyId"].FirstOrDefault();

            var status =
                Request.Form["status"].FirstOrDefault();

            var createdFrom =
                Request.Form["createdFrom"].FirstOrDefault();

            var createdTo =
                Request.Form["createdTo"].FirstOrDefault();

            var currentAgentOffset = await GetCurrentAgentTimeZoneOffsetAsync();

            var query = _dbStorage.Context.BettingLogs
                .AsNoTracking()
                .AsQueryable();

            if (playerCodeValue != null)
            {
                query = query.Where(x =>
                    x.UserCode == playerCodeValue);
            }

            var recordsTotal =
                await query.CountAsync();

            if (!string.IsNullOrWhiteSpace(providerCode))
            {
                query = query.Where(x =>
                    x.ProviderCode == providerCode);
            }

            if (!string.IsNullOrWhiteSpace(gameCode))
            {
                query = query.Where(x =>
                    x.GameCode.Contains(gameCode));
            }

            if (!string.IsNullOrWhiteSpace(roundId))
            {
                query = query.Where(x =>
                    x.RoundId.Contains(roundId));
            }

            if (long.TryParse(historyId, out var historyIdValue))
            {
                query = query.Where(x =>
                    x.HistoryId == historyIdValue);
            }

            if (!string.IsNullOrWhiteSpace(status) &&
                byte.TryParse(status, out var statusValue))
            {
                query = query.Where(x =>
                    x.Status == statusValue);
            }


            // ---------------------------------------------------------
            // Created At date range
            //
            // createdFrom / createdTo are dates in the logged-in agent's
            // local timezone. CreatedAt in the DB is assumed to be UTC,
            // so we convert the local day boundaries to UTC using the
            // agent's fixed offset before filtering.
            // ---------------------------------------------------------

            DateTime? createdFromUtc = null;
            DateTime? createdToUtc = null;

            if (DateTime.TryParse(
                createdFrom,
                out var fromDate))
            {
                createdFromUtc = fromDate.Date - currentAgentOffset;
            }

            if (DateTime.TryParse(
                createdTo,
                out var toDate))
            {
                createdToUtc = toDate.Date.AddDays(1) - currentAgentOffset;
            }


            if (createdFromUtc.HasValue)
            {
                query = query.Where(x =>
                    x.CreatedAt >= createdFromUtc.Value);
            }


            if (createdToUtc.HasValue)
            {
                query = query.Where(x =>
                    x.CreatedAt < createdToUtc.Value);
            }


            var recordsFiltered = await query.CountAsync();

            if (recordsFiltered == 0)
            {
                start = 0;
            }
            else if (start >= recordsFiltered)
            {
                start = Math.Max(
                    0,
                    ((recordsFiltered - 1) / length) * length
                );
            }


            var orderColumn =
                Request.Form["order[0][column]"].FirstOrDefault();

            var orderDirection =
                Request.Form["order[0][dir]"].FirstOrDefault();


            switch (orderColumn)
            {
                // ID
                case "0":

                    query = orderDirection == "desc"
                        ? query.OrderByDescending(x => x.Id)
                        : query.OrderBy(x => x.Id);

                    break;


                // Bet Amount
                case "5":

                    query = orderDirection == "desc"
                        ? query.OrderByDescending(x => x.BetAmount)
                        : query.OrderBy(x => x.BetAmount);

                    break;


                // Win Amount
                case "6":

                    query = orderDirection == "desc"
                        ? query.OrderByDescending(x => x.WinAmount)
                        : query.OrderBy(x => x.WinAmount);

                    break;


                // Created At
                case "10":

                    query = orderDirection == "desc"
                        ? query.OrderByDescending(x => x.CreatedAt)
                        : query.OrderBy(x => x.CreatedAt);

                    break;


                // Default
                default:

                    query =
                        query.OrderByDescending(x => x.Id);

                    break;
            }


            // ---------------------------------------------------------
            // Pagination + projection
            // ---------------------------------------------------------

            var bettingLogs = await query
                .Skip(start)
                .Take(length)
                .Select(x => new
                {
                    id = x.Id,

                    providerCode = x.ProviderCode,

                    providerName = x.ProviderName,

                    gameCode = x.GameCode,

                    gameName = x.GameName,

                    roundId = x.RoundId,

                    betAmount = x.BetAmount,

                    winAmount = x.WinAmount,

                    beforeBalance = x.BeforeBalance,

                    afterBalance = x.AfterBalance,

                    status = x.Status,

                    createdAt = x.CreatedAt + currentAgentOffset,

                    historyId = x.HistoryId
                })
                .ToListAsync();


            // ---------------------------------------------------------
            // DataTables response
            // ---------------------------------------------------------

            return Json(new
            {
                draw = draw,

                recordsTotal = recordsTotal,

                recordsFiltered = recordsFiltered,

                data = bettingLogs
            });
        }


        private async Task<TimeSpan> GetCurrentAgentTimeZoneOffsetAsync()
        {
            var aspNetUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(aspNetUserId))
            {
                return TimeSpan.Zero;
            }

            var timeZone = await _dbStorage.Context.Agents
                .AsNoTracking()
                .Where(a => a.AspNetUserId == aspNetUserId)
                .Select(a => a.TimeZone)
                .FirstOrDefaultAsync();

            return TryParseOffset(timeZone, out var offset)
                ? offset
                : TimeSpan.Zero;
        }

        private static bool TryParseOffset(string offsetStr, out TimeSpan offset)
        {
            offset = TimeSpan.Zero;

            if (string.IsNullOrWhiteSpace(offsetStr))
            {
                return false;
            }

            var normalized = offsetStr.StartsWith("+")
                ? offsetStr[1..]
                : offsetStr;

            return TimeSpan.TryParse(normalized, out offset);
        }

        [HttpGet]
        public async Task<IActionResult> GetBettingLogProviders()
        {
            var providers = await _dbStorage.Context.BettingLogs
                .AsNoTracking()
                .Where(x =>
                    x.ProviderCode != null &&
                    x.ProviderCode != "")
                .Select(x => x.ProviderCode)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            return Json(
                providers.Select(x => new
                {
                    providerCode = x
                })
            );
        }

        [HttpPost]
        public async Task<IActionResult> GetFinanceLogs()
        {
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                var startValue = Request.Form["start"].FirstOrDefault();
                var lengthValue = Request.Form["length"].FirstOrDefault();

                var start = 0;
                var length = 25;

                if (int.TryParse(startValue, out var parsedStart) && parsedStart >= 0)
                {
                    start = parsedStart;
                }

                if (int.TryParse(lengthValue, out var parsedLength) && parsedLength > 0)
                {
                    length = parsedLength;
                }

                var playerIdValue = Request.Form["playerId"].FirstOrDefault();
                var orderId = Request.Form["orderId"].FirstOrDefault();
                var agentCode = Request.Form["agentCode"].FirstOrDefault();
                var financeType = Request.Form["financeType"].FirstOrDefault();
                var amount = Request.Form["amount"].FirstOrDefault();
                var status = Request.Form["status"].FirstOrDefault();
                var fromDate = Request.Form["fromDate"].FirstOrDefault();
                var toDate = Request.Form["toDate"].FirstOrDefault();

                if (!int.TryParse(playerIdValue, out var playerId) || playerId <= 0)
                {
                    return Json(new
                    {
                        draw,
                        recordsTotal = 0,
                        recordsFiltered = 0,
                        data = Array.Empty<object>()
                    });
                }

                var query = _dbStorage.Context.FinanceLogs
                    .AsNoTracking()
                    .Where(x => x.TargetTableId == playerId);

                var recordsTotal = await query.CountAsync();

                if (!string.IsNullOrWhiteSpace(orderId))
                {
                    query = query.Where(x =>
                        x.Id.ToString().Contains(orderId));
                }

                if (!string.IsNullOrWhiteSpace(agentCode))
                {
                    query = query.Where(x =>
                        x.AgentCode == agentCode);
                }

                if (!string.IsNullOrWhiteSpace(financeType) &&
                    byte.TryParse(financeType, out var financeTypeValue))
                {
                    query = query.Where(x =>
                        x.FinanceType == financeTypeValue);
                }

                if (!string.IsNullOrWhiteSpace(amount) &&
                    decimal.TryParse(amount, out var amountValue))
                {
                    query = query.Where(x =>
                        x.Amount == amountValue);
                }

                if (!string.IsNullOrWhiteSpace(status) &&
                    byte.TryParse(status, out var statusValue))
                {
                    query = query.Where(x =>
                        x.Status == statusValue);
                }

                DateTime? createdFromDate = null;
                DateTime? createdToDate = null;

                if (DateTime.TryParse(fromDate, out var parsedFromDate))
                {
                    createdFromDate = parsedFromDate.Date;
                }

                if (DateTime.TryParse(toDate, out var parsedToDate))
                {
                    createdToDate = parsedToDate.Date;
                }

                if (createdFromDate.HasValue)
                {
                    query = query.Where(x =>
                        x.CreatedAt >= createdFromDate.Value);
                }

                if (createdToDate.HasValue)
                {
                    var nextDay = createdToDate.Value.AddDays(1);

                    query = query.Where(x =>
                        x.CreatedAt < nextDay);
                }

                var recordsFiltered = await query.CountAsync();

                var orderColumn = Request.Form["order[0][column]"].FirstOrDefault();
                var orderDirection = Request.Form["order[0][dir]"].FirstOrDefault();

                switch (orderColumn)
                {
                    case "1":
                        query = orderDirection == "desc"
                            ? query.OrderByDescending(x => x.Id)
                            : query.OrderBy(x => x.Id);
                        break;

                    case "2":
                        query = orderDirection == "desc"
                            ? query.OrderByDescending(x => x.AgentCode)
                            : query.OrderBy(x => x.AgentCode);
                        break;

                    case "3":
                        query = orderDirection == "desc"
                            ? query.OrderByDescending(x => x.FinanceType)
                            : query.OrderBy(x => x.FinanceType);
                        break;

                    case "4":
                        query = orderDirection == "desc"
                            ? query.OrderByDescending(x => x.Amount)
                            : query.OrderBy(x => x.Amount);
                        break;

                    case "6":
                        query = orderDirection == "asc"
                            ? query.OrderBy(x => x.CreatedAt)
                            : query.OrderByDescending(x => x.CreatedAt);
                        break;

                    case "7":
                        query = orderDirection == "desc"
                            ? query.OrderByDescending(x => x.Status)
                            : query.OrderBy(x => x.Status);
                        break;

                    default:
                        query = query.OrderByDescending(x => x.CreatedAt);
                        break;
                }

                if (recordsFiltered == 0)
                {
                    start = 0;
                }
                else if (start >= recordsFiltered)
                {
                    start = Math.Max(
                        0,
                        ((recordsFiltered - 1) / length) * length
                    );
                }

                var financeLogs = await query
                    .Skip(start)
                    .Take(length)
                    .Select(x => new
                    {
                        id = x.Id,
                        orderId = x.Id,
                        agentCode = x.AgentCode,
                        financeType = x.FinanceType,
                        amount = x.Amount,
                        targetBeforeBalance = x.TargetBeforeBalance,
                        targetAfterBalance = x.TargetAfterBalance,
                        createdAt = x.CreatedAt,
                        status = x.Status,
                        targetCode = x.TargetCode,
                        targetAccountType = x.TargetAccountType
                    })
                    .ToListAsync();

                return Json(new
                {
                    draw,
                    recordsTotal,
                    recordsFiltered,
                    data = financeLogs
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    draw = Request.Form["draw"].FirstOrDefault(),
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = Array.Empty<object>(),
                    error = "Unable to load finance logs."
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFinanceLogAgents(int playerId)
        {
            var agent = await _dbStorage.Context.Agents.Where(s => s.AgentCode == User.Identity.Name).FirstOrDefaultAsync();
            if (playerId <= 0)
            {
                return Json(Array.Empty<object>());
            }

            var agents = await _dbStorage.Context.FinanceLogs
                .AsNoTracking()
                .Where(x =>
                    x.TargetTableId == playerId &&
                    x.AgentCode != null &&
                    x.AgentCode != "")
                .Select(x => x.AgentCode)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            return Json(
                agents.Select(x => new
                {
                    agentCode = x
                })
            );
        }
        [HttpGet]
        public async Task<IActionResult> GetAgentBalance()
        {
            var agent = await _dbStorage.Context.Agents
                .Where(s => s.AgentCode == User.Identity.Name)
                .FirstOrDefaultAsync();

            if (agent == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Agent not found."
                });
            }

            return Json(new
            {
                success = true,
                balance = agent.Balance
            });
        }

        // =========================================================
        // DEPOSIT / WITHDRAW
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> TransferPlayerBalance(
            int playerId,
            decimal amount,
            string transactionType)
        {
            if (transactionType == "deposit")
            {
                var enableManualDeposit =
                    await _settingService.GetBooleanValueAsync(
                        "Deposit",
                        "Manual");

                if (!enableManualDeposit)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Manual deposit is disabled."
                    });
                }
            }


            if (transactionType == "withdraw")
            {
                var enableManualWithdraw =
                    await _settingService.GetBooleanValueAsync(
                        "Withdraw",
                        "Manual");

                if (!enableManualWithdraw)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Manual withdraw is disabled."
                    });
                }
            }




            if (amount <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Amount must be greater than 0."
                });
            }

            if (transactionType != "deposit" &&
                transactionType != "withdraw")
            {
                return Json(new
                {
                    success = false,
                    message = "Invalid transaction type."
                });
            }

            // Get logged-in agent
            var agent = await _dbStorage.Context.Agents
                .Where(s => s.AgentCode == User.Identity.Name)
                .FirstOrDefaultAsync();

            if (agent == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Agent not found."
                });
            }

            // Get player
            var player = await _dbStorage.Context.Users
                .Where(x => x.Id == playerId)
                .FirstOrDefaultAsync();

            if (player == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Player not found."
                });
            }

            // Currency must be USD
            if (player.CurrencyCode != "USD")
            {
                return Json(new
                {
                    success = false,
                    message = "Only USD transactions are supported."
                });
            }

            // ==========================================
            // SAVE BEFORE BALANCES
            // ==========================================

            var agentBeforeBalance = agent.Balance;
            var targetBeforeBalance = player.CurrentBalance;

            // ==========================================
            // DEPOSIT
            // ==========================================

            if (transactionType == "deposit")
            {
                if (agent.Balance < amount)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Agent balance is insufficient."
                    });
                }

                agent.Balance -= amount;

                player.CurrentBalance += amount;

                player.TotalDepositAmount += amount;

                player.LastDepositAt = DateTime.UtcNow;

                if (player.FirstDepositAt == null)
                {
                    player.FirstDepositAt = DateTime.UtcNow;
                }
            }

            // ==========================================
            // WITHDRAW
            // ==========================================

            else
            {
                if (player.CurrentBalance < amount)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Player balance is insufficient."
                    });
                }


                // =====================================================
                // Check Welcome Bonus Wagering Requirement
                // =====================================================

                if (player.WelcomeBonusAmount > 0)
                {
                    int requiredBettingAmount =
                        await _settingService.GetIntValueAsync(
                            "Bonus",
                            "RequiredBettingAmount");


                    decimal requiredTotalBetAmount =
                        player.WelcomeBonusAmount *
                        requiredBettingAmount;


                    if (player.TotalBetAmount < requiredTotalBetAmount)
                    {
                        return Json(new
                        {
                            success = false,
                            message =
                                $"Player has not completed wagering requirement. " +
                                $"Required: {requiredTotalBetAmount:0.##}, " +
                                $"Current: {player.TotalBetAmount:0.##}."
                        });
                    }
                }


                player.CurrentBalance -= amount;

                player.TotalWithdrawAmount += amount;

                player.LastWithdrawAt = DateTime.UtcNow;

                if (player.FirstWithdrawAt == null)
                {
                    player.FirstWithdrawAt = DateTime.UtcNow;
                }

                agent.Balance += amount;
            }

            // ==========================================
            // CREATE FINANCE LOG
            // ==========================================

            var financeLog = new FinanceLog
            {
                AgentCode = agent.AgentCode,
                AgentPath = agent.AgentPath,
                AgentTableId = agent.Id,

                TargetCode = player.UserCode,
                TargetPath = player.AgentPath,
                TargetTableId = player.Id,
                TargetAccountType = 0,

                FinanceType = transactionType == "deposit"
                    ? (byte)1
                    : (byte)3,

                Amount = amount,

                CreatedAt = DateTime.UtcNow,

                AgentBeforeBalance = agentBeforeBalance,
                AgentAfterBalance = agent.Balance,

                TargetBeforeBalance = targetBeforeBalance,
                TargetAfterBalance = player.CurrentBalance,

                Status = 2,

                OrderId = "ORD-" + Guid.NewGuid().ToString()
            };

            _dbStorage.Context.FinanceLogs.Add(financeLog);

            player.UpdatedAt = DateTime.UtcNow;

            await _dbStorage.Context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = transactionType == "deposit"
                    ? "Deposit completed successfully."
                    : "Withdrawal completed successfully.",
                agentBalance = agent.Balance,
                playerBalance = player.CurrentBalance
            });
        }


        [HttpPost]
        public async Task<IActionResult> CompleteFinanceLog(int id)
        {
            await using var transaction =
                await _dbStorage.Context.Database
                    .BeginTransactionAsync(
                        System.Data.IsolationLevel.Serializable
                    );

            try
            {
                var loggedInAgent =
                    await ResolveFinanceLogAgentAsync();

                if (loggedInAgent == null)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Agent not found."
                    });
                }

                var financeLogQuery =
                    _dbStorage.Context.FinanceLogs
                        .AsQueryable();

                financeLogQuery =
                    ApplyFinanceLogAgentScope(
                        financeLogQuery,
                        loggedInAgent
                    );

                var financeLog =
                    await financeLogQuery
                        .FirstOrDefaultAsync(
                            x => x.Id == id
                        );

                if (financeLog == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Finance log not found."
                    });
                }

                if (financeLog.FinanceType != 2)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "Only Self Withdraw can be completed."
                    });
                }

                if (financeLog.Status != 0)
                {
                    return Conflict(new
                    {
                        success = false,
                        message =
                            "This transaction is no longer Pending."
                    });
                }

                if (financeLog.TargetAccountType != 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "Self Withdraw target is not a Player."
                    });
                }

                var amount =
                    financeLog.Amount;

                if (amount <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "Finance amount must be greater than zero."
                    });
                }

                var player =
                    await _dbStorage.Context.Users
                        .FirstOrDefaultAsync(
                            x =>
                                x.Id ==
                                financeLog.TargetTableId

                                &&

                                x.UserCode ==
                                financeLog.TargetCode
                        );

                if (player == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message =
                            "Target player was not found."
                    });
                }

                var transactionAgent =
                    await _dbStorage.Context.Agents
                        .FirstOrDefaultAsync(
                            x =>
                                x.Id ==
                                financeLog.AgentTableId

                                &&

                                x.AgentCode ==
                                financeLog.AgentCode
                        );

                if (transactionAgent == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message =
                            "Finance log agent was not found."
                    });
                }

                if (player.CurrentBalance < amount)
                {
                    return Conflict(new
                    {
                        success = false,
                        message =
                            "Player does not have enough CurrentBalance."
                    });
                }

                var playerBeforeBalance =
                    player.CurrentBalance;

                var agentBeforeBalance =
                    transactionAgent.Balance;

                player.CurrentBalance =
                    playerBeforeBalance - amount;

                transactionAgent.Balance =
                    agentBeforeBalance + amount;

                financeLog.TargetBeforeBalance =
                    playerBeforeBalance;

                financeLog.TargetAfterBalance =
                    player.CurrentBalance;

                financeLog.AgentBeforeBalance =
                    agentBeforeBalance;

                financeLog.AgentAfterBalance =
                    transactionAgent.Balance;

                financeLog.Status =
                    2;

                player.UpdatedAt =
                    DateTime.UtcNow;

                await _dbStorage.Context
                    .SaveChangesAsync();

                await transaction.CommitAsync();

                return Json(new
                {
                    success = true,
                    message = "Self Withdraw completed.",
                    id = financeLog.Id,
                    status = financeLog.Status,
                    targetCode = player.UserCode,
                    agentCode = transactionAgent.AgentCode,
                    amount = amount,
                    playerBeforeBalance = playerBeforeBalance,
                    playerAfterBalance = player.CurrentBalance,
                    agentBeforeBalance = agentBeforeBalance,
                    agentAfterBalance = transactionAgent.Balance
                });
            }
            catch (Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync();
                }
                catch
                {
                    // Ignore rollback failure.
                }

                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message =
                            "An unexpected error occurred while completing the transaction."
                    }
                );
            }
        }

        [HttpPost]
        public async Task<IActionResult> RejectFinanceLog(int id)
        {
            await using var transaction =
                await _dbStorage.Context.Database
                    .BeginTransactionAsync(
                        System.Data.IsolationLevel.Serializable
                    );

            try
            {
                var loggedInAgent =
                    await ResolveFinanceLogAgentAsync();

                if (loggedInAgent == null)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Agent not found."
                    });
                }

                var financeLogQuery =
                    _dbStorage.Context.FinanceLogs
                        .AsQueryable();

                financeLogQuery =
                    ApplyFinanceLogAgentScope(
                        financeLogQuery,
                        loggedInAgent
                    );

                var financeLog =
                    await financeLogQuery
                        .FirstOrDefaultAsync(
                            x => x.Id == id
                        );

                if (financeLog == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Finance log not found."
                    });
                }

                if (financeLog.FinanceType != 2)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "Only Self Withdraw can be rejected."
                    });
                }

                if (financeLog.Status != 0)
                {
                    return Conflict(new
                    {
                        success = false,
                        message =
                            "This transaction is no longer Pending."
                    });
                }

                if (financeLog.TargetAccountType != 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "Self Withdraw target is not a Player."
                    });
                }

                financeLog.Status =
                    3;

                await _dbStorage.Context
                    .SaveChangesAsync();

                await transaction.CommitAsync();

                return Json(new
                {
                    success = true,
                    message = "Self Withdraw rejected.",
                    id = financeLog.Id,
                    status = financeLog.Status
                });
            }
            catch (Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync();
                }
                catch
                {
                    // Ignore rollback failure.
                }

                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message =
                            "An unexpected error occurred while rejecting the transaction."
                    }
                );
            }
        }

        // =============================================================
        // RESOLVE CURRENT AGENT
        //
        // New project:
        // Agent only.
        // No SubAccount resolution.
        // =============================================================

        private async Task<Agent?> ResolveFinanceLogAgentAsync()
        {
            string? loginName =
                User.Identity?.Name;


            if (string.IsNullOrWhiteSpace(loginName))
            {
                return null;
            }


            Agent? agent =
                await _dbStorage.Context.Agents
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x =>
                            x.AgentCode == loginName
                    );


            if (agent == null)
            {
                agent =
                    await _dbStorage.Context.Agents
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x =>
                                x.AgentLoginName == loginName
                        );
            }


            return agent;
        }


        // =============================================================
        // APPLY AGENT HIERARCHY SECURITY
        //
        // Example:
        //
        // one
        // ├── agentA
        // │   ├── agentA1
        // │   └── agentA2
        // └── agentB
        //
        // Root:
        // AgentPath = "."
        // AgentId   = 1
        //
        // Accessible descendant prefix:
        //
        // ".1."
        // =============================================================

        private static IQueryable<FinanceLog> ApplyFinanceLogAgentScope(IQueryable<FinanceLog> query, Agent loggedInAgent)
        {
            if (
                string.IsNullOrWhiteSpace(
                    loggedInAgent.AgentPath
                )
            )
            {
                // Safe fallback:
                // only current agent.
                return query.Where(
                    x =>
                        x.AgentTableId ==
                        loggedInAgent.Id
                );
            }


            string accessiblePathPrefix =
                loggedInAgent.AgentPath +
                loggedInAgent.Id +
                ".";


            return query.Where(
                x =>
                    x.AgentTableId ==
                    loggedInAgent.Id
                    ||
                    x.AgentPath.StartsWith(
                        accessiblePathPrefix
                    )
            );
        }

    }
}
