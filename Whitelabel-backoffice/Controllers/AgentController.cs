using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update.Internal;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Whitelabel_backoffice.Database;
using Whitelabel_backoffice.Models;
using Whitelabel_backoffice.Services.Interfaces;
using Whitelabel_backoffice.Services.Messaging.IntegratedApi;

namespace Whitelabel_backoffice.Controllers
{
    public class AgentController : BaseController
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IWhitelabelDBStorage _dbStorage;
        private readonly IConfiguration _configuration;
        private readonly IGameService _gameService;
        private readonly AppSettings _appSetting;
        private readonly ILogger<AgentController> _logger;

        public AgentController(
        IStringLocalizer<SharedResource> lang,
        ILogger<AgentController> logger,
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
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> VendorManage()
        {
            var providers = await _dbStorage.Context.Providers.ToListAsync();

            List<VendorManage> providerlist = new();
            foreach (var item in providers)
            {
                var provider = await _dbStorage.Context.Providers.Where(s => s.Id == item.Id).FirstOrDefaultAsync();
                var status = "";
                var category = "";
                if (provider.ProviderType == 1)
                    category = "Live Casino";
                if (provider.ProviderType == 2)
                    category = "Slot";
                if (provider.ProviderType == 3)
                    category = "Mini";
                if (provider.ProviderType == 4)
                    category = "Fishing";
                if (provider.ProviderType == 5)
                    category = "Sports";
                if (provider.ProviderType == 6)
                    category = "Poker";
                if (provider.Status == 1)
                    status = "open";
                if (provider.Status == 0)
                    status = "close";
                providerlist.Add(new VendorManage
                {
                    Id = item.Id,
                    ProviderCode = item.ProviderCode,
                    ProviderName = item.ProviderName,
                    Memo = item.Memo,
                    Logo = item.Logo,
                    ProviderType = item.ProviderType,
                    Status = item.Status,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt,
                    Category = category,
                    TotalGameCount = item.TotalGameCount,
                    StatusText = status
                });
            }

            ViewData["providerlist"] = providerlist;
            return View("VendorManage");
        }
        public async Task<IActionResult> UpdateStatus(int id = 0, string status = "")
        {
            var provider = await _dbStorage.Context.Providers.Where(s => s.Id == id).FirstOrDefaultAsync();
            if (status == "open")
            {
                provider.Status = 0;
            }
            else
            {
                provider.Status = 1;
            }
            await _dbStorage.Context.SaveChangesAsync();
            return Json(new
            {
                success = true
            });
        }


        [HttpPost]
        [Route("service/agent/vendormanage/update")]
        public async Task<IActionResult> Update([FromBody] UpdateProviderGamesRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.ProviderCode))
                return BadRequest(new { success = false, message = "ProviderCode is required." });

            var provider = await _dbStorage.Context.Providers
                .FirstOrDefaultAsync(x => x.ProviderCode == request.ProviderCode);
            if (provider == null)
                return NotFound(new { success = false, message = "Provider not found." });

            GetGamesResponse getGamesListResponse;
            try
            {
                getGamesListResponse = await _gameService.GetGameList(new GetGameListRequest
                {
                    ApiCode = _appSetting.ApiCode,
                    VendorCode = provider.ProviderCode,
                    Language = "en",
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Failed to fetch game list: {ex.Message}" });
            }

            if (getGamesListResponse?.Message == null || !getGamesListResponse.Message.Any())
                return Ok(new { success = true, addedCount = 0, updatedCount = 0, message = "No games returned from provider." });

            // pull existing game entities (not just codes) for THIS provider so we can diff/update
            var existingGames = await _dbStorage.Context.Games
                .Where(g => g.ProviderId == provider.Id)
                .ToDictionaryAsync(g => g.GameCode);

            var newGames = new List<Game>();
            var updatedCount = 0;
            var seenCodes = new HashSet<string>(existingGames.Keys);

            // codes actually returned by the provider in THIS call, used to detect games that
            // have disappeared from the provider's list
            var apiGameCodes = new HashSet<string>();

            foreach (var game in getGamesListResponse.Message)
            {
                apiGameCodes.Add(game.GameCode);

                var newStatus = game.UnderMaintenance ? (byte)0 : (byte)1;

                if (existingGames.TryGetValue(game.GameCode, out var existing))
                {
                    var changed = false;

                    if (existing.GameName != game.GameName)
                    {
                        existing.GameName = game.GameName;
                        changed = true;
                    }
                    if (existing.GameType != provider.ProviderType)
                    {
                        existing.GameType = provider.ProviderType;
                        changed = true;
                    }
                    if (existing.Thumbnail != game.Thumbnail)
                    {
                        existing.Thumbnail = game.Thumbnail;
                        changed = true;
                    }
                    if (existing.Status != newStatus)
                    {
                        existing.Status = newStatus;
                        changed = true;
                    }
                    if (existing.IsNew != game.IsNew)
                    {
                        existing.IsNew = game.IsNew;
                        changed = true;
                    }
                    // IsHot intentionally left alone - it's a manual curation flag, not synced from provider

                    if (changed)
                    {
                        updatedCount++;
                    }

                    continue;
                }

                if (!seenCodes.Add(game.GameCode))
                    continue; // guard against dup entries in the response

                newGames.Add(new Game
                {
                    ProviderId = provider.Id,
                    GameCode = game.GameCode,
                    GameName = game.GameName,
                    GameType = provider.ProviderType,
                    Thumbnail = game.Thumbnail,
                    Status = newStatus,
                    IsHot = false,
                    IsNew = game.IsNew,
                });
            }

            // any game we already had for this provider that the API no longer returned
            // gets deactivated (Status = 0), regardless of its previous status
            var missingCount = 0;
            foreach (var kvp in existingGames)
            {
                if (apiGameCodes.Contains(kvp.Key))
                    continue;

                if (kvp.Value.Status != 0)
                {
                    kvp.Value.Status = 0;
                    updatedCount++;
                }

                missingCount++;
            }

            if (newGames.Any())
            {
                await _dbStorage.Context.Games.AddRangeAsync(newGames);
            }

            provider.TotalGameCount = apiGameCodes.Count;
            provider.UpdatedAt = DateTime.UtcNow;

            await _dbStorage.Context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                providerCode = provider.ProviderCode,
                addedCount = newGames.Count,
                updatedCount,
                deactivatedCount = missingCount,
                totalGameCount = provider.TotalGameCount
            });
        }

        public class UpdateProviderGamesRequest
        {
            public string ProviderCode { get; set; }
        }
    }
}
