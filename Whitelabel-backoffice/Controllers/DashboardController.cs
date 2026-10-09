using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Whitelabel_backoffice.Models.ViewModels;
using Whitelabel_backoffice.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Whitelabel_backoffice.Models;
using Whitelabel_backoffice.Services.Interfaces;
using System.Globalization;
using Azure.Core;
using System.Security.Claims;

namespace Whitelabel_backoffice.Controllers
{
    [Authorize]
    public class DashboardController : BaseController
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IWhitelabelDBStorage _dbStorage;
        private readonly IConfiguration _configuration;
        private readonly IGameService _gameService;
        private readonly AppSettings _appSetting;
        public Global.Logging.ILogger _logger;



        public DashboardController(
            IStringLocalizer<SharedResource> lang,
            Global.Logging.ILogger logger,
            IWhitelabelDBStorage dbStorage,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IConfiguration configuration,
            IGameService gameService,
            IOptions<AppSettings> appsettings
        ) : base(lang)
        {
            _logger = logger;
            _dbStorage = dbStorage;
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _gameService = gameService;
            _appSetting = appsettings.Value;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // =========================================================
                // Resolve current agent + timezone offset
                // =========================================================

                var currentAgent = await GetCurrentAgentAsync();

                if (currentAgent == null)
                {
                    return Unauthorized();
                }

                var offset = TryParseOffset(currentAgent.TimeZone, out var parsedOffset)
                    ? parsedOffset
                    : TimeSpan.Zero;


                // =========================================================
                // Compute "today" boundaries in the agent's local timezone,
                // converted to UTC for querying (CreatedAt is stored UTC)
                // =========================================================

                var nowLocal = DateTime.UtcNow + offset;

                var todayLocalStart = nowLocal.Date;

                var todayLocalEnd = todayLocalStart.AddDays(1);

                var startUtc = todayLocalStart - offset;

                var endUtc = todayLocalEnd - offset;


                // =========================================================
                // Downline scope: this agent's own records + everything
                // under their AgentPath (same pattern as GetBettingLogs)
                // =========================================================

                var agentPathPrefix = currentAgent.AgentPath + currentAgent.Id + ".";


                // =========================================================
                // Betting logs — total bet amount / total win amount
                // Status == 1 (Success) only
                // =========================================================

                var bettingQuery = _dbStorage.Context.BettingLogs
                    .AsNoTracking()
                    .Where(x =>
                        x.Status == 1 &&
                        x.CreatedAt >= startUtc &&
                        x.CreatedAt < endUtc &&
                        (
                            x.AgentPath.StartsWith(agentPathPrefix) ||
                            x.AgentCode == currentAgent.AgentCode
                        ));

                // BetAmount is stored negative (see insert code), negate for display
                var totalBetAmount =
                    -(await bettingQuery.SumAsync(x => (decimal?)x.BetAmount) ?? 0m);

                var totalWinAmount =
                    await bettingQuery.SumAsync(x => (decimal?)x.WinAmount) ?? 0m;


                // =========================================================
                // Finance logs — total deposit / total withdraw
                // Status == 2 (Completed) only
                //
                // FinanceType: 0 = Self Deposit, 1 = Manual Deposit,
                //              2 = Self Withdraw, 3 = Manual Withdraw
                // =========================================================

                var depositTypes = new byte[] { 0, 1 };

                var withdrawTypes = new byte[] { 2, 3 };

                var financeQuery = _dbStorage.Context.FinanceLogs
                    .AsNoTracking()
                    .Where(x =>
                        x.Status == 2 &&
                        x.CreatedAt >= startUtc &&
                        x.CreatedAt < endUtc &&
                        (
                            x.TargetPath.StartsWith(agentPathPrefix) ||
                            x.TargetCode == currentAgent.AgentCode
                        ));

                var totalDepositAmount =
                    await financeQuery
                        .Where(x => depositTypes.Contains(x.FinanceType))
                        .SumAsync(x => (decimal?)x.Amount) ?? 0m;

                var totalWithdrawAmount =
                    await financeQuery
                        .Where(x => withdrawTypes.Contains(x.FinanceType))
                        .SumAsync(x => (decimal?)x.Amount) ?? 0m;

                ViewData["TotalBetAmount"] = totalBetAmount;
                ViewData["TotalWinAmount"] = totalWinAmount;
                ViewData["TotalDepositAmount"] = totalDepositAmount;
                ViewData["TotalWithdrawAmount"] = totalWithdrawAmount;

                // =========================================================
                // Response
                // =========================================================

                return View();
            }
            catch (Exception ex)
            {
                _logger.Error($"GetTodaySummary: {ex}");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { error = "Failed to load today's summary." });
            }
        }


        // =========================================================
        // Resolves the currently logged-in Agent entity.
        // =========================================================

        private async Task<Agent?> GetCurrentAgentAsync()
        {
            var aspNetUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(aspNetUserId))
            {
                return null;
            }

            return await _dbStorage.Context.Agents
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AspNetUserId == aspNetUserId);
        }


        // =========================================================
        // Parses offset strings like "-10:00" or "+07:00" into a TimeSpan.
        // =========================================================

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


        public IActionResult BettingLogs()
        {
            return View("BettingLogs");
        }

        public IActionResult Finance()
        {
            return View("Finance");
        }

        public IActionResult StatisticsLogs()
        {
            return View("StatisticsLogs");
        }


        // ========================================================================================
        // ========================================================================================

        // Related Request & Response Controller 

        // =========================================================================================
        // =========================================================================================



        [HttpGet]
        public async Task<IActionResult> GetBettingChart()
        {
            var endDate = DateTime.Today.AddDays(1);

            var startDate = DateTime.Today.AddDays(-29);

            var data = await _dbStorage.Context.BettingLogs
                .AsNoTracking()
                .Where(x =>
                    x.CreatedAt >= startDate &&
                    x.CreatedAt < endDate &&
                    x.Status == 1)
                .GroupBy(x => x.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,

                    BetAmount = g.Sum(x => x.BetAmount),

                    WinAmount = g.Sum(x => x.WinAmount)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();


            // Generate all 30 days
            var result = Enumerable
                .Range(0, 30)
                .Select(i =>
                {
                    var date = startDate.AddDays(i);

                    var item = data.FirstOrDefault(
                        x => x.Date == date.Date);


                    var betAmount =
                        item?.BetAmount ?? 0;

                    var winAmount =
                        item?.WinAmount ?? 0;


                    return new BettingChartItem
                    {
                        Label = date.ToString("MMM dd"),

                        BetAmount = betAmount,

                        WinAmount = winAmount,

                        Ggr = betAmount - winAmount
                    };
                })
                .ToList();


            return Json(result);
        }

        // =========================================================
        // PROVIDER DROPDOWN
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> GetProviderCodes()
        {
            try
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

                return Json(providers);
            }
            catch (Exception ex)
            {
                _logger.Error(
                    ex,
                    "Error loading provider codes."
                );

                return Json(new List<string>());
            }
        }


        // =========================================================
        // BETTING LOGS - SERVER SIDE DATATABLE
        // =========================================================

        [HttpPost]
        public async Task<IActionResult> GetBettingLogs()
        {
            try
            {

                // -------------------------------------------------
                // DATATABLE PAGINATION
                // -------------------------------------------------

                string? draw =
                    Request.Form["draw"]
                        .FirstOrDefault();


                int start =
                    int.TryParse(
                        Request.Form["start"]
                            .FirstOrDefault(),
                        out var parsedStart
                    )
                    ? parsedStart
                    : 0;


                int length =
                    int.TryParse(
                        Request.Form["length"]
                            .FirstOrDefault(),
                        out var parsedLength
                    )
                    ? parsedLength
                    : 25;


                if (start < 0)
                {
                    start = 0;
                }


                if (length <= 0)
                {
                    length = 25;
                }





                // -------------------------------------------------
                // SORTING
                // -------------------------------------------------

                int sortColumn =
                    int.TryParse(
                        Request.Form["order[0][column]"]
                            .FirstOrDefault(),
                        out var parsedSortColumn
                    )
                    ? parsedSortColumn
                    : 17;


                string sortDirection =
                    Request.Form["order[0][dir]"]
                        .FirstOrDefault()
                    ?? "desc";


                bool descending =
                    sortDirection.Equals(
                        "desc",
                        StringComparison.OrdinalIgnoreCase
                    );






                // -------------------------------------------------
                // FILTERS
                // -------------------------------------------------

                string? agent =
                    Request.Form["agent"]
                        .FirstOrDefault();


                string? user =
                    Request.Form["user"]
                        .FirstOrDefault();


                string? providerCode =
                    Request.Form["providerCode"]
                        .FirstOrDefault();


                string? gameCode =
                    Request.Form["gameCode"]
                        .FirstOrDefault();


                string? roundId =
                    Request.Form["roundId"]
                        .FirstOrDefault();


                string? statusValue =
                    Request.Form["status"]
                        .FirstOrDefault();


                string? fromDate =
                    Request.Form["fromDate"]
                        .FirstOrDefault();


                string? toDate =
                    Request.Form["toDate"]
                        .FirstOrDefault();







                // -------------------------------------------------
                // AGENT TIMEZONE
                // -------------------------------------------------

                TimeSpan agentOffset =
                    TimeSpan.Zero;


                string? agentLoginName =
                    User.Identity?.Name;


                Agent? currentAgent = null;


                if (!string.IsNullOrWhiteSpace(agentLoginName))
                {

                    currentAgent =
                        await _dbStorage.Context.Agents
                            .AsNoTracking()
                            .FirstOrDefaultAsync(
                                x =>
                                    x.AgentCode == agentLoginName
                            );


                    if (currentAgent == null)
                    {

                        currentAgent =
                            await _dbStorage.Context.Agents
                                .AsNoTracking()
                                .FirstOrDefaultAsync(
                                    x =>
                                        x.AgentLoginName == agentLoginName
                                );

                    }

                }



                // Database example:
                // +09:00

                if (currentAgent != null && !string.IsNullOrWhiteSpace(currentAgent.TimeZone))
                {
                    string savedTimezone = currentAgent.TimeZone.Trim();

                    if (savedTimezone.StartsWith('+'))
                        savedTimezone = savedTimezone.Substring(1);

                    if (TimeSpan.TryParse(savedTimezone, CultureInfo.InvariantCulture, out TimeSpan parsedOffset))
                    {
                        agentOffset = parsedOffset;
                    }
                }




                // -------------------------------------------------
                // BASE QUERY
                // -------------------------------------------------

                IQueryable<BettingLog> query =
                    _dbStorage.Context.BettingLogs
                        .AsNoTracking();





                // -------------------------------------------------
                // DATE RANGE
                // -------------------------------------------------

                DateTime agentNow =
                    DateTime.UtcNow
                        .Add(agentOffset);


                // Default:
                // last 1 month -> now
                DateTime fromLocal =
                    agentNow.AddMonths(-1);


                DateTime toLocal =
                    agentNow;



                // -----------------------------------------------
                // FROM DATE
                // -----------------------------------------------

                if (
                    DateTime.TryParse(
                        fromDate,
                        out DateTime parsedFrom
                    )
                )
                {
                    fromLocal = parsedFrom;
                }



                // -----------------------------------------------
                // TO DATE
                // -----------------------------------------------

                if (
                    DateTime.TryParse(
                        toDate,
                        out DateTime parsedTo
                    )
                )
                {
                    toLocal =
                        parsedTo;
                }



                // IMPORTANT
                // The received date has NO timezone.
                // Treat it as agent local time.

                fromLocal =
                    DateTime.SpecifyKind(
                        fromLocal,
                        DateTimeKind.Unspecified
                    );


                toLocal =
                    DateTime.SpecifyKind(
                        toLocal,
                        DateTimeKind.Unspecified
                    );



                // Convert agent local time -> UTC
                DateTime fromUtc =
                    fromLocal
                        .Subtract(agentOffset);


                DateTime toUtc =
                    toLocal
                        .Subtract(agentOffset);


                // Safety
                if (fromUtc > toUtc)
                {
                    DateTime temp =
                        fromUtc;

                    fromUtc =
                        toUtc;

                    toUtc =
                        temp;
                }



                // Database stores UTC
                query =
                    query.Where(
                        x =>
                            x.CreatedAt >= fromUtc &&
                            x.CreatedAt <= toUtc
                    );
                // -------------------------------------------------
                // AGENT CODE
                // -------------------------------------------------

                if (!string.IsNullOrWhiteSpace(agent))
                {

                    agent =
                        agent.Trim();


                    query =
                        query.Where(
                            x =>
                                x.AgentCode.Contains(agent)
                        );

                }







                // -------------------------------------------------
                // USER CODE
                // -------------------------------------------------

                if (!string.IsNullOrWhiteSpace(user))
                {

                    user =
                        user.Trim();


                    query =
                        query.Where(
                            x =>
                                x.UserCode.Contains(user)
                        );

                }







                // -------------------------------------------------
                // PROVIDER CODE
                // -------------------------------------------------

                if (!string.IsNullOrWhiteSpace(providerCode))
                {

                    providerCode =
                        providerCode.Trim();


                    query =
                        query.Where(
                            x =>
                                x.ProviderCode == providerCode
                        );

                }







                // -------------------------------------------------
                // GAME CODE
                // -------------------------------------------------

                if (!string.IsNullOrWhiteSpace(gameCode))
                {

                    gameCode =
                        gameCode.Trim();


                    query =
                        query.Where(
                            x =>
                                x.GameCode.Contains(gameCode)
                        );

                }







                // -------------------------------------------------
                // ROUND ID
                // -------------------------------------------------

                if (!string.IsNullOrWhiteSpace(roundId))
                {

                    roundId =
                        roundId.Trim();


                    query =
                        query.Where(
                            x =>
                                x.RoundId.Contains(roundId)
                        );

                }







                // -------------------------------------------------
                // STATUS
                //
                // 0 = Canceled
                // 1 = Success
                // 2 = Outstanding
                // -------------------------------------------------

                if (!string.IsNullOrWhiteSpace(statusValue))
                {

                    if (
                        byte.TryParse(
                            statusValue,
                            out byte status
                        )
                    )
                    {

                        query =
                            query.Where(
                                x =>
                                    x.Status == status
                            );

                    }

                }







                // -------------------------------------------------
                // TOTAL RECORDS
                // -------------------------------------------------

                int totalRecords =
                    await _dbStorage.Context.BettingLogs
                        .CountAsync();







                // -------------------------------------------------
                // FILTERED RECORDS
                // -------------------------------------------------

                int filteredRecords =
                    await query.CountAsync();
                // -------------------------------------------------
                // SERVER-SIDE SORTING
                // -------------------------------------------------

                query =
                    sortColumn switch
                    {

                        0 =>
                            descending
                                ? query.OrderByDescending(x => x.Id)
                                : query.OrderBy(x => x.Id),


                        1 =>
                            descending
                                ? query.OrderByDescending(x => x.UserTableId)
                                : query.OrderBy(x => x.UserTableId),


                        2 =>
                            descending
                                ? query.OrderByDescending(x => x.UserCode)
                                : query.OrderBy(x => x.UserCode),


                        3 =>
                            descending
                                ? query.OrderByDescending(x => x.AgentTableId)
                                : query.OrderBy(x => x.AgentTableId),


                        4 =>
                            descending
                                ? query.OrderByDescending(x => x.AgentCode)
                                : query.OrderBy(x => x.AgentCode),


                        5 =>
                            descending
                                ? query.OrderByDescending(x => x.AgentPath)
                                : query.OrderBy(x => x.AgentPath),


                        6 =>
                            descending
                                ? query.OrderByDescending(x => x.ProviderId)
                                : query.OrderBy(x => x.ProviderId),


                        7 =>
                            descending
                                ? query.OrderByDescending(x => x.ProviderName)
                                : query.OrderBy(x => x.ProviderName),


                        8 =>
                            descending
                                ? query.OrderByDescending(x => x.ProviderCode)
                                : query.OrderBy(x => x.ProviderCode),


                        9 =>
                            descending
                                ? query.OrderByDescending(x => x.GameName)
                                : query.OrderBy(x => x.GameName),


                        10 =>
                            descending
                                ? query.OrderByDescending(x => x.GameCode)
                                : query.OrderBy(x => x.GameCode),


                        11 =>
                            descending
                                ? query.OrderByDescending(x => x.RoundId)
                                : query.OrderBy(x => x.RoundId),


                        12 =>
                            descending
                                ? query.OrderByDescending(x => x.BetAmount)
                                : query.OrderBy(x => x.BetAmount),


                        13 =>
                            descending
                                ? query.OrderByDescending(x => x.WinAmount)
                                : query.OrderBy(x => x.WinAmount),


                        14 =>
                            descending
                                ? query.OrderByDescending(x => x.BeforeBalance)
                                : query.OrderBy(x => x.BeforeBalance),


                        15 =>
                            descending
                                ? query.OrderByDescending(x => x.AfterBalance)
                                : query.OrderBy(x => x.AfterBalance),


                        16 =>
                            descending
                                ? query.OrderByDescending(x => x.Status)
                                : query.OrderBy(x => x.Status),


                        17 =>
                            descending
                                ? query.OrderByDescending(x => x.CreatedAt)
                                : query.OrderBy(x => x.CreatedAt),


                        18 =>
                            descending
                                ? query.OrderByDescending(x => x.UpdatedAt)
                                : query.OrderBy(x => x.UpdatedAt),


                        19 =>
                            descending
                                ? query.OrderByDescending(x => x.HistoryId)
                                : query.OrderBy(x => x.HistoryId),


                        _ =>
                            query.OrderByDescending(
                                x => x.CreatedAt
                            )

                    };







                // -------------------------------------------------
                // PAGINATION + PROJECTION
                // -------------------------------------------------

                var data =
                    await query

                        .Skip(start)

                        .Take(length)

                        .Select(
                            x => new
                            {

                                x.Id,

                                x.UserTableId,

                                x.UserCode,

                                x.AgentTableId,

                                x.AgentCode,

                                x.AgentPath,

                                x.ProviderId,

                                x.ProviderName,

                                x.ProviderCode,

                                x.GameName,

                                x.GameCode,

                                x.RoundId,

                                x.BetAmount,

                                x.WinAmount,

                                x.BeforeBalance,

                                x.AfterBalance,

                                x.Status,

                                x.CreatedAt,

                                x.UpdatedAt,

                                x.HistoryId

                            }

                        )

                        .ToListAsync();







                // -------------------------------------------------
                // CONVERT UTC -> AGENT TIMEZONE
                // -------------------------------------------------

                var convertedData =
                    data.Select(
                        x =>
                        {

                            DateTime createdUtc =
                                DateTime.SpecifyKind(
                                    x.CreatedAt,
                                    DateTimeKind.Utc
                                );


                            DateTime updatedUtc =
                                DateTime.SpecifyKind(
                                    x.UpdatedAt,
                                    DateTimeKind.Utc
                                );

                            DateTime createdLocal =
                                createdUtc
                                    .Add(agentOffset);


                            DateTime updatedLocal =
                                updatedUtc
                                    .Add(agentOffset);



                            return new
                            {

                                x.Id,

                                x.UserTableId,

                                x.UserCode,

                                x.AgentTableId,

                                x.AgentCode,

                                x.AgentPath,

                                x.ProviderId,

                                x.ProviderName,

                                x.ProviderCode,

                                x.GameName,

                                x.GameCode,

                                x.RoundId,

                                x.BetAmount,

                                x.WinAmount,

                                x.BeforeBalance,

                                x.AfterBalance,

                                x.Status,


                                CreatedAt =
                                    createdLocal.ToString(
                                        "yyyy-MM-dd HH:mm:ss"
                                    ),


                                UpdatedAt =
                                    updatedLocal.ToString(
                                        "yyyy-MM-dd HH:mm:ss"
                                    ),


                                x.HistoryId

                            };

                        }
                    )
                    .ToList();








                // -------------------------------------------------
                // RESPONSE
                // -------------------------------------------------

                return Json(
                    new
                    {

                        draw = draw,

                        recordsTotal =
                            totalRecords,

                        recordsFiltered =
                            filteredRecords,

                        data =
                            convertedData

                    }
                );

            }
            catch (Exception ex)
            {

                _logger.Error(
                    ex,
                    "Error loading betting logs."
                );


                return Json(
                    new
                    {

                        draw =
                            Request.Form["draw"]
                                .FirstOrDefault(),


                        recordsTotal =
                            0,


                        recordsFiltered =
                            0,


                        data =
                            new List<object>(),


                        error =
                            "Failed to load betting logs."

                    }
                );

            }
        }



        // =============================================================
        // GET FINANCE LOGS
        // =============================================================

        [HttpPost]
        public async Task<IActionResult> GetFinanceLogs()
        {
            try
            {
                // =========================================================
                // DADOS / DATATABLE PARAMETERS
                // =========================================================

                int.TryParse(
                    Request.Form["draw"],
                    out var draw
                );

                int.TryParse(
                    Request.Form["start"],
                    out var start
                );

                int.TryParse(
                    Request.Form["length"],
                    out var length
                );

                int.TryParse(
                    Request.Form["order[0][column]"],
                    out var orderColumn
                );


                var orderDir =
                    Request.Form["order[0][dir]"]
                        .ToString()
                        .ToLowerInvariant();


                if (start < 0)
                {
                    start = 0;
                }


                if (length <= 0)
                {
                    length = 25;
                }


                if (length > 100)
                {
                    length = 100;
                }


                // =========================================================
                // FILTER VALUES
                // =========================================================

                var agentCode =
                    Request.Form["agentCode"]
                        .ToString()
                        .Trim();


                var targetCode =
                    Request.Form["targetCode"]
                        .ToString()
                        .Trim();


                var financeTypeText =
                    Request.Form["financeType"]
                        .ToString()
                        .Trim();


                var amountText =
                    Request.Form["amount"]
                        .ToString()
                        .Trim();


                var orderId =
                    Request.Form["orderId"]
                        .ToString()
                        .Trim();


                var statusText =
                    Request.Form["status"]
                        .ToString()
                        .Trim();


                var fromDateText =
                    Request.Form["fromDate"]
                        .ToString()
                        .Trim();


                var toDateText =
                    Request.Form["toDate"]
                        .ToString()
                        .Trim();


                // =========================================================
                // CURRENT LOGGED-IN AGENT
                // =========================================================

                var loggedInAgent =
                    await ResolveFinanceLogAgentAsync();


                if (loggedInAgent == null)
                {
                    return Unauthorized(
                        new
                        {
                            success = false,
                            message = "Agent not found."
                        }
                    );
                }


                // =========================================================
                // AGENT TIMEZONE
                //
                // Example:
                // +09:00
                // =========================================================

                TimeSpan agentOffset = TimeSpan.Zero;

                string? tz = loggedInAgent.TimeZone?.Trim();

                if (!string.IsNullOrEmpty(tz))
                {
                    bool isNegative = tz.StartsWith('-');
                    string digits = tz.TrimStart('+', '-');

                    if (TimeSpan.TryParseExact(digits, @"hh\:mm", CultureInfo.InvariantCulture, out TimeSpan parsed))
                    {
                        agentOffset = isNegative ? parsed.Negate() : parsed;
                    }
                    // else: invalid format, stays TimeSpan.Zero (consider logging tz here)
                }


                // =========================================================
                // BASE QUERY
                //
                // IMPORTANT:
                // Security scope is applied BEFORE all user filters.
                // =========================================================

                IQueryable<FinanceLog> query =
                    _dbStorage.Context.FinanceLogs
                        .AsNoTracking();


                query =
                    ApplyFinanceLogAgentScope(
                        query,
                        loggedInAgent
                    );


                // =========================================================
                // TOTAL ACCESSIBLE RECORDS
                //
                // This is the count before user's filters.
                // =========================================================

                var totalRecords =
                    await query.CountAsync();


                // =========================================================
                // AGENT CODE FILTER
                // =========================================================

                if (!string.IsNullOrWhiteSpace(agentCode))
                {
                    query =
                        query.Where(
                            x =>
                                x.AgentCode.Contains(
                                    agentCode
                                )
                        );
                }


                // =========================================================
                // TARGET CODE FILTER
                // =========================================================

                if (!string.IsNullOrWhiteSpace(targetCode))
                {
                    query =
                        query.Where(
                            x =>
                                x.TargetCode.Contains(
                                    targetCode
                                )
                        );
                }


                // =========================================================
                // FINANCE TYPE FILTER
                // =========================================================

                if (
                    byte.TryParse(
                        financeTypeText,
                        out var financeTypeValue
                    )
                )
                {
                    query =
                        query.Where(
                            x =>
                                x.FinanceType ==
                                financeTypeValue
                        );
                }


                // =========================================================
                // AMOUNT FILTER
                //
                // Exact numeric match.
                // =========================================================

                if (
                    decimal.TryParse(
                        amountText,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var amountValue
                    )
                )
                {
                    query =
                        query.Where(
                            x =>
                                x.Amount ==
                                amountValue
                        );
                }


                // =========================================================
                // ORDER ID FILTER
                // =========================================================

                if (!string.IsNullOrWhiteSpace(orderId))
                {
                    query =
                        query.Where(
                            x =>
                                x.OrderId.Contains(
                                    orderId
                                )
                        );
                }


                // =========================================================
                // STATUS FILTER
                //
                // Status 1 is NOT removed from database logic.
                // The UI simply does not display/use it.
                // =========================================================

                if (
                    byte.TryParse(
                        statusText,
                        out var statusValue
                    )
                )
                {
                    query =
                        query.Where(
                            x =>
                                x.Status ==
                                statusValue
                        );
                }


                // =========================================================
                // DATE RANGE
                //
                // Incoming dates are AGENT LOCAL time.
                // Database stores UTC.
                //
                // Format:
                // yyyy-MM-dd HH:mm:ss
                // =========================================================

                DateTime fromUtc;
                DateTime toUtcExclusive;


                // ---------------------------------------------------------
                // FROM DATE
                // ---------------------------------------------------------

                if (
                    DateTime.TryParseExact(
                        fromDateText,
                        "yyyy-MM-dd HH:mm:ss",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out var fromLocal
                    )
                )
                {
                    fromLocal =
                        DateTime.SpecifyKind(
                            fromLocal,
                            DateTimeKind.Unspecified
                        );


                    fromUtc =
                        fromLocal
                            .Subtract(
                                agentOffset
                            );
                }
                else
                {
                    // Default:
                    // Last month -> now
                    var agentLocalNow =
                        DateTime.UtcNow
                            .Add(
                                agentOffset
                            );


                    var defaultFromLocal =
                        agentLocalNow
                            .AddMonths(-1);


                    fromUtc =
                        defaultFromLocal
                            .Subtract(
                                agentOffset
                            );
                }


                // ---------------------------------------------------------
                // TO DATE
                //
                // Add one second so:
                //
                // 2026-09-17 23:59:59
                //
                // includes DB values with fractional seconds such as:
                //
                // 2026-09-17 23:59:59.888
                // ---------------------------------------------------------

                if (
                    DateTime.TryParseExact(
                        toDateText,
                        "yyyy-MM-dd HH:mm:ss",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out var toLocal
                    )
                )
                {
                    toLocal =
                        DateTime.SpecifyKind(
                            toLocal,
                            DateTimeKind.Unspecified
                        );


                    toUtcExclusive =
                        toLocal
                            .AddSeconds(1)
                            .Subtract(
                                agentOffset
                            );
                }
                else
                {
                    var agentLocalNow =
                        DateTime.UtcNow
                            .Add(
                                agentOffset
                            );


                    toUtcExclusive =
                        agentLocalNow
                            .AddSeconds(1)
                            .Subtract(
                                agentOffset
                            );
                }


                // =========================================================
                // APPLY DATE FILTER
                // =========================================================

                query =
                    query.Where(
                        x =>
                            x.CreatedAt >=
                            fromUtc

                            &&

                            x.CreatedAt <
                            toUtcExclusive
                    );


                // =========================================================
                // FILTERED RECORD COUNT
                // =========================================================

                var filteredRecords =
                    await query.CountAsync();


                // =========================================================
                // SERVER-SIDE SORTING
                //
                // CURRENT TABLE:
                //
                // 0  #
                // 1  Order Id
                // 2  Target Code
                // 3  Agent Code
                // 4  Finance Type
                // 5  Amount
                // 6  Balance
                // 7  Date
                // 8  Status
                // 9  Details
                // 10 Actions
                //
                // Only 1-8 are sortable.
                // =========================================================

                if (
                    orderDir != "asc"
                    &&
                    orderDir != "desc"
                )
                {
                    orderDir =
                        "desc";
                }


                switch (orderColumn)
                {
                    // -----------------------------------------------------
                    // 1. ORDER ID
                    // -----------------------------------------------------

                    case 1:

                        query =
                            orderDir == "asc"
                                ? query.OrderBy(
                                    x => x.OrderId
                                )
                                : query.OrderByDescending(
                                    x => x.OrderId
                                );

                        break;


                    // -----------------------------------------------------
                    // 2. TARGET CODE
                    // -----------------------------------------------------

                    case 2:

                        query =
                            orderDir == "asc"
                                ? query.OrderBy(
                                    x => x.TargetCode
                                )
                                : query.OrderByDescending(
                                    x => x.TargetCode
                                );

                        break;


                    // -----------------------------------------------------
                    // 3. AGENT CODE
                    // -----------------------------------------------------

                    case 3:

                        query =
                            orderDir == "asc"
                                ? query.OrderBy(
                                    x => x.AgentCode
                                )
                                : query.OrderByDescending(
                                    x => x.AgentCode
                                );

                        break;


                    // -----------------------------------------------------
                    // 4. FINANCE TYPE
                    // -----------------------------------------------------

                    case 4:

                        query =
                            orderDir == "asc"
                                ? query.OrderBy(
                                    x => x.FinanceType
                                )
                                : query.OrderByDescending(
                                    x => x.FinanceType
                                );

                        break;


                    // -----------------------------------------------------
                    // 5. AMOUNT
                    // -----------------------------------------------------

                    case 5:

                        query =
                            orderDir == "asc"
                                ? query.OrderBy(
                                    x => x.Amount
                                )
                                : query.OrderByDescending(
                                    x => x.Amount
                                );

                        break;


                    // -----------------------------------------------------
                    // 6. BALANCE
                    //
                    // Balance column displays:
                    // TargetBeforeBalance -> TargetAfterBalance
                    //
                    // Sort by final balance.
                    // -----------------------------------------------------

                    case 6:

                        query =
                            orderDir == "asc"
                                ? query.OrderBy(
                                    x => x.TargetAfterBalance
                                )
                                : query.OrderByDescending(
                                    x => x.TargetAfterBalance
                                );

                        break;


                    // -----------------------------------------------------
                    // 7. DATE
                    // -----------------------------------------------------

                    case 7:

                        query =
                            orderDir == "asc"
                                ? query.OrderBy(
                                    x => x.CreatedAt
                                )
                                : query.OrderByDescending(
                                    x => x.CreatedAt
                                );

                        break;


                    // -----------------------------------------------------
                    // 8. STATUS
                    // -----------------------------------------------------

                    case 8:

                        query =
                            orderDir == "asc"
                                ? query.OrderBy(
                                    x => x.Status
                                )
                                : query.OrderByDescending(
                                    x => x.Status
                                );

                        break;


                    // -----------------------------------------------------
                    // 0 # / 9 DETAILS / 10 ACTIONS
                    //
                    // Not sortable.
                    //
                    // Default to newest first.
                    // -----------------------------------------------------

                    default:

                        query =
                            query.OrderByDescending(
                                x => x.CreatedAt
                            );

                        break;
                }


                // =========================================================
                // PAGING + PROJECTION
                // =========================================================

                var logs =
                    await query
                        .Skip(start)
                        .Take(length)
                        .Select(
                            x => new
                            {
                                id =
                                    x.Id,

                                agentCode =
                                    x.AgentCode,

                                agentPath =
                                    x.AgentPath,

                                agentTableId =
                                    x.AgentTableId,

                                targetCode =
                                    x.TargetCode,

                                targetPath =
                                    x.TargetPath,

                                targetTableId =
                                    x.TargetTableId,

                                targetAccountType =
                                    x.TargetAccountType,

                                financeType =
                                    x.FinanceType,

                                amount =
                                    x.Amount,

                                createdAtUtc =
                                    x.CreatedAt,

                                agentBeforeBalance =
                                    x.AgentBeforeBalance,

                                agentAfterBalance =
                                    x.AgentAfterBalance,

                                targetBeforeBalance =
                                    x.TargetBeforeBalance,

                                targetAfterBalance =
                                    x.TargetAfterBalance,

                                status =
                                    x.Status,

                                orderId =
                                    x.OrderId
                            }
                        )
                        .ToListAsync();


                // =========================================================
                // CONVERT CREATED AT:
                //
                // UTC -> LOGGED-IN AGENT LOCAL TIME
                // =========================================================

                var data =
                    logs.Select(
                        x => new
                        {
                            id =
                                x.id,

                            agentCode =
                                x.agentCode,

                            agentPath =
                                x.agentPath,

                            agentTableId =
                                x.agentTableId,

                            targetCode =
                                x.targetCode,

                            targetPath =
                                x.targetPath,

                            targetTableId =
                                x.targetTableId,

                            targetAccountType =
                                x.targetAccountType,

                            financeType =
                                x.financeType,

                            amount =
                                x.amount,

                            createdAt =
                                x.createdAtUtc
                                    .Add(
                                        agentOffset
                                    )
                                    .ToString(
                                        "yyyy-MM-dd HH:mm:ss"
                                    ),

                            agentBeforeBalance =
                                x.agentBeforeBalance,

                            agentAfterBalance =
                                x.agentAfterBalance,

                            targetBeforeBalance =
                                x.targetBeforeBalance,

                            targetAfterBalance =
                                x.targetAfterBalance,

                            status =
                                x.status,

                            orderId =
                                x.orderId
                        }
                    )
                    .ToList();


                // =========================================================
                // DATATABLE RESPONSE
                // =========================================================

                return Json(
                    new
                    {
                        draw =
                            draw,

                        recordsTotal =
                            totalRecords,

                        recordsFiltered =
                            filteredRecords,

                        data =
                            data
                    }
                );
            }
            catch (Exception ex)
            {
                // =========================================================
                // ERROR
                // =========================================================

                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message =
                            ex.Message
                    }
                );
            }
        }


        // =============================================================
        // APPROVE SELF WITHDRAW
        //
        // Only:
        //     FinanceType = 2  (Self Withdraw)
        //     Status      = 0  (Pending)
        //
        // Result:
        //     Status 0 -> Status 1
        //
        // This action does NOT directly change balances.
        // =============================================================

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
                // ---------------------------------------------------------
                // LOGGED-IN AGENT
                // ---------------------------------------------------------

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


                // ---------------------------------------------------------
                // FINANCE LOG + AGENT SECURITY SCOPE
                // ---------------------------------------------------------

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


                // ---------------------------------------------------------
                // SELF WITHDRAW ONLY
                // ---------------------------------------------------------

                if (financeLog.FinanceType != 2)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "Only Self Withdraw can be completed."
                    });
                }


                // ---------------------------------------------------------
                // PENDING ONLY
                // ---------------------------------------------------------

                if (financeLog.Status != 0)
                {
                    return Conflict(new
                    {
                        success = false,
                        message =
                            "This transaction is no longer Pending."
                    });
                }


                // ---------------------------------------------------------
                // PLAYER TARGET ONLY
                //
                // 0 = Player
                // 1 = Agent
                // ---------------------------------------------------------

                if (financeLog.TargetAccountType != 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "Self Withdraw target is not a Player."
                    });
                }


                // ---------------------------------------------------------
                // AMOUNT
                // ---------------------------------------------------------

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


                // ---------------------------------------------------------
                // PLAYER
                //
                // TargetTableId = User.Id
                // TargetCode    = User.UserCode
                // ---------------------------------------------------------

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


                // ---------------------------------------------------------
                // TRANSACTION AGENT
                //
                // AgentTableId = Agent.Id
                // AgentCode    = Agent.AgentCode
                //
                // IMPORTANT:
                // Do NOT use loggedInAgent here.
                // The finance log may belong to a child agent.
                // ---------------------------------------------------------

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


                // ---------------------------------------------------------
                // PLAYER BALANCE CHECK
                // ---------------------------------------------------------

                if (player.CurrentBalance < amount)
                {
                    return Conflict(new
                    {
                        success = false,
                        message =
                            "Player does not have enough balance."
                    });
                }


                // ---------------------------------------------------------
                // PLAYER BALANCE BEFORE
                // ---------------------------------------------------------

                var playerBeforeBalance =
                    player.CurrentBalance;


                // ---------------------------------------------------------
                // AGENT BALANCE BEFORE
                // ---------------------------------------------------------

                var agentBeforeBalance =
                    transactionAgent.Balance;


                // ---------------------------------------------------------
                // ACTUAL SELF WITHDRAW
                //
                // Player gives the amount back to the agent.
                //
                // Player:
                // CurrentBalance decreases.
                //
                // Agent:
                // Balance increases.
                // ---------------------------------------------------------

                player.CurrentBalance =
                    playerBeforeBalance - amount;


                transactionAgent.Balance =
                    agentBeforeBalance + amount;


                // ---------------------------------------------------------
                // PLAYER WITHDRAW INFORMATION
                // ---------------------------------------------------------

                player.TotalWithdrawAmount += amount;

                if (player.FirstWithdrawAt == null)
                {
                    player.FirstWithdrawAt = DateTime.UtcNow;
                }


                // ---------------------------------------------------------
                // FINANCE LOG PLAYER BALANCE SNAPSHOT
                // ---------------------------------------------------------

                financeLog.TargetBeforeBalance =
                    playerBeforeBalance;


                financeLog.TargetAfterBalance =
                    player.CurrentBalance;


                // ---------------------------------------------------------
                // FINANCE LOG AGENT BALANCE SNAPSHOT
                // ---------------------------------------------------------

                financeLog.AgentBeforeBalance =
                    agentBeforeBalance;


                financeLog.AgentAfterBalance =
                    transactionAgent.Balance;


                // ---------------------------------------------------------
                // STATUS
                //
                // Pending   = 0
                // Completed = 2
                // ---------------------------------------------------------

                financeLog.Status =
                    2;


                // ---------------------------------------------------------
                // PLAYER UPDATED AT
                // ---------------------------------------------------------

                player.UpdatedAt =
                    DateTime.UtcNow;


                // ---------------------------------------------------------
                // SAVE BOTH BALANCES + LOG ATOMICALLY
                // ---------------------------------------------------------

                await _dbStorage.Context
                    .SaveChangesAsync();


                // ---------------------------------------------------------
                // COMMIT
                // ---------------------------------------------------------

                await transaction.CommitAsync();


                // ---------------------------------------------------------
                // SUCCESS
                // ---------------------------------------------------------

                return Json(new
                {
                    success = true,

                    message =
                        "Self Withdraw completed.",

                    id =
                        financeLog.Id,

                    status =
                        financeLog.Status,

                    targetCode =
                        player.UserCode,

                    agentCode =
                        transactionAgent.AgentCode,

                    amount =
                        amount,

                    playerBeforeBalance =
                        playerBeforeBalance,

                    playerAfterBalance =
                        player.CurrentBalance,

                    agentBeforeBalance =
                        agentBeforeBalance,

                    agentAfterBalance =
                        transactionAgent.Balance
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message = ex.Message
                    }
                );
            }
        }


        [HttpPost]
        public async Task<IActionResult> RejectFinanceLog(int id)
        {
            try
            {
                // ---------------------------------------------------------
                // LOGGED-IN AGENT
                // ---------------------------------------------------------

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


                // ---------------------------------------------------------
                // FINANCE LOG + SECURITY SCOPE
                // ---------------------------------------------------------

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


                // ---------------------------------------------------------
                // SELF WITHDRAW ONLY
                // ---------------------------------------------------------

                if (financeLog.FinanceType != 2)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "Only Self Withdraw can be rejected."
                    });
                }


                // ---------------------------------------------------------
                // PENDING ONLY
                // ---------------------------------------------------------

                if (financeLog.Status != 0)
                {
                    return Conflict(new
                    {
                        success = false,
                        message =
                            "This transaction is no longer Pending."
                    });
                }


                // ---------------------------------------------------------
                // PLAYER TARGET ONLY
                // ---------------------------------------------------------

                if (financeLog.TargetAccountType != 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            "Self Withdraw target is not a Player."
                    });
                }


                // ---------------------------------------------------------
                // REJECT
                //
                // No player balance change.
                // ---------------------------------------------------------

                financeLog.Status =
                    3;


                await _dbStorage.Context
                    .SaveChangesAsync();


                return Json(new
                {
                    success = true,

                    message =
                        "Self Withdraw rejected.",

                    id =
                        financeLog.Id,

                    status =
                        financeLog.Status
                });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message = ex.Message
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


        // =============================================================
        // CLIENT DATE PARSER
        // =============================================================

        private static bool TryParseFinanceLogDate(
            string? value,
            out DateTime result
        )
        {
            result =
                default;


            if (
                string.IsNullOrWhiteSpace(
                    value
                )
            )
            {
                return false;
            }


            value =
                value.Trim();


            if (
                DateTime.TryParseExact(
                    value,
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out result
                )
            )
            {
                return true;
            }


            if (
                DateTime.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out result
                )
            )
            {
                return true;
            }


            return false;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAgents()
        {
            var agents = await _dbStorage.Context.FinanceLogs
                .AsNoTracking()
                .Where(x =>
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
        [Route("agent/transaction/financelog/notifications")]
        public async Task<IActionResult> GetFinanceLogNotifications(
        long lastId = 0)
        {
            try
            {
                var notifications = await _dbStorage.Context.FinanceLogs
                    .AsNoTracking()
                    .Where(x =>
                        x.Id > lastId &&
                        x.FinanceType == 2 &&
                        x.Status == 0)
                    .OrderBy(x => x.Id)
                    .Select(x => new
                    {
                        id = x.Id,
                        orderId = x.OrderId,
                        userCode = x.TargetCode,
                        amount = x.Amount,
                        createdAt = x.CreatedAt
                    })
                    .Take(10)
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    notifications
                });
            }
            catch (Exception ex)
            {
                //_logger.Error($"GetFinanceLogNotifications: {ex}");

                return Json(new
                {
                    success = false,
                    notifications = Array.Empty<object>()
                });
            }
        }


        [HttpGet]
        public IActionResult KycVerified()
        {
            ViewData["Title"] = "KYC Verification";
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> GetKycRequestLogs(
            int draw,
            int start,
            int length,
            int? userId,
            byte? status,
            string fromDate,
            string toDate)
        {
            try
            {
                var query =
                    _dbStorage.Context.KycRequestLogs
                        .AsNoTracking()
                        .AsQueryable();


                /* =========================================================
                   USER ID FILTER
                   ========================================================= */

                if (userId.HasValue)
                {
                    query = query.Where(
                        x => x.UserId == userId.Value);
                }


                /* =========================================================
                   STATUS FILTER
                   ========================================================= */

                if (status.HasValue)
                {
                    query = query.Where(
                        x => x.Status == status.Value);
                }


                /* =========================================================
                   AGENT TIME ZONE
                   ========================================================= */

                var agent =
                    await _dbStorage.Context.Agents
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x => x.AgentLoginName ==
                                 User.Identity.Name);


                TimeZoneInfo agentTimeZone =
                    TimeZoneInfo.Utc;


                if (agent != null &&
                    !string.IsNullOrWhiteSpace(agent.TimeZone))
                {
                    try
                    {
                        agentTimeZone =
                            TimeZoneInfo.FindSystemTimeZoneById(
                                agent.TimeZone);
                    }
                    catch
                    {
                        /*
                         * Keep UTC if the configured timezone
                         * cannot be found.
                         */
                    }
                }


                /* =========================================================
                   DATE FILTER

                   The date selected in the UI represents the
                   AGENT'S LOCAL TIME.

                   Convert it to UTC before querying the DB.
                   ========================================================= */

                if (!string.IsNullOrWhiteSpace(fromDate) &&
                    DateTime.TryParse(
                        fromDate,
                        out DateTime localFromDate))
                {
                    var utcFromDate =
                        TimeZoneInfo.ConvertTimeToUtc(
                            DateTime.SpecifyKind(
                                localFromDate,
                                DateTimeKind.Unspecified),
                            agentTimeZone);

                    query = query.Where(
                        x => x.RequestTime >= utcFromDate);
                }


                if (!string.IsNullOrWhiteSpace(toDate) &&
                    DateTime.TryParse(
                        toDate,
                        out DateTime localToDate))
                {
                    var utcToDate =
                        TimeZoneInfo.ConvertTimeToUtc(
                            DateTime.SpecifyKind(
                                localToDate,
                                DateTimeKind.Unspecified),
                            agentTimeZone);

                    query = query.Where(
                        x => x.RequestTime <= utcToDate);
                }


                /* =========================================================
                   TOTAL FILTERED RECORDS
                   ========================================================= */

                var recordsFiltered =
                    await query.CountAsync();


                /* =========================================================
                   ORDER
                   ========================================================= */

                query =
                    query.OrderByDescending(
                        x => x.RequestTime);


                /* =========================================================
                   PAGING
                   ========================================================= */

                var data =
                    await query
                        .Skip(start)
                        .Take(length)
                        .Select(x => new
                        {
                            id = x.Id,

                            userId = x.UserId,

                            requestTime =
                                TimeZoneInfo.ConvertTimeFromUtc(
                                    DateTime.SpecifyKind(
                                        x.RequestTime,
                                        DateTimeKind.Utc),
                                    agentTimeZone)
                                .ToString(
                                    "yyyy-MM-dd HH:mm:ss"),

                            createdAt =
                                TimeZoneInfo.ConvertTimeFromUtc(
                                    DateTime.SpecifyKind(
                                        x.CreatedAt,
                                        DateTimeKind.Utc),
                                    agentTimeZone)
                                .ToString(
                                    "yyyy-MM-dd HH:mm:ss"),

                            updatedAt =
                                TimeZoneInfo.ConvertTimeFromUtc(
                                    DateTime.SpecifyKind(
                                        x.UpdatedAt,
                                        DateTimeKind.Utc),
                                    agentTimeZone)
                                .ToString(
                                    "yyyy-MM-dd HH:mm:ss"),

                            status = x.Status
                        })
                        .ToListAsync();


                /* =========================================================
                   TOTAL RECORDS
                   ========================================================= */

                var recordsTotal =
                    await _dbStorage.Context.KycRequestLogs
                        .AsNoTracking()
                        .CountAsync();


                /* =========================================================
                   DATATABLE RESPONSE
                   ========================================================= */

                return Json(new
                {
                    draw = draw,

                    recordsTotal =
                        recordsTotal,

                    recordsFiltered =
                        recordsFiltered,

                    data = data
                });
            }
            catch (Exception ex)
            {
                _logger.Error(
                    $"GetKycRequestLogs: {ex}");

                return Json(new
                {
                    draw = draw,

                    recordsTotal = 0,

                    recordsFiltered = 0,

                    data = new List<object>(),

                    error =
                        "Unable to load KYC request logs."
                });
            }
        }



        [HttpPost]
        public async Task<IActionResult> UpdateKycStatus(
            int id,
            int status)
        {
            try
            {
                // ============================================================
                // 1. Validate Status
                // ============================================================

                if (status != 1 && status != 2)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid KYC status."
                    });
                }


                // ============================================================
                // 2. Find KYC Request Log
                // ============================================================

                var kycRequest =
                    await _dbStorage.Context.KycRequestLogs
                    .FirstOrDefaultAsync(
                        x => x.Id == id
                    );


                if (kycRequest == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "KYC request not found."
                    });
                }



                // ============================================================
                // 3. Update KYC Request Status
                // ============================================================

                kycRequest.Status = (byte)status;



                // ============================================================
                // 4. Update User KYC Status
                // ============================================================

                var user =
                    await _dbStorage.Context.Users
                    .FirstOrDefaultAsync(
                        x => x.Id == kycRequest.UserId
                    );


                if (user == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "User not found."
                    });
                }


                user.KycStatus = (byte)status;



                // ============================================================
                // 5. Save Changes
                // ============================================================

                await _dbStorage.Context.SaveChangesAsync();



                // ============================================================
                // 6. Response
                // ============================================================

                return Json(new
                {
                    success = true,

                    message =
                        status == 1
                            ? "KYC approved successfully."
                            : "KYC rejected successfully."
                });

            }
            catch (Exception ex)
            {
                _logger.Error(
                    $"UpdateKycStatus Error: {ex}"
                );


                return Json(new
                {
                    success = false,
                    message = "An error occurred while updating KYC status."
                });
            }
        }


        //===========================================================================
        //===========================================================================
        //
        //               Statistics Logs
        //
        //===========================================================================
        //===========================================================================

        [HttpPost]
        public async Task<IActionResult> GetStatisticsLogs()
        {
            try
            {
                // =========================================================
                // DATATABLE PARAMETERS
                // =========================================================

                string? draw =
                    Request.Form["draw"]
                        .FirstOrDefault();


                int start =
                    int.TryParse(
                        Request.Form["start"]
                            .FirstOrDefault(),
                        out var parsedStart
                    )
                        ? parsedStart
                        : 0;


                int length =
                    int.TryParse(
                        Request.Form["length"]
                            .FirstOrDefault(),
                        out var parsedLength
                    )
                        ? parsedLength
                        : 25;


                if (start < 0)
                {
                    start = 0;
                }


                if (length <= 0)
                {
                    length = 25;
                }


                if (length > 100)
                {
                    length = 100;
                }


                // =========================================================
                // SORTING
                //
                // CURRENT RAZOR TABLE:
                //
                // 0 - Agent Code
                // 1 - Total Bet Amount
                // 2 - Total Win Amount
                // 3 - Total Bet Count
                // 4 - Total Win Count
                //
                // NOTE:
                // The Razor currently sends column 0 as its default order.
                // We keep the backend mapping correct to these 5 columns.
                // =========================================================

                int sortColumn =
                    int.TryParse(
                        Request.Form["order[0][column]"]
                            .FirstOrDefault(),
                        out var parsedSortColumn
                    )
                        ? parsedSortColumn
                        : 1;


                string sortDirection =
                    Request.Form["order[0][dir]"]
                        .FirstOrDefault()
                        ?? "desc";


                bool descending =
                    sortDirection.Equals(
                        "desc",
                        StringComparison.OrdinalIgnoreCase
                    );


                // =========================================================
                // FILTERS
                // =========================================================

                string? agent =
                    Request.Form["agent"]
                        .FirstOrDefault();

                string? user =
                    Request.Form["user"]
                        .FirstOrDefault();

                string? providerCode =
                    Request.Form["providerCode"]
                        .FirstOrDefault();

                string? gameCode =
                    Request.Form["gameCode"]
                        .FirstOrDefault();

                string? totalBetAmountText =
                    Request.Form["totalBetAmount"]
                        .FirstOrDefault();

                string? totalWinAmountText =
                    Request.Form["totalWinAmount"]
                        .FirstOrDefault();

                string? fromDate =
                    Request.Form["fromDate"]
                        .FirstOrDefault();

                string? toDate =
                    Request.Form["toDate"]
                        .FirstOrDefault();


                // =========================================================
                // TARGET AGENT
                //
                // Supported:
                //
                // targetAgent
                // targetAgentCode
                //
                // Empty = current logged-in agent.
                // =========================================================

                string? targetAgentCode =
                    Request.Form["targetAgent"]
                        .FirstOrDefault();


                if (
                    string.IsNullOrWhiteSpace(
                        targetAgentCode
                    )
                )
                {
                    targetAgentCode =
                        Request.Form["targetAgentCode"]
                            .FirstOrDefault();
                }


                if (
                    string.IsNullOrWhiteSpace(
                        targetAgentCode
                    )
                )
                {
                    targetAgentCode =
                        Request.Query["targetAgent"]
                            .FirstOrDefault();
                }


                if (
                    string.IsNullOrWhiteSpace(
                        targetAgentCode
                    )
                )
                {
                    targetAgentCode =
                        Request.Query["targetAgentCode"]
                            .FirstOrDefault();
                }


                targetAgentCode =
                    targetAgentCode?.Trim();


                // =========================================================
                // CURRENT LOGGED-IN AGENT
                // =========================================================

                Agent? currentAgent =
                    await GetCurrentAgentAsync();


                if (currentAgent == null)
                {
                    return Unauthorized();
                }


                // =========================================================
                // TARGET AGENT
                // =========================================================

                Agent? targetAgent =
                    await ResolveStatisticsTargetAgentAsync(
                        targetAgentCode,
                        currentAgent
                    );


                if (targetAgent == null)
                {
                    return Forbid();
                }


                // =========================================================
                // TIMEZONE
                // =========================================================

                TimeSpan agentOffset =
                    TryParseOffset(
                        currentAgent.TimeZone,
                        out var parsedOffset
                    )
                        ? parsedOffset
                        : TimeSpan.Zero;


                // =========================================================
                // DATE RANGE
                // =========================================================

                GetStatisticsUtcRange(
                    fromDate,
                    toDate,
                    agentOffset,
                    out DateTime fromUtc,
                    out DateTime toUtcExclusive
                );


                // =========================================================
                // STATISTICS BASE QUERY
                // =========================================================

                IQueryable<StatisticsLog> statisticsQuery =
                    _dbStorage.Context.StatisticsLogs
                        .AsNoTracking()
                        .Where(
                            x =>
                                x.StartTime >= fromUtc
                                &&
                                x.StartTime < toUtcExclusive
                        );


                // =========================================================
                // USER FILTER
                // =========================================================

                if (!string.IsNullOrWhiteSpace(user))
                {
                    user =
                        user.Trim();


                    statisticsQuery =
                        statisticsQuery.Where(
                            x =>
                                x.UserCode.Contains(user)
                        );
                }


                // =========================================================
                // PROVIDER FILTER
                // =========================================================

                if (!string.IsNullOrWhiteSpace(providerCode))
                {
                    providerCode =
                        providerCode.Trim();


                    statisticsQuery =
                        statisticsQuery.Where(
                            x =>
                                x.ProviderCode ==
                                providerCode
                        );
                }


                // =========================================================
                // GAME FILTER
                // =========================================================

                if (!string.IsNullOrWhiteSpace(gameCode))
                {
                    gameCode =
                        gameCode.Trim();


                    statisticsQuery =
                        statisticsQuery.Where(
                            x =>
                                x.GameCode.Contains(gameCode)
                        );
                }


                // =========================================================
                // DIRECT DOWNLINE AGENTS
                //
                // Target agent itself is NOT included.
                // =========================================================

                IQueryable<Agent> directAgents =
                    _dbStorage.Context.Agents
                        .AsNoTracking()
                        .Where(
                            x =>
                                x.UplineCode ==
                                targetAgent.AgentCode
                        );


                // =========================================================
                // TOTAL RECORDS
                // =========================================================

                int totalRecords =
                    await directAgents.CountAsync();


                // =========================================================
                // AGENT CODE FILTER
                // =========================================================

                if (!string.IsNullOrWhiteSpace(agent))
                {
                    agent =
                        agent.Trim();


                    directAgents =
                        directAgents.Where(
                            x =>
                                x.AgentCode.Contains(agent)
                        );
                }


                // =========================================================
                // AMOUNT FILTER PARSING
                // =========================================================

                bool hasBetAmountFilter =
                    false;


                decimal parsedBetAmount =
                    0m;


                if (!string.IsNullOrWhiteSpace(totalBetAmountText))
                {
                    string normalized =
                        totalBetAmountText
                            .Trim()
                            .Replace(",", "");


                    hasBetAmountFilter =
                        decimal.TryParse(
                            normalized,
                            NumberStyles.Number,
                            CultureInfo.InvariantCulture,
                            out parsedBetAmount
                        );
                }


                bool hasWinAmountFilter =
                    false;


                decimal parsedWinAmount =
                    0m;


                if (!string.IsNullOrWhiteSpace(totalWinAmountText))
                {
                    string normalized =
                        totalWinAmountText
                            .Trim()
                            .Replace(",", "");


                    hasWinAmountFilter =
                        decimal.TryParse(
                            normalized,
                            NumberStyles.Number,
                            CultureInfo.InvariantCulture,
                            out parsedWinAmount
                        );
                }


                bool invalidBetAmountFilter =
                    !string.IsNullOrWhiteSpace(
                        totalBetAmountText
                    )
                    &&
                    !hasBetAmountFilter;


                bool invalidWinAmountFilter =
                    !string.IsNullOrWhiteSpace(
                        totalWinAmountText
                    )
                    &&
                    !hasWinAmountFilter;


                if (
                    invalidBetAmountFilter
                    ||
                    invalidWinAmountFilter
                )
                {
                    return Json(
                        new
                        {
                            draw = draw,

                            recordsTotal =
                                totalRecords,

                            recordsFiltered =
                                0,

                            targetAgent =
                                targetAgent.AgentCode,

                            data =
                                new List<object>()
                        }
                    );
                }


                // =========================================================
                // AGENT AGGREGATION
                //
                // Each direct child gets:
                //
                // - its own statistics
                // - all descendant statistics
                //
                // using the existing AgentPath hierarchy rule.
                // =========================================================

                var aggregatedQuery =
                    directAgents.Select(
                        childAgent =>
                            new
                            {
                                AgentCode =
                                    childAgent.AgentCode,


                                TotalBetAmount =
                                    statisticsQuery
                                        .Where(
                                            x =>
                                                x.AgentCode ==
                                                    childAgent.AgentCode
                                                ||
                                                x.AgentPath.StartsWith(
                                                    childAgent.AgentPath +
                                                    childAgent.Id +
                                                    "."
                                                )
                                        )
                                        .Sum(
                                            x =>
                                                (decimal?)x.TotalBetAmount
                                        )
                                        ?? 0m,


                                TotalWinAmount =
                                    statisticsQuery
                                        .Where(
                                            x =>
                                                x.AgentCode ==
                                                    childAgent.AgentCode
                                                ||
                                                x.AgentPath.StartsWith(
                                                    childAgent.AgentPath +
                                                    childAgent.Id +
                                                    "."
                                                )
                                        )
                                        .Sum(
                                            x =>
                                                (decimal?)x.TotalWinAmount
                                        )
                                        ?? 0m,


                                TotalBetCount =
                                    statisticsQuery
                                        .Where(
                                            x =>
                                                x.AgentCode ==
                                                    childAgent.AgentCode
                                                ||
                                                x.AgentPath.StartsWith(
                                                    childAgent.AgentPath +
                                                    childAgent.Id +
                                                    "."
                                                )
                                        )
                                        .Sum(
                                            x =>
                                                (decimal?)x.TotalBetCount
                                        )
                                        ?? 0m,


                                TotalWinCount =
                                    statisticsQuery
                                        .Where(
                                            x =>
                                                x.AgentCode ==
                                                    childAgent.AgentCode
                                                ||
                                                x.AgentPath.StartsWith(
                                                    childAgent.AgentPath +
                                                    childAgent.Id +
                                                    "."
                                                )
                                        )
                                        .Sum(
                                            x =>
                                                (decimal?)x.TotalWinCount
                                        )
                                        ?? 0m
                            }
                    );


                // =========================================================
                // AMOUNT FILTERS
                // =========================================================

                if (hasBetAmountFilter)
                {
                    aggregatedQuery =
                        aggregatedQuery.Where(
                            x =>
                                x.TotalBetAmount ==
                                parsedBetAmount
                        );
                }


                if (hasWinAmountFilter)
                {
                    aggregatedQuery =
                        aggregatedQuery.Where(
                            x =>
                                x.TotalWinAmount ==
                                parsedWinAmount
                        );
                }


                // =========================================================
                // FILTERED RECORDS
                // =========================================================

                int filteredRecords =
                    await aggregatedQuery.CountAsync();


                // =========================================================
                // SORTING
                //
                // 0 = Agent Code
                // 1 = Total Bet Amount
                // 2 = Total Win Amount
                // 3 = Total Bet Count
                // 4 = Total Win Count
                // =========================================================

                aggregatedQuery =
                    sortColumn switch
                    {
                        0 =>
                            descending
                                ? aggregatedQuery
                                    .OrderByDescending(
                                        x => x.AgentCode
                                    )
                                : aggregatedQuery
                                    .OrderBy(
                                        x => x.AgentCode
                                    ),


                        1 =>
                            descending
                                ? aggregatedQuery
                                    .OrderByDescending(
                                        x => x.TotalBetAmount
                                    )
                                : aggregatedQuery
                                    .OrderBy(
                                        x => x.TotalBetAmount
                                    ),


                        2 =>
                            descending
                                ? aggregatedQuery
                                    .OrderByDescending(
                                        x => x.TotalWinAmount
                                    )
                                : aggregatedQuery
                                    .OrderBy(
                                        x => x.TotalWinAmount
                                    ),


                        3 =>
                            descending
                                ? aggregatedQuery
                                    .OrderByDescending(
                                        x => x.TotalBetCount
                                    )
                                : aggregatedQuery
                                    .OrderBy(
                                        x => x.TotalBetCount
                                    ),


                        4 =>
                            descending
                                ? aggregatedQuery
                                    .OrderByDescending(
                                        x => x.TotalWinCount
                                    )
                                : aggregatedQuery
                                    .OrderBy(
                                        x => x.TotalWinCount
                                    ),


                        _ =>
                            aggregatedQuery
                                .OrderByDescending(
                                    x => x.TotalBetAmount
                                )
                    };


                // =========================================================
                // PAGINATION
                // =========================================================

                var data =
                    await aggregatedQuery
                        .Skip(start)
                        .Take(length)
                        .ToListAsync();


                // =========================================================
                // RESPONSE
                // =========================================================

                return Json(
                    new
                    {
                        draw = draw,

                        recordsTotal =
                            totalRecords,

                        recordsFiltered =
                            filteredRecords,

                        /*
                         * Current Razor reads:
                         *
                         * json.targetAgent
                         */

                        targetAgent =
                            targetAgent.AgentCode,

                        data =
                            data.Select(
                                x =>
                                    new
                                    {
                                        x.AgentCode,

                                        x.TotalBetAmount,

                                        x.TotalWinAmount,

                                        x.TotalBetCount,

                                        x.TotalWinCount
                                    }
                            )
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.Error(
                    ex,
                    "Error loading statistics logs."
                );


                return Json(
                    new
                    {
                        draw =
                            Request.Form["draw"]
                                .FirstOrDefault(),

                        recordsTotal =
                            0,

                        recordsFiltered =
                            0,

                        data =
                            new List<object>(),

                        error =
                            "Failed to load statistics logs."
                    }
                );
            }
        }


        //===========================================================================
        //===========================================================================

        //               STATISTICS LOG HELPERS

        //===========================================================================
        //===========================================================================


        // =============================================================
        // RESOLVE STATISTICS TARGET AGENT
        // =============================================================

        private async Task<Agent?> ResolveStatisticsTargetAgentAsync(
            string? targetAgentCode,
            Agent currentAgent
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    targetAgentCode
                )
            )
            {
                return currentAgent;
            }


            targetAgentCode =
                targetAgentCode.Trim();


            // ---------------------------------------------------------
            // CURRENT AGENT
            // ---------------------------------------------------------

            if (
                string.Equals(
                    targetAgentCode,
                    currentAgent.AgentCode,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return currentAgent;
            }


            // ---------------------------------------------------------
            // REQUESTED AGENT
            // ---------------------------------------------------------

            Agent? targetAgent =
                await _dbStorage.Context.Agents
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x =>
                            x.AgentCode ==
                            targetAgentCode
                    );


            if (targetAgent == null)
            {
                return null;
            }


            // ---------------------------------------------------------
            // SECURITY
            // ---------------------------------------------------------

            string allowedPrefix =
                (currentAgent.AgentPath ?? string.Empty) +
                currentAgent.Id +
                ".";


            bool isDownline =
                !string.IsNullOrWhiteSpace(
                    targetAgent.AgentPath
                )
                &&
                targetAgent.AgentPath.StartsWith(
                    allowedPrefix,
                    StringComparison.Ordinal
                );


            if (!isDownline)
            {
                return null;
            }


            return targetAgent;
        }

        // =============================================================
        // STATISTICS DATE RANGE
        // =============================================================

        private static void GetStatisticsUtcRange(
            string? fromDate,
            string? toDate,
            TimeSpan agentOffset,
            out DateTime fromUtc,
            out DateTime toUtcExclusive
        )
        {
            DateTime agentNow =
                DateTime.UtcNow +
                agentOffset;


            DateTime fromLocal =
                agentNow.AddMonths(-1);


            DateTime toLocal =
                agentNow;


            // ---------------------------------------------------------
            // FROM
            // ---------------------------------------------------------

            if (
                DateTime.TryParse(
                    fromDate,
                    out DateTime parsedFrom
                )
            )
            {
                fromLocal =
                    parsedFrom;
            }


            // ---------------------------------------------------------
            // TO
            // ---------------------------------------------------------

            if (
                DateTime.TryParse(
                    toDate,
                    out DateTime parsedTo
                )
            )
            {
                toLocal =
                    parsedTo;
            }


            // ---------------------------------------------------------
            // Agent-local values have no timezone information.
            // ---------------------------------------------------------

            fromLocal =
                DateTime.SpecifyKind(
                    fromLocal,
                    DateTimeKind.Unspecified
                );


            toLocal =
                DateTime.SpecifyKind(
                    toLocal,
                    DateTimeKind.Unspecified
                );


            // ---------------------------------------------------------
            // Agent local -> UTC
            // ---------------------------------------------------------

            fromUtc =
                fromLocal -
                agentOffset;


            /*
             * Add one second because the Razor sends:
             *
             * yyyy-MM-dd HH:mm:ss
             *
             * DB timestamps may contain fractional seconds.
             */

            toUtcExclusive =
                toLocal
                    .AddSeconds(1)
                    -
                agentOffset;


            // ---------------------------------------------------------
            // Safety
            // ---------------------------------------------------------

            if (
                fromUtc >
                toUtcExclusive
            )
            {
                DateTime temp =
                    fromUtc;


                fromUtc =
                    toUtcExclusive;


                toUtcExclusive =
                    temp;
            }
        }

        //===========================================================================
        //===========================================================================
        //
        //               AGENT -> PROVIDER STATISTICS
        //
        //===========================================================================
        //===========================================================================

        [HttpPost]
        public async Task<IActionResult> GetStatisticsAgentProviders()
        {
            try
            {
                // =========================================================
                // CURRENT RAZOR SENDS:
                //
                // agentCode
                //
                // Also support:
                // targetAgent
                // targetAgentCode
                // =========================================================

                string? targetAgentCode =
                    Request.Form["agentCode"]
                        .FirstOrDefault();


                if (
                    string.IsNullOrWhiteSpace(
                        targetAgentCode
                    )
                )
                {
                    targetAgentCode =
                        Request.Form["targetAgent"]
                            .FirstOrDefault();
                }


                if (
                    string.IsNullOrWhiteSpace(
                        targetAgentCode
                    )
                )
                {
                    targetAgentCode =
                        Request.Form["targetAgentCode"]
                            .FirstOrDefault();
                }


                if (
                    string.IsNullOrWhiteSpace(
                        targetAgentCode
                    )
                )
                {
                    targetAgentCode =
                        Request.Query["targetAgent"]
                            .FirstOrDefault();
                }


                targetAgentCode =
                    targetAgentCode?.Trim();


                // =========================================================
                // FILTERS
                // =========================================================

                string? user =
                    Request.Form["user"]
                        .FirstOrDefault();


                string? providerCode =
                    Request.Form["providerCode"]
                        .FirstOrDefault();


                string? gameCode =
                    Request.Form["gameCode"]
                        .FirstOrDefault();


                string? totalBetAmountText =
                    Request.Form["totalBetAmount"]
                        .FirstOrDefault();


                string? totalWinAmountText =
                    Request.Form["totalWinAmount"]
                        .FirstOrDefault();


                string? fromDate =
                    Request.Form["fromDate"]
                        .FirstOrDefault();


                string? toDate =
                    Request.Form["toDate"]
                        .FirstOrDefault();


                // =========================================================
                // CURRENT AGENT
                // =========================================================

                Agent? currentAgent =
                    await GetCurrentAgentAsync();


                if (currentAgent == null)
                {
                    return Unauthorized();
                }


                // =========================================================
                // TARGET AGENT
                // =========================================================

                Agent? targetAgent =
                    await ResolveStatisticsTargetAgentAsync(
                        targetAgentCode,
                        currentAgent
                    );


                if (targetAgent == null)
                {
                    return Forbid();
                }


                // =========================================================
                // TIMEZONE
                // =========================================================

                TimeSpan agentOffset =
                    TryParseOffset(
                        currentAgent.TimeZone,
                        out var parsedOffset
                    )
                        ? parsedOffset
                        : TimeSpan.Zero;


                // =========================================================
                // DATE RANGE
                // =========================================================

                GetStatisticsUtcRange(
                    fromDate,
                    toDate,
                    agentOffset,
                    out DateTime fromUtc,
                    out DateTime toUtcExclusive
                );


                // =========================================================
                // TARGET AGENT SUBTREE
                // =========================================================

                string targetPathPrefix =
                    (targetAgent.AgentPath ?? string.Empty) +
                    targetAgent.Id +
                    ".";


                // =========================================================
                // BASE QUERY
                // =========================================================

                IQueryable<StatisticsLog> query =
                    _dbStorage.Context.StatisticsLogs
                        .AsNoTracking()
                        .Where(
                            x =>
                                x.StartTime >= fromUtc
                                &&
                                x.StartTime < toUtcExclusive
                                &&
                                (
                                    x.AgentCode ==
                                        targetAgent.AgentCode
                                    ||
                                    x.AgentPath.StartsWith(
                                        targetPathPrefix
                                    )
                                )
                        );


                // =========================================================
                // USER FILTER
                // =========================================================

                if (
                    !string.IsNullOrWhiteSpace(
                        user
                    )
                )
                {
                    user =
                        user.Trim();


                    query =
                        query.Where(
                            x =>
                                x.UserCode.Contains(user)
                        );
                }


                // =========================================================
                // PROVIDER FILTER
                // =========================================================

                if (
                    !string.IsNullOrWhiteSpace(
                        providerCode
                    )
                )
                {
                    providerCode =
                        providerCode.Trim();


                    query =
                        query.Where(
                            x =>
                                x.ProviderCode ==
                                providerCode
                        );
                }


                // =========================================================
                // GAME FILTER
                // =========================================================

                if (
                    !string.IsNullOrWhiteSpace(
                        gameCode
                    )
                )
                {
                    gameCode =
                        gameCode.Trim();


                    query =
                        query.Where(
                            x =>
                                x.GameCode.Contains(
                                    gameCode
                                )
                        );
                }


                // =========================================================
                // GROUP PROVIDERS
                // =========================================================

                var providerQuery =
                    query
                        .GroupBy(
                            x =>
                                new
                                {
                                    x.ProviderCode,
                                    x.ProviderName
                                }
                        )
                        .Select(
                            g =>
                                new
                                {
                                    ProviderCode =
                                        g.Key.ProviderCode,

                                    ProviderName =
                                        g.Key.ProviderName,

                                    TotalBetAmount =
                                        g.Sum(
                                            x =>
                                                x.TotalBetAmount
                                        ),

                                    TotalWinAmount =
                                        g.Sum(
                                            x =>
                                                x.TotalWinAmount
                                        ),

                                    TotalBetCount =
                                        g.Sum(
                                            x =>
                                                x.TotalBetCount
                                        ),

                                    TotalWinCount =
                                        g.Sum(
                                            x =>
                                                x.TotalWinCount
                                        )
                                }
                        );


                // =========================================================
                // AMOUNT FILTERS
                // =========================================================

                bool hasBetAmountFilter =
                    false;


                decimal parsedBetAmount =
                    0m;


                if (
                    !string.IsNullOrWhiteSpace(
                        totalBetAmountText
                    )
                )
                {
                    string normalized =
                        totalBetAmountText
                            .Trim()
                            .Replace(",", "");


                    hasBetAmountFilter =
                        decimal.TryParse(
                            normalized,
                            NumberStyles.Number,
                            CultureInfo.InvariantCulture,
                            out parsedBetAmount
                        );
                }


                bool hasWinAmountFilter =
                    false;


                decimal parsedWinAmount =
                    0m;


                if (
                    !string.IsNullOrWhiteSpace(
                        totalWinAmountText
                    )
                )
                {
                    string normalized =
                        totalWinAmountText
                            .Trim()
                            .Replace(",", "");


                    hasWinAmountFilter =
                        decimal.TryParse(
                            normalized,
                            NumberStyles.Number,
                            CultureInfo.InvariantCulture,
                            out parsedWinAmount
                        );
                }


                bool invalidBetAmount =
                    !string.IsNullOrWhiteSpace(
                        totalBetAmountText
                    )
                    &&
                    !hasBetAmountFilter;


                bool invalidWinAmount =
                    !string.IsNullOrWhiteSpace(
                        totalWinAmountText
                    )
                    &&
                    !hasWinAmountFilter;


                if (
                    invalidBetAmount
                    ||
                    invalidWinAmount
                )
                {
                    return Json(
                        new
                        {
                            recordsTotal = 0,

                            recordsFiltered = 0,

                            data =
                                new List<object>()
                        }
                    );
                }


                if (hasBetAmountFilter)
                {
                    providerQuery =
                        providerQuery.Where(
                            x =>
                                x.TotalBetAmount ==
                                parsedBetAmount
                        );
                }


                if (hasWinAmountFilter)
                {
                    providerQuery =
                        providerQuery.Where(
                            x =>
                                x.TotalWinAmount ==
                                parsedWinAmount
                        );
                }


                // =========================================================
                // CURRENT RAZOR BUILDS A NORMAL HTML TABLE.
                //
                // It does NOT send DataTables paging/sorting for this
                // child table, so return all providers.
                //
                // Default:
                // Total Bet Amount DESC
                // =========================================================

                var data =
                    await providerQuery
                        .OrderByDescending(
                            x =>
                                x.TotalBetAmount
                        )
                        .ThenBy(
                            x =>
                                x.ProviderName
                        )
                        .Select(
                            x =>
                                new
                                {
                                    /*
                                     * Current Razor still expects this
                                     * property even though we will later
                                     * remove it from the display.
                                     */

                                    x.ProviderCode,

                                    x.ProviderName,

                                    x.TotalBetAmount,

                                    x.TotalWinAmount,

                                    x.TotalBetCount,

                                    x.TotalWinCount
                                }
                        )
                        .ToListAsync();


                return Json(
                    new
                    {
                        recordsTotal =
                            data.Count,

                        recordsFiltered =
                            data.Count,

                        data =
                            data
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.Error(
                    ex,
                    "Error loading statistics agent providers."
                );


                return Json(
                    new
                    {
                        recordsTotal =
                            0,

                        recordsFiltered =
                            0,

                        data =
                            new List<object>(),

                        error =
                            "Failed to load agent provider statistics."
                    }
                );
            }
        }

        //===========================================================================
        //===========================================================================
        //
        //               DIRECT PLAYERS
        //
        //===========================================================================
        //===========================================================================

        [HttpPost]
        public async Task<IActionResult> GetStatisticsDirectPlayers()
        {
            try
            {
                // =========================================================
                // DATATABLE PARAMETERS
                // =========================================================

                string? draw =
                    Request.Form["draw"]
                        .FirstOrDefault();


                int start =
                    int.TryParse(
                        Request.Form["start"]
                            .FirstOrDefault(),
                        out var parsedStart
                    )
                        ? parsedStart
                        : 0;


                int length =
                    int.TryParse(
                        Request.Form["length"]
                            .FirstOrDefault(),
                        out var parsedLength
                    )
                        ? parsedLength
                        : 25;


                if (start < 0)
                {
                    start = 0;
                }


                if (length <= 0)
                {
                    length = 25;
                }


                if (length > 100)
                {
                    length = 100;
                }


                // =========================================================
                // SORTING
                //
                // 0 - Player Code
                // 1 - Total Bet Amount
                // 2 - Total Win Amount
                // 3 - Total Bet Count
                // 4 - Total Win Count
                // =========================================================

                int sortColumn =
                    int.TryParse(
                        Request.Form["order[0][column]"]
                            .FirstOrDefault(),
                        out var parsedSortColumn
                    )
                        ? parsedSortColumn
                        : 1;


                string sortDirection =
                    Request.Form["order[0][dir]"]
                        .FirstOrDefault()
                        ?? "desc";


                bool descending =
                    sortDirection.Equals(
                        "desc",
                        StringComparison.OrdinalIgnoreCase
                    );


                // =========================================================
                // PARAMETERS
                // =========================================================

                string? targetAgentCode =
                    Request.Form["targetAgent"]
                        .FirstOrDefault();


                if (
                    string.IsNullOrWhiteSpace(
                        targetAgentCode
                    )
                )
                {
                    targetAgentCode =
                        Request.Form["targetAgentCode"]
                            .FirstOrDefault();
                }


                if (
                    string.IsNullOrWhiteSpace(
                        targetAgentCode
                    )
                )
                {
                    targetAgentCode =
                        Request.Query["targetAgent"]
                            .FirstOrDefault();
                }


                string? user =
                    Request.Form["user"]
                        .FirstOrDefault();


                string? providerCode =
                    Request.Form["providerCode"]
                        .FirstOrDefault();


                string? gameCode =
                    Request.Form["gameCode"]
                        .FirstOrDefault();


                string? totalBetAmountText =
                    Request.Form["totalBetAmount"]
                        .FirstOrDefault();


                string? totalWinAmountText =
                    Request.Form["totalWinAmount"]
                        .FirstOrDefault();


                string? fromDate =
                    Request.Form["fromDate"]
                        .FirstOrDefault();


                string? toDate =
                    Request.Form["toDate"]
                        .FirstOrDefault();


                // =========================================================
                // CURRENT AGENT
                // =========================================================

                Agent? currentAgent =
                    await GetCurrentAgentAsync();


                if (currentAgent == null)
                {
                    return Unauthorized();
                }


                // =========================================================
                // TARGET AGENT
                // =========================================================

                Agent? targetAgent =
                    await ResolveStatisticsTargetAgentAsync(
                        targetAgentCode,
                        currentAgent
                    );


                if (targetAgent == null)
                {
                    return Forbid();
                }


                // =========================================================
                // TIMEZONE
                // =========================================================

                TimeSpan agentOffset =
                    TryParseOffset(
                        currentAgent.TimeZone,
                        out var parsedOffset
                    )
                        ? parsedOffset
                        : TimeSpan.Zero;


                // =========================================================
                // DATE RANGE
                // =========================================================

                GetStatisticsUtcRange(
                    fromDate,
                    toDate,
                    agentOffset,
                    out DateTime fromUtc,
                    out DateTime toUtcExclusive
                );


                // =========================================================
                // DIRECT PLAYERS ONLY
                //
                // No child-agent players.
                // =========================================================

                IQueryable<StatisticsLog> query =
                    _dbStorage.Context.StatisticsLogs
                        .AsNoTracking()
                        .Where(
                            x =>
                                x.AgentCode ==
                                    targetAgent.AgentCode
                                &&
                                x.StartTime >= fromUtc
                                &&
                                x.StartTime < toUtcExclusive
                        );


                // =========================================================
                // USER FILTER
                // =========================================================

                if (
                    !string.IsNullOrWhiteSpace(
                        user
                    )
                )
                {
                    user =
                        user.Trim();


                    query =
                        query.Where(
                            x =>
                                x.UserCode.Contains(user)
                        );
                }


                // =========================================================
                // PROVIDER FILTER
                // =========================================================

                if (
                    !string.IsNullOrWhiteSpace(
                        providerCode
                    )
                )
                {
                    providerCode =
                        providerCode.Trim();


                    query =
                        query.Where(
                            x =>
                                x.ProviderCode ==
                                providerCode
                        );
                }


                // =========================================================
                // GAME FILTER
                // =========================================================

                if (
                    !string.IsNullOrWhiteSpace(
                        gameCode
                    )
                )
                {
                    gameCode =
                        gameCode.Trim();


                    query =
                        query.Where(
                            x =>
                                x.GameCode.Contains(
                                    gameCode
                                )
                        );
                }


                // =========================================================
                // GROUP BY PLAYER
                // =========================================================

                var playerQuery =
                    query
                        .GroupBy(
                            x =>
                                x.UserCode
                        )
                        .Select(
                            g =>
                                new
                                {
                                    UserCode =
                                        g.Key,

                                    TotalBetAmount =
                                        g.Sum(
                                            x =>
                                                x.TotalBetAmount
                                        ),

                                    TotalWinAmount =
                                        g.Sum(
                                            x =>
                                                x.TotalWinAmount
                                        ),

                                    TotalBetCount =
                                        g.Sum(
                                            x =>
                                                x.TotalBetCount
                                        ),

                                    TotalWinCount =
                                        g.Sum(
                                            x =>
                                                x.TotalWinCount
                                        )
                                }
                        );


                // =========================================================
                // AMOUNT FILTERS
                // =========================================================

                bool hasBetAmountFilter =
                    false;


                decimal parsedBetAmount =
                    0m;


                if (
                    !string.IsNullOrWhiteSpace(
                        totalBetAmountText
                    )
                )
                {
                    string normalized =
                        totalBetAmountText
                            .Trim()
                            .Replace(",", "");


                    hasBetAmountFilter =
                        decimal.TryParse(
                            normalized,
                            NumberStyles.Number,
                            CultureInfo.InvariantCulture,
                            out parsedBetAmount
                        );
                }


                bool hasWinAmountFilter =
                    false;


                decimal parsedWinAmount =
                    0m;


                if (
                    !string.IsNullOrWhiteSpace(
                        totalWinAmountText
                    )
                )
                {
                    string normalized =
                        totalWinAmountText
                            .Trim()
                            .Replace(",", "");


                    hasWinAmountFilter =
                        decimal.TryParse(
                            normalized,
                            NumberStyles.Number,
                            CultureInfo.InvariantCulture,
                            out parsedWinAmount
                        );
                }


                bool invalidBetAmount =
                    !string.IsNullOrWhiteSpace(
                        totalBetAmountText
                    )
                    &&
                    !hasBetAmountFilter;


                bool invalidWinAmount =
                    !string.IsNullOrWhiteSpace(
                        totalWinAmountText
                    )
                    &&
                    !hasWinAmountFilter;


                if (
                    invalidBetAmount
                    ||
                    invalidWinAmount
                )
                {
                    return Json(
                        new
                        {
                            draw = draw,

                            recordsTotal = 0,

                            recordsFiltered = 0,

                            data =
                                new List<object>()
                        }
                    );
                }


                if (hasBetAmountFilter)
                {
                    playerQuery =
                        playerQuery.Where(
                            x =>
                                x.TotalBetAmount ==
                                parsedBetAmount
                        );
                }


                if (hasWinAmountFilter)
                {
                    playerQuery =
                        playerQuery.Where(
                            x =>
                                x.TotalWinAmount ==
                                parsedWinAmount
                        );
                }


                // =========================================================
                // TOTAL
                // =========================================================

                int totalRecords =
                    await playerQuery.CountAsync();


                // =========================================================
                // SORTING
                // =========================================================

                playerQuery =
                    sortColumn switch
                    {
                        0 =>
                            descending
                                ? playerQuery
                                    .OrderByDescending(
                                        x => x.UserCode
                                    )
                                : playerQuery
                                    .OrderBy(
                                        x => x.UserCode
                                    ),


                        1 =>
                            descending
                                ? playerQuery
                                    .OrderByDescending(
                                        x => x.TotalBetAmount
                                    )
                                : playerQuery
                                    .OrderBy(
                                        x => x.TotalBetAmount
                                    ),


                        2 =>
                            descending
                                ? playerQuery
                                    .OrderByDescending(
                                        x => x.TotalWinAmount
                                    )
                                : playerQuery
                                    .OrderBy(
                                        x => x.TotalWinAmount
                                    ),


                        3 =>
                            descending
                                ? playerQuery
                                    .OrderByDescending(
                                        x => x.TotalBetCount
                                    )
                                : playerQuery
                                    .OrderBy(
                                        x => x.TotalBetCount
                                    ),


                        4 =>
                            descending
                                ? playerQuery
                                    .OrderByDescending(
                                        x => x.TotalWinCount
                                    )
                                : playerQuery
                                    .OrderBy(
                                        x => x.TotalWinCount
                                    ),


                        _ =>
                            playerQuery
                                .OrderByDescending(
                                    x => x.TotalBetAmount
                                )
                    };


                // =========================================================
                // PAGINATION
                // =========================================================

                var data =
                    await playerQuery
                        .Skip(start)
                        .Take(length)
                        .Select(
                            x =>
                                new
                                {
                                    /*
                                     * IMPORTANT:
                                     *
                                     * Current Razor expects:
                                     * playerCode
                                     *
                                     * not UserCode.
                                     */

                                    playerCode =
                                        x.UserCode,

                                    x.TotalBetAmount,

                                    x.TotalWinAmount,

                                    x.TotalBetCount,

                                    x.TotalWinCount
                                }
                        )
                        .ToListAsync();


                // =========================================================
                // RESPONSE
                // =========================================================

                return Json(
                    new
                    {
                        draw = draw,

                        recordsTotal =
                            totalRecords,

                        recordsFiltered =
                            totalRecords,

                        data =
                            data
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.Error(
                    ex,
                    "Error loading statistics direct players."
                );


                return Json(
                    new
                    {
                        draw =
                            Request.Form["draw"]
                                .FirstOrDefault(),

                        recordsTotal =
                            0,

                        recordsFiltered =
                            0,

                        data =
                            new List<object>(),

                        error =
                            "Failed to load direct player statistics."
                    }
                );
            }
        }
        //===========================================================================
        //===========================================================================
        //
        //               PLAYER -> PROVIDER STATISTICS
        //
        //===========================================================================
        //===========================================================================

        [HttpPost]
        public async Task<IActionResult> GetStatisticsPlayerProviders()
        {
            try
            {
                // =========================================================
                // TARGET AGENT
                // =========================================================

                string? targetAgentCode =
                    Request.Form["targetAgent"]
                        .FirstOrDefault();


                if (
                    string.IsNullOrWhiteSpace(
                        targetAgentCode
                    )
                )
                {
                    targetAgentCode =
                        Request.Form["targetAgentCode"]
                            .FirstOrDefault();
                }


                if (
                    string.IsNullOrWhiteSpace(
                        targetAgentCode
                    )
                )
                {
                    targetAgentCode =
                        Request.Query["targetAgent"]
                            .FirstOrDefault();
                }


                // =========================================================
                // PLAYER CODE
                //
                // CURRENT RAZOR:
                //
                // playerCode
                //
                // Also accept userCode/user for compatibility.
                // =========================================================

                string? userCode =
                    Request.Form["playerCode"]
                        .FirstOrDefault();


                if (
                    string.IsNullOrWhiteSpace(
                        userCode
                    )
                )
                {
                    userCode =
                        Request.Form["userCode"]
                            .FirstOrDefault();
                }


                if (
                    string.IsNullOrWhiteSpace(
                        userCode
                    )
                )
                {
                    userCode =
                        Request.Form["user"]
                            .FirstOrDefault();
                }


                if (
                    string.IsNullOrWhiteSpace(
                        userCode
                    )
                )
                {
                    return Json(
                        new
                        {
                            recordsTotal = 0,

                            recordsFiltered = 0,

                            data =
                                new List<object>()
                        }
                    );
                }


                userCode =
                    userCode.Trim();


                // =========================================================
                // FILTERS
                // =========================================================

                string? providerCode =
                    Request.Form["providerCode"]
                        .FirstOrDefault();


                string? gameCode =
                    Request.Form["gameCode"]
                        .FirstOrDefault();


                string? totalBetAmountText =
                    Request.Form["totalBetAmount"]
                        .FirstOrDefault();


                string? totalWinAmountText =
                    Request.Form["totalWinAmount"]
                        .FirstOrDefault();


                string? fromDate =
                    Request.Form["fromDate"]
                        .FirstOrDefault();


                string? toDate =
                    Request.Form["toDate"]
                        .FirstOrDefault();


                // =========================================================
                // CURRENT AGENT
                // =========================================================

                Agent? currentAgent =
                    await GetCurrentAgentAsync();


                if (currentAgent == null)
                {
                    return Unauthorized();
                }


                // =========================================================
                // TARGET AGENT
                // =========================================================

                Agent? targetAgent =
                    await ResolveStatisticsTargetAgentAsync(
                        targetAgentCode,
                        currentAgent
                    );


                if (targetAgent == null)
                {
                    return Forbid();
                }


                // =========================================================
                // TIMEZONE
                // =========================================================

                TimeSpan agentOffset =
                    TryParseOffset(
                        currentAgent.TimeZone,
                        out var parsedOffset
                    )
                        ? parsedOffset
                        : TimeSpan.Zero;


                // =========================================================
                // DATE RANGE
                // =========================================================

                GetStatisticsUtcRange(
                    fromDate,
                    toDate,
                    agentOffset,
                    out DateTime fromUtc,
                    out DateTime toUtcExclusive
                );


                // =========================================================
                // DIRECT PLAYER QUERY
                //
                // Only this player's statistics under target agent.
                // =========================================================

                IQueryable<StatisticsLog> query =
                    _dbStorage.Context.StatisticsLogs
                        .AsNoTracking()
                        .Where(
                            x =>
                                x.AgentCode ==
                                    targetAgent.AgentCode
                                &&
                                x.UserCode ==
                                    userCode
                                &&
                                x.StartTime >= fromUtc
                                &&
                                x.StartTime < toUtcExclusive
                        );


                // =========================================================
                // PROVIDER FILTER
                // =========================================================

                if (
                    !string.IsNullOrWhiteSpace(
                        providerCode
                    )
                )
                {
                    providerCode =
                        providerCode.Trim();


                    query =
                        query.Where(
                            x =>
                                x.ProviderCode ==
                                providerCode
                        );
                }


                // =========================================================
                // GAME FILTER
                // =========================================================

                if (
                    !string.IsNullOrWhiteSpace(
                        gameCode
                    )
                )
                {
                    gameCode =
                        gameCode.Trim();


                    query =
                        query.Where(
                            x =>
                                x.GameCode.Contains(
                                    gameCode
                                )
                        );
                }


                // =========================================================
                // GROUP BY PROVIDER
                // =========================================================

                var providerQuery =
                    query
                        .GroupBy(
                            x =>
                                new
                                {
                                    x.ProviderCode,
                                    x.ProviderName
                                }
                        )
                        .Select(
                            g =>
                                new
                                {
                                    ProviderCode =
                                        g.Key.ProviderCode,

                                    ProviderName =
                                        g.Key.ProviderName,

                                    TotalBetAmount =
                                        g.Sum(
                                            x =>
                                                x.TotalBetAmount
                                        ),

                                    TotalWinAmount =
                                        g.Sum(
                                            x =>
                                                x.TotalWinAmount
                                        ),

                                    TotalBetCount =
                                        g.Sum(
                                            x =>
                                                x.TotalBetCount
                                        ),

                                    TotalWinCount =
                                        g.Sum(
                                            x =>
                                                x.TotalWinCount
                                        )
                                }
                        );


                // =========================================================
                // NUMERIC FILTERS
                // =========================================================

                bool hasBetAmountFilter =
                    false;


                decimal parsedBetAmount =
                    0m;


                if (
                    !string.IsNullOrWhiteSpace(
                        totalBetAmountText
                    )
                )
                {
                    string normalized =
                        totalBetAmountText
                            .Trim()
                            .Replace(",", "");


                    hasBetAmountFilter =
                        decimal.TryParse(
                            normalized,
                            NumberStyles.Number,
                            CultureInfo.InvariantCulture,
                            out parsedBetAmount
                        );
                }


                bool hasWinAmountFilter =
                    false;


                decimal parsedWinAmount =
                    0m;


                if (
                    !string.IsNullOrWhiteSpace(
                        totalWinAmountText
                    )
                )
                {
                    string normalized =
                        totalWinAmountText
                            .Trim()
                            .Replace(",", "");


                    hasWinAmountFilter =
                        decimal.TryParse(
                            normalized,
                            NumberStyles.Number,
                            CultureInfo.InvariantCulture,
                            out parsedWinAmount
                        );
                }


                bool invalidBetAmount =
                    !string.IsNullOrWhiteSpace(
                        totalBetAmountText
                    )
                    &&
                    !hasBetAmountFilter;


                bool invalidWinAmount =
                    !string.IsNullOrWhiteSpace(
                        totalWinAmountText
                    )
                    &&
                    !hasWinAmountFilter;


                if (
                    invalidBetAmount
                    ||
                    invalidWinAmount
                )
                {
                    return Json(
                        new
                        {
                            recordsTotal = 0,

                            recordsFiltered = 0,

                            data =
                                new List<object>()
                        }
                    );
                }


                if (hasBetAmountFilter)
                {
                    providerQuery =
                        providerQuery.Where(
                            x =>
                                x.TotalBetAmount ==
                                parsedBetAmount
                        );
                }


                if (hasWinAmountFilter)
                {
                    providerQuery =
                        providerQuery.Where(
                            x =>
                                x.TotalWinAmount ==
                                parsedWinAmount
                        );
                }


                // =========================================================
                // PROVIDER RESULT
                //
                // Current Razor builds a normal HTML table.
                //
                // Default:
                // Total Bet Amount DESC
                // =========================================================

                var data =
                    await providerQuery
                        .OrderByDescending(
                            x =>
                                x.TotalBetAmount
                        )
                        .ThenBy(
                            x =>
                                x.ProviderName
                        )
                        .Select(
                            x =>
                                new
                                {
                                    x.ProviderCode,

                                    x.ProviderName,

                                    x.TotalBetAmount,

                                    x.TotalWinAmount,

                                    x.TotalBetCount,

                                    x.TotalWinCount
                                }
                        )
                        .ToListAsync();


                // =========================================================
                // RESPONSE
                // =========================================================

                return Json(
                    new
                    {
                        recordsTotal =
                            data.Count,

                        recordsFiltered =
                            data.Count,

                        data =
                            data
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.Error(
                    ex,
                    "Error loading statistics player providers."
                );


                return Json(
                    new
                    {
                        recordsTotal =
                            0,

                        recordsFiltered =
                            0,

                        data =
                            new List<object>(),

                        error =
                            "Failed to load player provider statistics."
                    }
                );
            }
        }

    }
}

