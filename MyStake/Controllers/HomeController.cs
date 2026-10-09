using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using MyStake.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using MyStake.Services.Interfaces;
using Microsoft.AspNetCore.Diagnostics;
using Titan.Repository;

namespace MyStake.Controllers
{
    public class HomeController : BaseController
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IWhitelabelDBStorage _dbStorage;
        private readonly IConfiguration _configuration;
        private readonly IGameService _gameService = null;
        private readonly AppSettings _appSetting;
        public Global.Logging.ILogger _logger;

        public HomeController(

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



        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        [HttpGet]
        public async Task<IActionResult> GetUserInfo()
        {
            try
            {
                var user = await _dbStorage.Context.Users
                    .Where(s => s.UserCode == User.Identity.Name)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    _logger.Error(
                        $"HomeController.GetUserInfo: User not found. " +
                        $"Identity: {User.Identity?.Name}");

                    return NotFound();
                }

                var symbol = "$";

                return Json(new
                {
                    user_code = user.UserCode,
                    user
                });
            }
            catch (Exception ex)
            {
                _logger.Error(
                    $"HomeController.GetUserInfo: {ex}");

                return Json(new
                {
                    success = false,
                    message = "Unable to get user information."
                });
            }
        }



        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 30)
        {
            try
            {
                if (page < 1)
                    page = 1;

                if (pageSize < 1)
                    pageSize = 30;

                if (pageSize > 100)
                    pageSize = 100;

                ViewData["IsSignupRoute"] = false;
                ViewData["PromoCode"] = "";

                var currencyList = await _dbStorage.Context.Currencies
                    .ToListAsync();

                if (currencyList == null || currencyList.Count == 0)
                {
                    _logger.Error(
                        "HomeController.Index: Currency list is empty.");
                }

                ViewData["currencylist"] = currencyList;

                var skip = (page - 1) * pageSize;

                var getGamesList = await _dbStorage.Context.Games
                    .Where(s => s.ProviderId == 1)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                if (getGamesList == null)
                {
                    _logger.Error(
                        "HomeController.Index: Slot games list returned null.");
                }

                List<GameListMedel> SlotGameList = new();

                foreach (var item in getGamesList)
                {
                    SlotGameList.Add(new GameListMedel
                    {
                        Id = item.Id,
                        ProviderId = item.ProviderId,
                        GameCode = item.GameCode,
                        GameName = item.GameName,
                        GameType = item.GameType,
                        Thumbnail = item.Thumbnail,
                        Status = item.Status,
                        IsHot = item.IsHot,
                        IsNew = item.IsNew,
                        SortNumber = item.SortNumber,
                        VendorCode = "slot-pragmatic"
                    });
                }


                var getLiveGamesList = await _dbStorage.Context.Games
                    .Where(s => s.ProviderId == 4)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                if (getLiveGamesList == null)
                {
                    _logger.Error(
                        "HomeController.Index: Live games list returned null.");
                }

                List<GameListMedel> LiveGameList = new();

                foreach (var item in getLiveGamesList)
                {
                    LiveGameList.Add(new GameListMedel
                    {
                        Id = item.Id,
                        ProviderId = item.ProviderId,
                        GameCode = item.GameCode,
                        GameName = item.GameName,
                        GameType = item.GameType,
                        Thumbnail = item.Thumbnail,
                        Status = item.Status,
                        IsHot = item.IsHot,
                        IsNew = item.IsNew,
                        SortNumber = item.SortNumber,
                        VendorCode = "casino-evolution"
                    });
                }


                var getMiniGamesList = await _dbStorage.Context.Games
                    .Where(s => s.ProviderId == 28)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                if (getMiniGamesList == null)
                {
                    _logger.Error(
                        "HomeController.Index: Mini games list returned null.");
                }

                List<GameListMedel> MiniGameList = new();

                foreach (var item in getMiniGamesList)
                {
                    MiniGameList.Add(new GameListMedel
                    {
                        Id = item.Id,
                        ProviderId = item.ProviderId,
                        GameCode = item.GameCode,
                        GameName = item.GameName,
                        GameType = item.GameType,
                        Thumbnail = item.Thumbnail,
                        Status = item.Status,
                        IsHot = item.IsHot,
                        IsNew = item.IsNew,
                        SortNumber = item.SortNumber,
                        VendorCode = "mini-spribe"
                    });
                }


                ViewData["Games1"] = SlotGameList;
                ViewData["Games3"] = LiveGameList;
                ViewData["Games7"] = MiniGameList;

                return View();
            }
            catch (Exception ex)
            {
                _logger.Error(
                    $"HomeController.Index: {ex}");

                return View();
            }
        }

        [Route("signup")]
        public async Task<IActionResult> Signup(
            int page = 1,
            int pageSize = 30,
            string promocode = "")
        {
            try
            {
                if (page < 1)
                    page = 1;

                if (pageSize < 1)
                    pageSize = 30;

                if (pageSize > 100)
                    pageSize = 100;

                ViewData["IsSignupRoute"] = true;

                ViewData["PromoCode"] =
                    !string.IsNullOrEmpty(promocode)
                        ? promocode
                        : "";


                var currencyList = await _dbStorage.Context.Currencies
                    .ToListAsync();

                if (currencyList == null || currencyList.Count == 0)
                {
                    _logger.Error(
                        "HomeController.Signup: Currency list is empty.");
                }

                ViewData["currencylist"] = currencyList;


                var skip = (page - 1) * pageSize;


                var getGamesList = await _dbStorage.Context.Games
                    .Where(s => s.ProviderId == 1)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                if (getGamesList == null)
                {
                    _logger.Error(
                        "HomeController.Signup: Slot games list returned null.");
                }


                List<GameListMedel> SlotGameList = new();

                foreach (var item in getGamesList)
                {
                    SlotGameList.Add(new GameListMedel
                    {
                        Id = item.Id,
                        ProviderId = item.ProviderId,
                        GameCode = item.GameCode,
                        GameName = item.GameName,
                        GameType = item.GameType,
                        Thumbnail = item.Thumbnail,
                        Status = item.Status,
                        IsHot = item.IsHot,
                        IsNew = item.IsNew,
                        SortNumber = item.SortNumber,
                        VendorCode = "slot-pragmatic"
                    });
                }


                var getLiveGamesList = await _dbStorage.Context.Games
                    .Where(s => s.ProviderId == 4)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                if (getLiveGamesList == null)
                {
                    _logger.Error(
                        "HomeController.Signup: Live games list returned null.");
                }


                List<GameListMedel> LiveGameList = new();

                foreach (var item in getLiveGamesList)
                {
                    LiveGameList.Add(new GameListMedel
                    {
                        Id = item.Id,
                        ProviderId = item.ProviderId,
                        GameCode = item.GameCode,
                        GameName = item.GameName,
                        GameType = item.GameType,
                        Thumbnail = item.Thumbnail,
                        Status = item.Status,
                        IsHot = item.IsHot,
                        IsNew = item.IsNew,
                        SortNumber = item.SortNumber,
                        VendorCode = "casino-evolution"
                    });
                }


                var getMiniGamesList = await _dbStorage.Context.Games
                    .Where(s => s.ProviderId == 28)
                    .Skip(skip)
                    .Take(pageSize)
                    .ToListAsync();

                if (getMiniGamesList == null)
                {
                    _logger.Error(
                        "HomeController.Signup: Mini games list returned null.");
                }


                List<GameListMedel> MiniGameList = new();

                foreach (var item in getMiniGamesList)
                {
                    MiniGameList.Add(new GameListMedel
                    {
                        Id = item.Id,
                        ProviderId = item.ProviderId,
                        GameCode = item.GameCode,
                        GameName = item.GameName,
                        GameType = item.GameType,
                        Thumbnail = item.Thumbnail,
                        Status = item.Status,
                        IsHot = item.IsHot,
                        IsNew = item.IsNew,
                        SortNumber = item.SortNumber,
                        VendorCode = "mini-spribe"
                    });
                }


                ViewData["Games1"] = SlotGameList;
                ViewData["Games3"] = LiveGameList;
                ViewData["Games7"] = MiniGameList;


                return View("Index");
            }
            catch (Exception ex)
            {
                _logger.Error(
                    $"HomeController.Signup: {ex}");

                return View("Index");
            }
        }

        public IActionResult NotFound()
        {
            var feature =
                HttpContext.Features.Get<IStatusCodeReExecuteFeature>();

            var originalPath = feature?.OriginalPath;

            var originalQueryString = feature?.OriginalQueryString;

            ViewBag.OriginalUrl = "https://localhost:7183" +
                originalPath + originalQueryString;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetNotifications()
        {
            var user = await _dbStorage.Context.Users
                .Where(s => s.UserCode == User.Identity.Name)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.Error($"[NotificationController->GetNotifications] [user not found] userCode: {User.Identity.Name}");

                return Json(new
                {
                    success = false
                });
            }

            var id = user.Id;

            var messages = await _dbStorage.Context.Messages
                .Where(s =>
                    (s.Type == 2 || s.TargetTableId == id) &&
                    s.Status == 1)
                .ToListAsync();

            var count = messages.Count;

            _logger.Error($"[NotificationController->GetNotifications] [success] userCode: {User.Identity.Name}, count: {count}");

            return Json(new
            {
                success = true,
                messages,
                count
            });
        }


        [HttpPost]
        public async Task<IActionResult> UpdateNotificationRead(int id = 0)
        {
            // IsRead is no longer stored in the database.
            // Read/view state is handled by localStorage.

            _logger.Error($"[NotificationController->UpdateNotificationRead] [success] userCode: {User.Identity.Name}, notificationId: {id}");

            return Json(new
            {
                success = true
            });
        }


        [HttpPost]
        public async Task<IActionResult> UpdateDelete(int id = 0)
        {
            // Delete state is no longer stored in the database.
            // Delete state is handled by localStorage.

            var user = await _dbStorage.Context.Users
                .Where(s => s.UserCode == User.Identity.Name)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.Error($"[NotificationController->UpdateDelete] [user not found] userCode: {User.Identity.Name}");

                return Json(new
                {
                    success = false,
                    count = 0
                });
            }

            var userid = user.Id;

            var messages = await _dbStorage.Context.Messages
                .Where(s =>
                    (s.Type == 2 || s.TargetTableId == userid) &&
                    s.Status == 1)
                .ToListAsync();

            var count = messages.Count;

            _logger.Error($"[NotificationController->UpdateDelete] [success] userCode: {User.Identity.Name}, notificationId: {id}, count: {count}");

            return Json(new
            {
                success = true,
                count
            });
        }


        [HttpPost]
        public async Task<IActionResult> GetUnreadMessages(int read = 0)
        {
            var user = await _dbStorage.Context.Users
                .Where(s => s.UserCode == User.Identity.Name)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.Error($"[NotificationController->GetUnreadMessages] [user not found] userCode: {User.Identity.Name}");

                return Json(new
                {
                    success = false,
                    messages = new List<object>(),
                    count = 0
                });
            }

            var id = user.Id;

            var messages = await _dbStorage.Context.Messages
                .Where(s =>
                    (s.Type == 2 || s.TargetTableId == id) &&
                    s.Status == 1)
                .ToListAsync();

            var count = messages.Count;

            _logger.Error($"[NotificationController->GetUnreadMessages] [success] userCode: {User.Identity.Name}, read: {read}, count: {count}");

            return Json(new
            {
                success = true,
                messages,
                count
            });
        }


        [HttpPost]
        public async Task<IActionResult> GetSearchNotification(
            int read = 0,
            string search = "")
        {
            var user = await _dbStorage.Context.Users
                .Where(s => s.UserCode == User.Identity.Name)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.Error($"[NotificationController->GetSearchNotification] [user not found] userCode: {User.Identity.Name}");

                return Json(new
                {
                    success = false,
                    messages = new List<object>(),
                    count = 0
                });
            }

            var id = user.Id;

            var messages = await _dbStorage.Context.Messages
                .Where(s =>
                    (s.Type == 2 || s.TargetTableId == id) &&
                    s.Status == 1)
                .ToListAsync();

            if (!string.IsNullOrEmpty(search))
            {
                messages = messages
                    .Where(g =>
                        g.Subject != null &&
                        g.Subject.Contains(
                            search,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var count = messages.Count;

            _logger.Error($"[NotificationController->GetSearchNotification] [success] userCode: {User.Identity.Name}, search: {search}, read: {read}, count: {count}");

            return Json(new
            {
                success = true,
                messages,
                count
            });
        }
    }


}
