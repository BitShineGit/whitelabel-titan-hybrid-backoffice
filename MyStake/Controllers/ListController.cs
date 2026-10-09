using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Titan.Repository;
using MyStake.Models;
using MyStake.Services.Interfaces;
using MyStake.Services.Messaging.IntegratedApi;
using System.Collections.Generic;

namespace MyStake.Controllers
{
    public class ListController : BaseController
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IWhitelabelDBStorage _dbStorage;
        private readonly IConfiguration _configuration;
        private readonly IGameService _gameService = null;
        private readonly AppSettings _appSetting;
        public Global.Logging.ILogger _logger;
    
        public ListController(

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

        public async Task<IActionResult> Index(
            int providertype = 0,
            string providercode = "",
            int page = 1,
            int pageSize = 35)
        {
            _logger.Error($"[ListController->Index] [start] providertype: {providertype}, providercode: {providercode}, page: {page}, pageSize: {pageSize}");

            var title = "";

            var providers = await _dbStorage.Context.Providers
                .Where(s => s.ProviderType == 1)
                .ToListAsync();

            if (providertype == 1)
            {
                title = "Live";

                providers = await _dbStorage.Context.Providers
                    .Where(s => s.ProviderType == 1)
                    .ToListAsync();
            }

            if (providertype == 2)
            {
                title = "Slot";

                providers = await _dbStorage.Context.Providers
                    .Where(s => s.ProviderCode == "slot-pragmatic")
                    .ToListAsync();

                var providers2 = await _dbStorage.Context.Providers
                    .Where(s => s.ProviderCode == "slot-pgsoft")
                    .ToListAsync();

                var providers3 = await _dbStorage.Context.Providers
                    .Where(s => s.ProviderCode == "slot-booongo")
                    .ToListAsync();

                var providers4 = await _dbStorage.Context.Providers
                    .Where(s => s.ProviderCode == "slot-jili")
                    .ToListAsync();

                var providers5 = await _dbStorage.Context.Providers
                    .Where(s => s.ProviderCode == "slot-nolimitcity")
                    .ToListAsync();

                var providers6 = await _dbStorage.Context.Providers
                    .Where(s => s.ProviderCode == "slot-egt")
                    .ToListAsync();

                var providers7 = await _dbStorage.Context.Providers
                    .Where(s => s.ProviderCode == "slot-playngo")
                    .ToListAsync();

                providers.AddRange(providers2);
                providers.AddRange(providers3);
                providers.AddRange(providers4);
                providers.AddRange(providers5);
                providers.AddRange(providers6);
                providers.AddRange(providers7);
            }

            if (providertype == 3)
            {
                title = "Mini";

                providers = await _dbStorage.Context.Providers
                    .Where(s => s.ProviderType == 3)
                    .ToListAsync();
            }

            var defaultprovider = providercode;

            var currencyList = await _dbStorage.Context.Currencies
                .ToListAsync();

            ViewData["currencylist"] = currencyList;

            var provider = await _dbStorage.Context.Providers
                .Where(s => s.ProviderType == providertype)
                .ToListAsync();

            var providerid = await _dbStorage.Context.Providers
                .Where(s => s.ProviderCode == providercode)
                .FirstOrDefaultAsync();

            if (providerid == null)
            {
                _logger.Error($"[ListController->Index] [provider not found] providercode: {providercode}");

                return NotFound();
            }

            var selectproviderid = providerid.Id;

            var getGamesListResponse = await _dbStorage.Context.Games
                .Where(s => s.ProviderId == selectproviderid)
                .OrderByDescending(s => s.SortNumber)
                .ThenBy(s => s.Id)
                .ToListAsync();


            getGamesListResponse = getGamesListResponse
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();


            List<GameListMedel> SelectGameList = new();

            foreach (var item in getGamesListResponse)
            {
                SelectGameList.Add(new GameListMedel
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
                    VendorCode = providercode
                });
            }


            ViewData["SelectGameList"] = SelectGameList;
            ViewData["provider"] = provider;
            ViewData["default"] = defaultprovider;
            ViewData["providers"] = providers;
            ViewData["providertype"] = providertype;
            ViewData["providercode"] = providercode;
            ViewData["Title"] = title;


            _logger.Error($"[ListController->Index] [success] providercode: {providercode}, providerId: {selectproviderid}, gameCount: {SelectGameList.Count}");


            return View();
        }

        public async Task<IActionResult> GetGame(
            int page = 1,
            int pageSize = 35,
            string providercode = "")
        {
            if (string.IsNullOrWhiteSpace(providercode))
            {
                _logger.Error($"[ListController->GetGame] [empty provider code]");

                return BadRequest();
            }


            var provider = await _dbStorage.Context.Providers
                .FirstOrDefaultAsync(x => x.ProviderCode == providercode);


            if (provider == null)
            {
                _logger.Error($"[ListController->GetGame] [provider not found] providercode: {providercode}");

                return NotFound();
            }


            var query = _dbStorage.Context.Games
                .Where(x => x.ProviderId == provider.Id)
                .OrderByDescending(x => x.SortNumber)
                .ThenBy(x => x.Id);


            var totalGames = await query.CountAsync();


            var gamesFromDb = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();


            var games = new List<GameListMedel>();

            foreach (var item in gamesFromDb)
            {
                games.Add(new GameListMedel
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
                    VendorCode = providercode
                });
            }


