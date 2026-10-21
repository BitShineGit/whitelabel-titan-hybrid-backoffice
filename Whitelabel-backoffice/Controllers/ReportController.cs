using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Whitelabel_backoffice.Models.ViewModels;
using Titan.HybridRepository.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Whitelabel_backoffice.Models;
using Whitelabel_backoffice.Services.Interfaces;
using System.Globalization;
using Azure.Core;
using System.Security.Claims;
using Titan.HybridRepository;
using System.Text.Json;
using Whitelabel_backoffice.Services;

namespace Whitelabel_backoffice.Controllers
{
    [Authorize]
    public class ReportController : BaseController
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IWhitelabelDBStorage _dbStorage;
        private readonly IConfiguration _configuration;
        private readonly IBoltGameService _gameService;
        public Global.Logging.ILogger _logger;
        private readonly WebSocketHandler _webSocketHandler;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISettingService _settingService;
        private readonly AppSettings _appSettings;



        public ReportController(
            IStringLocalizer<SharedResource> lang,
            Global.Logging.ILogger logger,
            IWhitelabelDBStorage dbStorage,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IConfiguration configuration,
            IBoltGameService gameService,
            WebSocketHandler webSocketHandler,
            IHttpClientFactory httpClientFactory,
            ISettingService settingService,
            IOptions<AppSettings> appSettings

        ) : base(lang)
        {
            _logger = logger;
            _dbStorage = dbStorage;
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _gameService = gameService;
            _httpClientFactory = httpClientFactory;
            _webSocketHandler = webSocketHandler;
            _settingService = settingService;
            _appSettings = appSettings.Value;

        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult LoginHistory()
        {
            return View("LoginHistory");
        }

        [HttpPost]
        public async Task<IActionResult> GetLoginHistory()
        {
            try
            {
                string? draw = Request.Form["draw"].FirstOrDefault();

                int start = int.TryParse(
                    Request.Form["start"].FirstOrDefault(),
                    out var parsedStart)
                    ? Math.Max(parsedStart, 0)
                    : 0;

                int length = int.TryParse(
                    Request.Form["length"].FirstOrDefault(),
                    out var parsedLength)
                    ? parsedLength
                    : 25;

                if (length <= 0)
                {
                    length = 25;
                }

                // -------------------------------------------------
                // SORTING
                // -------------------------------------------------

                int sortColumn = int.TryParse(
                    Request.Form["order[0][column]"].FirstOrDefault(),
                    out var parsedSortColumn)
                    ? parsedSortColumn
                    : 3;

                string sortDirection =
                    Request.Form["order[0][dir]"].FirstOrDefault() ?? "desc";

                bool descending = sortDirection.Equals(
                    "desc",
                    StringComparison.OrdinalIgnoreCase);

                // -------------------------------------------------
                // FILTERS
                // -------------------------------------------------

                string? targetCode = Request.Form["targetCode"].FirstOrDefault();
                string? targetTypeValue = Request.Form["targetType"].FirstOrDefault();
                string? ipAddress = Request.Form["ipAddress"].FirstOrDefault();
                string? action = Request.Form["action"].FirstOrDefault();
                string? device = Request.Form["device"].FirstOrDefault();
                string? domain = Request.Form["domain"].FirstOrDefault();
                string? targetNickname = Request.Form["targetNickname"].FirstOrDefault();
                string? fromDate = Request.Form["fromDate"].FirstOrDefault();
                string? toDate = Request.Form["toDate"].FirstOrDefault();

                // -------------------------------------------------
                // CURRENT AGENT TIMEZONE
                // -------------------------------------------------

                TimeSpan agentOffset = TimeSpan.Zero;
                string? agentLoginName = User.Identity?.Name;
                Agent? currentAgent = null;

                if (!string.IsNullOrWhiteSpace(agentLoginName))
                {
                    currentAgent = await _dbStorage.Context.Agents
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.AgentCode == agentLoginName);

                    if (currentAgent == null)
                    {
                        currentAgent = await _dbStorage.Context.Agents
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x => x.AgentLoginName == agentLoginName);
                    }
                }

                if (currentAgent != null &&
                    !string.IsNullOrWhiteSpace(currentAgent.TimeZone))
                {
                    string savedTimezone = currentAgent.TimeZone.Trim();

                    if (savedTimezone.StartsWith('+'))
                    {
                        savedTimezone = savedTimezone.Substring(1);
                    }

                    if (TimeSpan.TryParse(
                        savedTimezone,
                        CultureInfo.InvariantCulture,
                        out TimeSpan parsedOffset))
                    {
                        agentOffset = parsedOffset;
                    }
                }

                // -------------------------------------------------
                // BASE QUERY
                // -------------------------------------------------

                IQueryable<LoginLog> query = _dbStorage.Context.LoginLogs
                    .AsNoTracking();

                int totalRecords = await query.CountAsync();

                // -------------------------------------------------
                // DATE RANGE
                // Interpret submitted dates in the current agent's
                // timezone, then convert them to UTC for SQL filtering.
                // -------------------------------------------------

                DateTime agentNow = DateTime.UtcNow.Add(agentOffset);

                DateTime fromLocal = agentNow.AddMonths(-1);
                DateTime toLocal = agentNow;

                if (DateTime.TryParse(
                    fromDate,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime parsedFrom))
                {
                    fromLocal = parsedFrom;
                }

                if (DateTime.TryParse(
                    toDate,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime parsedTo))
                {
                    toLocal = parsedTo;
                }

                fromLocal = DateTime.SpecifyKind(
                    fromLocal,
                    DateTimeKind.Unspecified);

                toLocal = DateTime.SpecifyKind(
                    toLocal,
                    DateTimeKind.Unspecified);

                DateTime fromUtc = fromLocal.Subtract(agentOffset);
                DateTime toUtc = toLocal.Subtract(agentOffset);

                if (fromUtc > toUtc)
                {
                    (fromUtc, toUtc) = (toUtc, fromUtc);
                }

                query = query.Where(x =>
                    x.LoginTime >= fromUtc &&
                    x.LoginTime <= toUtc);

                // -------------------------------------------------
                // TARGET CODE
                // -------------------------------------------------

                if (!string.IsNullOrWhiteSpace(targetCode))
                {
                    targetCode = targetCode.Trim();
                    query = query.Where(x => x.TargetCode.Contains(targetCode));
                }

                // -------------------------------------------------
                // TARGET TYPE: 0 = Agent, 1 = Player
                // -------------------------------------------------

                if (byte.TryParse(targetTypeValue, out byte targetType) &&
                    targetType <= 1)
                {
                    query = query.Where(x => x.TargetType == targetType);
                }

                // -------------------------------------------------
                // IP ADDRESS
                // -------------------------------------------------

                if (!string.IsNullOrWhiteSpace(ipAddress))
                {
                    ipAddress = ipAddress.Trim();
                    query = query.Where(x => x.IpAddress.Contains(ipAddress));
                }

                // -------------------------------------------------
                // ACTION
                // -------------------------------------------------

                if (!string.IsNullOrWhiteSpace(action))
                {
                    action = action.Trim();
                    query = query.Where(x => x.Action == action);
                }

                // -------------------------------------------------
                // DEVICE
                // -------------------------------------------------

                if (!string.IsNullOrWhiteSpace(device))
                {
                    device = device.Trim();
                    query = query.Where(x => x.Device == device);
                }

                // -------------------------------------------------
                // DOMAIN
                // -------------------------------------------------

                if (!string.IsNullOrWhiteSpace(domain))
                {
                    domain = domain.Trim();
                    query = query.Where(x => x.Domain.Contains(domain));
                }

                // -------------------------------------------------
                // TARGET NICKNAME
                // -------------------------------------------------

                if (!string.IsNullOrWhiteSpace(targetNickname))
                {
                    targetNickname = targetNickname.Trim();
                    query = query.Where(
                        x => x.TargetNickName.Contains(targetNickname));
                }

                int filteredRecords = await query.CountAsync();

                // -------------------------------------------------
                // SERVER-SIDE SORTING
                // -------------------------------------------------

                query = sortColumn switch
                {
                    0 => descending
                        ? query.OrderByDescending(x => x.TargetCode)
                        : query.OrderBy(x => x.TargetCode),

                    1 => descending
                        ? query.OrderByDescending(x => x.TargetType)
                        : query.OrderBy(x => x.TargetType),

                    2 => descending
                        ? query.OrderByDescending(x => x.IpAddress)
                        : query.OrderBy(x => x.IpAddress),

                    3 => descending
                        ? query.OrderByDescending(x => x.LoginTime)
                        : query.OrderBy(x => x.LoginTime),

                    4 => descending
                        ? query.OrderByDescending(x => x.Action)
                        : query.OrderBy(x => x.Action),

                    5 => descending
                        ? query.OrderByDescending(x => x.Device)
                        : query.OrderBy(x => x.Device),

                    6 => descending
                        ? query.OrderByDescending(x => x.Domain)
                        : query.OrderBy(x => x.Domain),

                    7 => descending
                        ? query.OrderByDescending(x => x.TargetNickName)
                        : query.OrderBy(x => x.TargetNickName),

                    8 => descending
                        ? query.OrderByDescending(x => x.TargetEmail)
                        : query.OrderBy(x => x.TargetEmail),

                    9 => descending
                        ? query.OrderByDescending(x => x.DeviceDetail)
                        : query.OrderBy(x => x.DeviceDetail),

                    _ => query.OrderByDescending(x => x.LoginTime)
                };

                // -------------------------------------------------
                // PAGINATION
                // -------------------------------------------------

                var data = await query
                    .Skip(start)
                    .Take(length)
                    .Select(x => new
                    {
                        x.TargetCode,
                        x.TargetType,
                        x.IpAddress,
                        x.LoginTime,
                        x.Action,
                        x.Device,
                        x.Domain,
                        x.TargetNickName,
                        x.TargetEmail,
                        x.DeviceDetail
                    })
                    .ToListAsync();

                // -------------------------------------------------
                // CONVERT UTC -> CURRENT AGENT TIMEZONE
                // -------------------------------------------------

                var convertedData = data.Select(x =>
                {
                    DateTime loginUtc = DateTime.SpecifyKind(
                        x.LoginTime,
                        DateTimeKind.Utc);

                    DateTime loginLocal = loginUtc.Add(agentOffset);

                    return new
                    {
                        x.TargetCode,
                        x.TargetType,
                        TargetTypeName = x.TargetType == 0 ? "Agent" : "Player",
                        x.IpAddress,
                        Time = loginLocal.ToString("yyyy-MM-dd HH:mm:ss"),
                        x.Action,
                        x.Device,
                        x.Domain,
                        TargetNickname = x.TargetNickName,
                        x.TargetEmail,
                        x.DeviceDetail
                    };
                }).ToList();

                return Json(new
                {
                    draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = filteredRecords,
                    data = convertedData
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error loading login history.");

                return Json(new
                {
                    draw = Request.Form["draw"].FirstOrDefault(),
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new List<object>(),
                    error = "Failed to load login history."
                });
            }
        }

    }
}
