using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using MyStake.Models;
using MyStake.Services.Interfaces;
using MyStake.Services.Messaging.IntegratedApi;
using System.Collections.Generic;
using Titan.Repository;

namespace MyStake.Controllers
{
    public class GameController : BaseController
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IWhitelabelDBStorage _dbStorage;
        private readonly IConfiguration _configuration;
        private readonly IGameService _gameService = null;
        private readonly AppSettings _appSetting;
        private readonly Global.Logging.ILogger _logger;


        public GameController(

            IStringLocalizer<SharedResource> lang,
            IWhitelabelDBStorage dbStorage,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IConfiguration configuration,
            IGameService gameService,
            IOptions<AppSettings> appsettings,
            Global.Logging.ILogger logger
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
        //public async Task<IActionResult> Index(string gameurl = "", string gamename = "")
        //{
        //    var currencyList = await _dbStorage.Context.Currencies.ToListAsync();
        //    var user = await _dbStorage.Context.Users.Where(s => s.UserCode == User.Identity.Name).FirstOrDefaultAsync();
        //    ViewData["currencylist"] = currencyList;
        //    ViewData["gameurl"] = gameurl;
        //    ViewData["gamename"] = gamename;
        //    ViewData["user"] = user;
        //    var userAgent = Request.Headers["User-Agent"].ToString();
        //    bool isMobile = userAgent.Contains("Android") || userAgent.Contains("iPhone") || userAgent.Contains("iPad");

        //    if (isMobile)
        //        return View("MobileIndex");

        //    return View();

        //}

        public async Task<IActionResult> GetLaunchUrl(
            string providercode,
            string gamecode,
            string gamename)
        {
            try
            {
                var provider = await _dbStorage.Context.Providers
                    .FirstOrDefaultAsync(s => s.ProviderCode == providercode);

                if (provider == null)
                {
                    _logger.Error(
                        $"GetLaunchUrl: Provider not found. " +
                        $"ProviderCode: {providercode}, " +
                        $"GameCode: {gamecode}, " +
                        $"Identity: {User.Identity?.Name}");

                    return BadRequest();
                }

                var user = await _dbStorage.Context.Users
                    .FirstOrDefaultAsync(u => u.UserCode == User.Identity.Name);

                if (user == null)
                {
                    _logger.Error(
                        $"GetLaunchUrl: Current user not found. " +
                        $"Identity: {User.Identity?.Name}, " +
                        $"ProviderCode: {providercode}, " +
                        $"GameCode: {gamecode}");

                    return Unauthorized();
                }

                var currency = await _dbStorage.Context.Currencies
                    .FirstOrDefaultAsync(c => c.Id == user.CurrencyId);

                if (currency == null)
                {
                    _logger.Error(
                        $"GetLaunchUrl: Currency not found. " +
                        $"CurrencyId: {user.CurrencyId}, " +
                        $"UserCode: {user.UserCode}, " +
                        $"GameCode: {gamecode}");

                    return BadRequest();
                }

                // Get games list via API
                var getGamesListResponse = await _gameService.GetGameList(
                    new MyStake.Services.Messaging.IntegratedApi.GetGameListRequest
                    {
                        ApiCode = _appSetting.ApiCode,
                        VendorCode = provider.ProviderCode,
                        Language = GetLanguage(),
                    });

                var game = getGamesListResponse.Message
                    .FirstOrDefault(s => s.GameCode == gamecode);

                if (game == null)
                {
                    _logger.Error(
                        $"GetLaunchUrl: Game not found. " +
                        $"GameCode: {gamecode}, " +
                        $"ProviderCode: {provider.ProviderCode}, " +
                        $"UserCode: {user.UserCode}");

                    return View("Index", new PlayViewModel
                    {
                        GameUrl = ""
                    });
                }

                // Get launch URL
                string lobbyUrl = _appSetting.MainDomain.TrimEnd('/');

                var getLaunchUrlResponse = await _gameService.GetLaunchURL(
                    new GetLaunchUrlRequest
                    {
                        ApiCode = _appSetting.ApiCode,
                        GameCode = game.GameCode,
                        UserCode = user.UserCode,
                        VendorCode = providercode,
                        Language = GetLanguage(),
                        Currency = currency.CurrencyCode,
                        LobbyUrl = lobbyUrl
                    });

                if (getLaunchUrlResponse == null || !getLaunchUrlResponse.Success)
                {
                    _logger.Error(
                        $"GetLaunchUrl: Failed to get launch URL. " +
                        $"GameCode: {gamecode}, " +
                        $"ProviderCode: {providercode}, " +
                        $"UserCode: {user.UserCode}");

                    return BadRequest();
                }

                // Prepare Game page
                var currencyList = await _dbStorage.Context.Currencies.ToListAsync();

                ViewData["currencylist"] = currencyList;
                ViewData["gameurl"] = getLaunchUrlResponse.Message;
                ViewData["gamename"] = gamename;
                ViewData["user"] = user;

                var userAgent = Request.Headers["User-Agent"].ToString();

                bool isMobile =
                    userAgent.Contains("Android") ||
                    userAgent.Contains("iPhone") ||
                    userAgent.Contains("iPad");

                if (isMobile)
                    return View("MobileIndex");

                return View("Index");
            }
            catch (Exception ex)
            {
                _logger.Error(
                    $"GetLaunchUrl: {ex}");

                return BadRequest();
            }
        }


    }
}