            var hasMore = (page * pageSize) < totalGames;


            _logger.Error($"[ListController->GetGame] [success] providercode: {providercode}, page: {page}, gameCount: {games.Count}, totalGames: {totalGames}");


            return Json(new
            {
                success = true,
                msg = "Successfully loaded!",
                games,
                providercode,
                totalGames,
                hasMore
            });
        }


        [HttpPost]
        public async Task<IActionResult> GetGamesAjax(
            string code = "",
            int page = 1,
            string search = "")
        {
            const int pageSize = 30;

            if (page < 1)
                page = 1;

            _logger.Error($"[ListController->GetGamesAjax] [start] code: {code}, page: {page}, search: {search}");

            /*
             * If provider code is empty,
             * use the first provider.
             */
            if (string.IsNullOrWhiteSpace(code))
            {
                code = await _dbStorage.Context.Providers
                    .Select(p => p.ProviderCode)
                    .FirstOrDefaultAsync() ?? "";
            }

            /*
             * Find provider.
             */
            var provider = await _dbStorage.Context.Providers
                .FirstOrDefaultAsync(p => p.ProviderCode == code);

            if (provider == null)
            {
                _logger.Error($"[ListController->GetGamesAjax] [provider not found] code: {code}");

                return Json(new
                {
                    success = false,
                    msg = "Provider not found.",
                    selectGameList = new List<GameListMedel>(),
                    hasMore = false,
                    page,
                    pageSize,
                    totalGames = 0
                });
            }

            /*
             * Base game query.
             */
            var gameQuery = _dbStorage.Context.Games
                .Where(g => g.ProviderId == provider.Id);

            /*
             * Search filter.
             */
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                gameQuery = gameQuery.Where(g =>
                    g.GameName != null &&
                    EF.Functions.Like(
                        g.GameName,
                        $"%{search}%"
                    )
                );
            }

            /*
             * Count matching games.
             */
            var totalGames = await gameQuery.CountAsync();

            /*
             * Get ONLY the requested page.
             */
            var selectGame = await gameQuery
                .OrderByDescending(g => g.SortNumber)
                .ThenBy(g => g.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            /*
             * Check whether another page exists.
             */
            var hasMore = page * pageSize < totalGames;

            /*
             * Convert to GameListMedel.
             */
            var selectGameList = new List<GameListMedel>();

            foreach (var item in selectGame)
            {
                selectGameList.Add(new GameListMedel
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
                    VendorCode = code
                });
            }

            _logger.Error($"[ListController->GetGamesAjax] [success] code: {code}, page: {page}, search: {search}, gameCount: {selectGameList.Count}, totalGames: {totalGames}, hasMore: {hasMore}");

            return Json(new
            {
                success = true,
                msg = "Successfully loaded!",
                selectGameList,
                hasMore,
                page,
                pageSize,
                totalGames
            });
        }


        [HttpPost]
        public async Task<IActionResult> GetProvidersAjax(
            int page = 1,
            string search = "",
            int type = 2)
        {
            const int pageSize = 30;

            _logger.Error($"[ListController->GetProvidersAjax] [start] page: {page}, search: {search}, type: {type}");

            // Load providers.
            var providers = await _dbStorage.Context.Providers
                .Where(s => s.ProviderType == type)
                .ToListAsync();

            if (!string.IsNullOrEmpty(search))
            {
                providers = providers
                    .Where(g =>
                        g.ProviderCode != null &&
                        g.ProviderCode.Contains(
                            search,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            int totalproviders = providers.Count;
            int totalPages = (int)Math.Ceiling(totalproviders / (double)pageSize);

            if (page > totalPages && totalPages > 0)
                page = 1;

            var selectProviderList = providers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            _logger.Error($"[ListController->GetProvidersAjax] [success] type: {type}, page: {page}, search: {search}, providerCount: {selectProviderList.Count}, totalProviders: {totalproviders}");

            return Json(new
            {
                success = true,
                msg = "Successfully loaded!",
                selectProviderList
            });
        }


        [HttpPost]
        public async Task<IActionResult> GetMobileProviders()
        {
            _logger.Error($"[ListController->GetMobileProviders] [start]");

            var slot = await _dbStorage.Context.Providers
                .Where(s => s.ProviderType == 2)
                .ToListAsync();

            var casino = await _dbStorage.Context.Providers
                .Where(s => s.ProviderType == 1)
                .ToListAsync();

            var mini = await _dbStorage.Context.Providers
                .Where(s => s.ProviderType == 3)
                .ToListAsync();

            _logger.Error($"[ListController->GetMobileProviders] [success] slotCount: {slot.Count}, casinoCount: {casino.Count}, miniCount: {mini.Count}");

            return Json(new
            {
                success = true,
                slot,
                casino,
                mini
            });
        }
    }
}
