using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Titan.HybridRepository;
using Titan.HybridRepository.Database;
using Whitelabel_backoffice.Models;
using Whitelabel_backoffice.Models.ViewModels;
using Whitelabel_backoffice.Services.Interfaces;
using Whitelabel_backoffice.Services.Messaging.IntegratedApi;

namespace Whitelabel_backoffice.Controllers
{
	public class SystemController : BaseController
	{
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IWhitelabelDBStorage _dbStorage;
        private readonly IConfiguration _configuration;
        private readonly IBoltGameService _gameService;
        private readonly AppSettings _appSetting;
        public Global.Logging.ILogger _logger;



        public SystemController(
            IStringLocalizer<SharedResource> lang,
            Global.Logging.ILogger logger,
            IWhitelabelDBStorage dbStorage,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IConfiguration configuration,
            IBoltGameService gameService,
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

        public async Task<IActionResult> WLProviders()
        {
            var wlproviders =
                await _dbStorage.Context.Wlproviders
					.AsNoTracking()
                    .OrderBy(x => x.Category)
                    .ToListAsync();

            return View(wlproviders);
        }

        [HttpGet]
        public async Task<IActionResult> APIProviders(string apiCode = "bolt")
        {
            apiCode = (apiCode ?? "").Trim().ToLowerInvariant();

            var apiList = await _dbStorage.Context.Apis
                .AsNoTracking()
                .OrderBy(x => x.ApiCode)
                .ToListAsync();

            if (!apiList.Any(x =>
                string.Equals(x.ApiCode, apiCode, StringComparison.OrdinalIgnoreCase)))
            {
                return NotFound();
            }

            var model = new APIProviderTableViewModel
            {
                ApiCode = apiCode,
                Title = $"{apiCode.ToUpperInvariant()} Providers",
                BulkSettings = new List<string> { "Wlstatus", "Maintenance" },
                Columns = GetAPIProviderColumns(apiCode)
            };

            switch (apiCode)
            {
                case "bolt":
                    {
                        var providers = await _dbStorage.Context.BoltProviders
                            .AsNoTracking()
                            .OrderBy(x => x.BoltType)
                            .ToListAsync();

                        model.Rows = providers
                            .Select(x => ToAPIProviderRow(x, model.Columns))
                            .ToList();

                        break;
                    }

                //case "gamingsoft":
                //    {
                //        var providers = await _dbStorage.Context.Gscproviders
                //            .AsNoTracking()
                //            .OrderBy(x => x.Gscprovider1)
                //            .ThenBy(x => x.GscproductId)
                //            .ToListAsync();

                //        model.Rows = providers
                //            .Select(x => ToAPIProviderRow(x, model.Columns))
                //            .ToList();

                //        break;
                //    }
                // Register other API data sources here.
                // The table markup and JavaScript remain unchanged.

                default:
                    return BadRequest("API provider configuration not found.");
            }

            model.WLProviderOptions = await _dbStorage.Context.Wlproviders
                .AsNoTracking()
                .Where(x => x.Wlstatus != 0)
                .OrderBy(x => x.Order)
                .ThenBy(x => x.WlproviderName)
                .Select(x => new APIProviderOption
                {
                    Value = x.WlproviderCode,
                    Text = x.WlproviderCode + " - " + x.WlproviderName
                })
                .ToListAsync();

            ViewData["ApiList"] = apiList;

            return View(model);
        }

        private static List<APIProviderColumn> GetAPIProviderColumns(string apiCode)
        {
            return apiCode switch
            {
                "bolt" => new List<APIProviderColumn>
                {
                    new() { Field = "BoltVendorCode", Title = "Vendor Code" },
                    new() { Field = "BoltType", Title = "Category", Type = "number" },
                    new() { Field = "BoltName", Title = "Vendor Name" },
                    new() { Field = "BoltStatus", Title = "API Status", Type = "number" },
                    new() { Field = "BolttotalGameCount", Title = "Total Games", Type = "number" },
                    new() { Field = "WlproviderCode", Title = "WL Provider Code", Type = "select", Editable = true },
                    new() { Field = "Wlstatus", Title = "WL Status", Type = "switch", Setting = "Wlstatus" },
                    new() { Field = "Wlstatus", Title = "Maintenance", Type = "switch", Setting = "Maintenance" }
                },

                //"gamingsoft" => new List<APIProviderColumn>
                //{
                //    new() { Field = "GscproviderId", Title = "GSC Provider ID", Type = "number" },
                //    new() { Field = "Gscprovider1", Title = "GSC Provider", Type = "text" },
                //    new() { Field = "GscproductId", Title = "Product ID", Type = "number" },
                //    new() { Field = "GscproductCode", Title = "Product Code", Type = "number" },
                //    new() { Field = "GscproductName", Title = "Product Name", Type = "text" },
                //    new() { Field = "GscproductTitle", Title = "Product Title", Type = "text" },
                //    new() { Field = "Gsccurrency", Title = "Currency", Type = "text" },
                //    new() { Field = "GscgameType", Title = "Game Type", Type = "text" },
                //    new() { Field = "Gscstatus", Title = "GSC Status", Type = "number" },
                //    new() { Field = "GsctotalGameCount", Title = "Total Games", Type = "number" },
                //    new() { Field = "WlproviderCode", Title = "WL Provider Code", Type = "select", Editable = true },
                //    new() { Field = "Wlstatus", Title = "WL Status", Type = "switch", Setting = "Wlstatus" },
                //    new() { Field = "Wlstatus", Title = "Maintenance", Type = "switch", Setting = "Maintenance" }
                //},

                _ => throw new ArgumentException("Unsupported API provider.", nameof(apiCode))
            };
        }

        private static Dictionary<string, object?> ToAPIProviderRow(
            object entity,
            IEnumerable<APIProviderColumn> columns)
        {
            var row = new Dictionary<string, object?>
            {
                ["Id"] = entity.GetType().GetProperty("Id")?.GetValue(entity)
            };

            foreach (var column in columns)
            {
                row[column.Field] = entity.GetType()
                    .GetProperty(column.Field)?
                    .GetValue(entity);
            }

            return row;
        }

        [HttpPost]
        public async Task<IActionResult> SetStatus(int id, string setting, bool value)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    msg = "Invalid provider ID."
                });
            }

            var wlprovider = await _dbStorage.Context.Wlproviders.FirstOrDefaultAsync(x => x.Id == id);

            if (wlprovider == null)
            {
                return NotFound(new
                {
                    success = false,
                    msg = "Provider not found."
                });
            }

            switch (setting)
            {
                case "IsPromo":
                    wlprovider.IsPromo = value;
                    break;

                case "ShowJackpot":
                    wlprovider.ShowJackpot = value;
                    break;

                case "Wlstatus":
                    wlprovider.Wlstatus = value ? (byte)1 : (byte)0;
                    break;

                case "Maintenance":
                    wlprovider.Wlstatus = value ? (byte)2 : (byte)1;
                    break;

                default:
                    return BadRequest(new
                    {
                        success = false,
                        msg = "Invalid setting."
                    });
            }

            wlprovider.UpdatedAt = DateTime.UtcNow;

            await _dbStorage.Context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                wlstatus = wlprovider.Wlstatus,
                msg = $"{wlprovider.WlproviderName}: {setting} updated successfully."
            });
        }


        [HttpPost]
        public async Task<IActionResult> AddWLProvider()
        {
            var provider = new Wlprovider
            {
                Category = 0,
                WlproviderCode = string.Empty,
                WlproviderName = string.Empty,
                Wllogo = string.Empty,
                Wlstatus = 0,
                WltotalGameCount = 0,
                IsPromo = false,
                ShowJackpot = false,
                Order = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbStorage.Context.Wlproviders.Add(provider);
            await _dbStorage.Context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                id = provider.Id,
                msg = "Empty WL provider created successfully."
            });
        }


        [HttpPost]
        public async Task<IActionResult> UpdateWLProvider(
            int id,
            string wlproviderCode,
            string wlproviderName,
            string? wllogo,
            byte category,
            int totalGameCount,
            int order)
        {
            if (id <= 0 || string.IsNullOrWhiteSpace(wlproviderCode) ||
                string.IsNullOrWhiteSpace(wlproviderName) ||
                totalGameCount < 0 || order < 0 || category > 7)
            {
                return BadRequest(new
                {
                    success = false,
                    msg = "Please enter valid provider details."
                });
            }

            var provider = await _dbStorage.Context.Wlproviders
                .FirstOrDefaultAsync(x => x.Id == id);

            if (provider == null)
            {
                return NotFound(new
                {
                    success = false,
                    msg = "Provider not found."
                });
            }

            provider.WlproviderCode = wlproviderCode.Trim();
            provider.WlproviderName = wlproviderName.Trim();
            provider.Wllogo = wllogo?.Trim() ?? string.Empty;
            provider.Category = category;
            provider.WltotalGameCount = totalGameCount;
            provider.Order = order;
            provider.UpdatedAt = DateTime.UtcNow;

            await _dbStorage.Context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                msg = "WL provider updated successfully."
            });
        }


        [HttpPost]
        public async Task<IActionResult> DeleteWLProviders(List<int> ids)
        {
            var providerIds = ids.Where(id => id > 0).Distinct().ToList();

            if (providerIds.Count == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    msg = "No providers selected."
                });
            }

            var providers = await _dbStorage.Context.Wlproviders
                .Where(x => providerIds.Contains(x.Id))
                .ToListAsync();

            if (providers.Count != providerIds.Count)
            {
                return NotFound(new
                {
                    success = false,
                    msg = "One or more providers could not be found."
                });
            }

            _dbStorage.Context.Wlproviders.RemoveRange(providers);
            await _dbStorage.Context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                msg = $"{providers.Count} provider(s) deleted successfully."
            });
        }


        [HttpPost]
        public async Task<IActionResult> SetBulkStatus(List<int> ids, string setting, bool value)
        {
            var providerIds = ids
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (providerIds.Count == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    msg = "Please select at least one provider."
                });
            }

            if (providerIds.Count > 500)
            {
                return BadRequest(new
                {
                    success = false,
                    msg = "You cannot update more than 500 providers at once."
                });
            }

            if (setting != "IsPromo" &&
                setting != "ShowJackpot" &&
                setting != "Wlstatus" &&
                setting != "Maintenance")
            {
                return BadRequest(new
                {
                    success = false,
                    msg = "Invalid status setting."
                });
            }

            var providers = await _dbStorage.Context.Wlproviders
                .Where(x => providerIds.Contains(x.Id))
                .ToListAsync();

            if (providers.Count != providerIds.Count)
            {
                return NotFound(new
                {
                    success = false,
                    msg = "One or more selected providers could not be found."
                });
            }

            foreach (var provider in providers)
            {
                switch (setting)
                {
                    case "IsPromo":
                        provider.IsPromo = value;
                        break;

                    case "ShowJackpot":
                        provider.ShowJackpot = value;
                        break;

                    case "Wlstatus":
                        provider.Wlstatus = value ? (byte)1 : (byte)0;
                        break;

                    case "Maintenance":
                        provider.Wlstatus = value ? (byte)2 : (byte)1;
                        break;
                }

                provider.UpdatedAt = DateTime.UtcNow;
            }

            await _dbStorage.Context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                count = providers.Count,
                setting,
                value,
                msg = $"{setting} updated successfully for {providers.Count} provider(s)."
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAPIProviderWLCode(string apiCode, int id, string wlproviderCode)
        {
            var validCode = await _dbStorage.Context.Wlproviders
                .AnyAsync(x => x.WlproviderCode == wlproviderCode);

            if (!validCode)
            {
                return BadRequest(new { success = false, msg = "Selected WL provider was not found." });
            }

            apiCode = (apiCode ?? "").Trim().ToLowerInvariant();
            wlproviderCode = (wlproviderCode ?? "").Trim();

            switch (apiCode)
            {
                case "bolt":
                    {
                        var provider = await _dbStorage.Context.BoltProviders
                            .FirstOrDefaultAsync(x => x.Id == id);

                        if (provider == null)
                            return NotFound(new { success = false, msg = "Provider not found." });

                        provider.WlproviderCode = wlproviderCode;
                        provider.UpdatedAt = DateTime.UtcNow;
                        break;
                    }

                //case "gamingsoft":
                //    {
                //        var provider = await _dbStorage.Context.Gscproviders
                //            .FirstOrDefaultAsync(x => x.Id == id);

                //        if (provider == null)
                //            return NotFound(new { success = false, msg = "Provider not found." });

                //        provider.WlproviderCode = wlproviderCode;
                //        provider.UpdatedAt = DateTime.UtcNow;
                //        break;
                //    }

                default:
                    return BadRequest(new { success = false, msg = "Unsupported API provider." });
            }

            try
            {
                await _dbStorage.Context.SaveChangesAsync();
                return Json(new { success = true, msg = "WL provider mapping updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to update API provider WL mapping. API: {ApiCode}, ID: {ProviderId}", apiCode, id);
                return StatusCode(500, new { success = false, msg = "Failed to update WL provider mapping." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SetAPIProviderStatus(string apiCode, int id, string setting, bool value)
        {
            apiCode = (apiCode ?? "").Trim().ToLowerInvariant();
            setting = (setting ?? "").Trim();

            if (!(apiCode == "bolt" || apiCode == "gamingsoft") || id <= 0 ||
                (setting != "Wlstatus" && setting != "Maintenance"))
            {
                return BadRequest(new { success = false, msg = "Invalid request." });
            }


            byte currentStatus;

            switch (apiCode)
            {
                case "bolt":
                    {
                        var provider = await _dbStorage.Context.BoltProviders
                            .FirstOrDefaultAsync(x => x.Id == id);

                        if (provider == null)
                            return NotFound(new { success = false, msg = "Provider not found." });

                        provider.Wlstatus = setting == "Maintenance"
                            ? (byte)(value ? 2 : 1)
                            : (byte)(value ? 1 : 0);

                        provider.UpdatedAt = DateTime.UtcNow;
                        currentStatus = provider.Wlstatus;
                        break;
                    }

                //case "gamingsoft":
                //    {
                //        var provider = await _dbStorage.Context.Gscproviders
                //            .FirstOrDefaultAsync(x => x.Id == id);

                //        if (provider == null)
                //            return NotFound(new { success = false, msg = "Provider not found." });

                //        provider.Wlstatus = setting == "Maintenance"
                //            ? (byte)(value ? 2 : 1)
                //            : (byte)(value ? 1 : 0);

                //        provider.UpdatedAt = DateTime.UtcNow;
                //        currentStatus = provider.Wlstatus;
                //        break;
                //    }

                default:
                    return BadRequest(new { success = false, msg = "Unsupported API provider." });
            }

            try
            {
                await _dbStorage.Context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    wlstatus = currentStatus,
                    msg = "Provider status updated successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to update API provider status. ID: {ProviderId}", id);
                return StatusCode(500, new { success = false, msg = "Failed to update provider status." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SetBulkAPIProviderStatus(string apiCode, List<int> ids, string setting, bool value)
        {
            apiCode = (apiCode ?? "").Trim().ToLowerInvariant();
            setting = (setting ?? "").Trim();

            if (!(apiCode == "bolt" || apiCode == "gamingsoft") ||
                ids == null ||
                ids.Count == 0 ||
                ids.Count > 500 ||
                ids.Any(x => x <= 0) ||
                (setting != "Wlstatus" && setting != "Maintenance"))
            {
                return BadRequest(new { success = false, msg = "Invalid request." });
            }

            var distinctIds = ids.Distinct().ToList();

            switch (apiCode)
            {
                case "bolt":
                    {
                        var providers = await _dbStorage.Context.BoltProviders
                            .Where(x => distinctIds.Contains(x.Id))
                            .ToListAsync();

                        if (providers.Count != distinctIds.Count)
                        {
                            return NotFound(new { success = false, msg = "One or more providers were not found." });
                        }

                        var status = setting switch
                        {
                            "Wlstatus" => (byte)(value ? 1 : 0),
                            "Maintenance" => (byte)(value ? 2 : 1),
                            _ => (byte)0
                        };

                        foreach (var provider in providers)
                        {
                            provider.Wlstatus = status;
                            provider.UpdatedAt = DateTime.UtcNow;
                        }

                        try
                        {
                            await _dbStorage.Context.SaveChangesAsync();

                            return Json(new
                            {
                                success = true,
                                count = providers.Count,
                                msg = $"Updated {providers.Count} provider(s) successfully."
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, "Failed to bulk update API provider statuses.");
                            return StatusCode(500, new { success = false, msg = "Failed to update provider statuses." });
                        }
                    }

                //case "gamingsoft":
                //    {
                //        var providers = await _dbStorage.Context.Gscproviders
                //            .Where(x => distinctIds.Contains(x.Id))
                //            .ToListAsync();

                //        if (providers.Count != distinctIds.Count)
                //        {
                //            return NotFound(new { success = false, msg = "One or more providers were not found." });
                //        }

                //        var status = setting switch
                //        {
                //            "Wlstatus" => (byte)(value ? 1 : 0),
                //            "Maintenance" => (byte)(value ? 2 : 1),
                //            _ => (byte)0
                //        };

                //        foreach (var provider in providers)
                //        {
                //            provider.Wlstatus = status;
                //            provider.UpdatedAt = DateTime.UtcNow;
                //        }

                //        try
                //        {
                //            await _dbStorage.Context.SaveChangesAsync();

                //            return Json(new
                //            {
                //                success = true,
                //                count = providers.Count,
                //                msg = $"Updated {providers.Count} provider(s) successfully."
                //            });
                //        }
                //        catch (Exception ex)
                //        {
                //            _logger.Error(ex, "Failed to bulk update API provider statuses.");
                //            return StatusCode(500, new { success = false, msg = "Failed to update provider statuses." });
                //        }
                //    }

                default:
                    return BadRequest(new { success = false, msg = "Unsupported API provider." });
            }

        }

        [HttpPost]
        public async Task<IActionResult> SyncBoltProviders()
        {
            var result = await _gameService.GetProviderList(new Models.ApiModels.GetProviderListRequestModel { ApiCode = "bolt" });

            if (result == null || !result.success || result.message == null)
            {
                return BadRequest(new { success = false, errorCode = result?.errorCode });
            }

            // one entry per vendor code
            var incoming = result.message
                .Where(v => !string.IsNullOrWhiteSpace(v.VendorCode))
                .GroupBy(v => v.VendorCode, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            // safety: an empty list would deactivate every provider, treat it as a bad response instead
            if (incoming.Count == 0)
            {
                return BadRequest(new { success = false, message = "BOLT returned an empty vendor list, nothing was changed" });
            }

            var context = _dbStorage.Context;
            var now = DateTime.UtcNow;
            var existing = await context.BoltProviders.ToListAsync();
            var existingByCode = existing
                .GroupBy(x => x.BoltVendorCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var incomingCodes = new HashSet<string>(incoming.Select(v => v.VendorCode), StringComparer.OrdinalIgnoreCase);

            int added = 0, updated = 0, reactivated = 0, deactivated = 0;

            // 1. insert new vendors, refresh changed ones
            foreach (var vendor in incoming)
            {
                if (existingByCode.TryGetValue(vendor.VendorCode, out var row))
                {
                    var changed = false;

                    // GGX-owned fields: only written when they really differ
                    if (row.BoltName != vendor.Name || row.BoltType != (byte)vendor.Type)
                    {
                        row.BoltName = vendor.Name;
                        row.BoltType = (byte)vendor.Type;
                        changed = true;
                    }

                    // vendor is back in the GGX list after being switched off
                    if (row.BoltStatus == 0)
                    {
                        row.BoltStatus = 1;
                        reactivated++;
                        changed = true;
                    }

                    if (changed)
                    {
                        row.UpdatedAt = now;
                        updated++;
                    }
                }
                else
                {
                    var newRow = new BoltProvider
                    {
                        BoltVendorCode = vendor.VendorCode,
                        BoltType = (byte)vendor.Type,
                        BoltName = vendor.Name,
                        BoltStatus = 1,
                        BoltTotalGameCount = 0,
                        WlproviderCode = "",
                        Wlstatus = 0,
                        CreatedAt = now,
                        UpdatedAt = now,
                    };
                    context.BoltProviders.Add(newRow);
                    existingByCode[vendor.VendorCode] = newRow;   // protects against a repeat within the same run
                    added++;
                }
            }

            // 2. vendors stored before but missing from the new list -> BoltStatus = 0
            foreach (var row in existing)
            {
                if (!incomingCodes.Contains(row.BoltVendorCode) && row.BoltStatus != 0)
                {
                    row.BoltStatus = 0;
                    row.UpdatedAt = now;
                    deactivated++;
                }
            }

            await context.SaveChangesAsync();

            return Ok(new { success = true, added, updated, reactivated, deactivated, total = incoming.Count });
        }

        [HttpPost]
        public async Task<IActionResult> SyncBoltGames([FromQuery] string language = "en", [FromQuery] string vendorCode = null)
        {
            var context = _dbStorage.Context;
            var now = DateTime.UtcNow;

            var providerQuery = context.BoltProviders.Where(p => p.BoltStatus == 1);

            // optional filter: only sync the games of this vendor
            if (!string.IsNullOrWhiteSpace(vendorCode))
            {
                var code = vendorCode.Trim();
                providerQuery = providerQuery.Where(p => p.BoltVendorCode == code);
            }

            var providers = await providerQuery.ToListAsync();

            if (!string.IsNullOrWhiteSpace(vendorCode) && providers.Count == 0)
            {
                return Ok(new
                {
                    success = false,
                    vendorCode,
                    message = "No active provider found for this vendor code. Run the provider sync first, or check its status."
                });
            }

            var summary = new List<object>();
            int totalAdded = 0, totalUpdated = 0, totalReactivated = 0, totalDeactivated = 0, totalCount = 0;

            foreach (var provider in providers)
            {
                try
                {
                    var gamesResponse = await _gameService.GetGameList(new GetGameListRequest
                    {
                        ApiCode = "bolt",
                        VendorCode = provider.BoltVendorCode,
                        Language = language
                    });

                    if (gamesResponse?.Message == null)
                    {
                        summary.Add(new { vendor = provider.BoltVendorCode, status = "no data" });
                        continue;
                    }

                    // one entry per game code
                    var incoming = gamesResponse.Message
                        .Where(g => !string.IsNullOrWhiteSpace(g.GameCode))
                        .GroupBy(g => g.GameCode, StringComparer.OrdinalIgnoreCase)
                        .Select(g => g.First())
                        .ToList();
                    var incomingCodes = new HashSet<string>(incoming.Select(g => g.GameCode), StringComparer.OrdinalIgnoreCase);

                    var existing = await context.BoltGames
                        .Where(g => g.BoltVendorCode == provider.BoltVendorCode)
                        .ToListAsync();
                    var existingByCode = existing
                        .GroupBy(g => g.BoltGameCode, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                    int added = 0, updated = 0, reactivated = 0, deactivated = 0;

                    // 1. insert new games, refresh changed ones
                    foreach (var game in incoming)
                    {
                        if (existingByCode.TryGetValue(game.GameCode, out var row))
                        {
                            var changed = false;

                            // GGX-owned fields: a null from the API never overwrites a stored value
                            var name = game.GameName ?? row.BoltGameName;
                            var slug = game.Slug ?? row.BoltSlug;
                            var thumbnail = game.Thumbnail ?? row.BoltThumbnail;

                            if (row.BoltGameName != name) { row.BoltGameName = name; changed = true; }
                            if (row.BoltSlug != slug) { row.BoltSlug = slug; changed = true; }
                            if (row.BoltThumbnail != thumbnail) { row.BoltThumbnail = thumbnail; changed = true; }
                            if (row.BoltIsNew != game.IsNew) { row.BoltIsNew = game.IsNew; row.IsNew = game.IsNew; changed = true; }
                            if (row.BoltProvider != provider.BoltName) { row.BoltProvider = provider.BoltName; changed = true; }

                            // game is back in the GGX list after being switched off
                            if (row.BoltStatus == 0)
                            {
                                row.BoltStatus = 1;
                                reactivated++;
                                changed = true;
                            }

                            if (changed)
                            {
                                row.UpdatedAt = now;
                                updated++;
                            }
                        }
                        else
                        {
                            var newRow = new BoltGame
                            {
                                BoltProvider = provider.BoltName,
                                BoltVendorCode = provider.BoltVendorCode,
                                BoltGameCode = game.GameCode,
                                BoltGameName = game.GameName ?? "",
                                BoltSlug = game.Slug ?? "",
                                BoltThumbnail = game.Thumbnail ?? "",
                                BoltIsNew = game.IsNew,
                                IsNew = game.IsNew,
                                BoltStatus = 1,
                                Wlstatus = 0,
                                CreatedAt = now,
                                UpdatedAt = now,
                            };
                            context.BoltGames.Add(newRow);
                            existingByCode[game.GameCode] = newRow;   // guards against duplicate codes in the same response
                            added++;
                        }
                    }

                    // 2. games stored before but missing from the new list -> BoltStatus = 0
                    if (incoming.Count > 0)
                    {
                        foreach (var row in existing)
                        {
                            if (!incomingCodes.Contains(row.BoltGameCode) && row.BoltStatus != 0)
                            {
                                row.BoltStatus = 0;
                                row.UpdatedAt = now;
                                deactivated++;
                            }
                        }
                    }
                    else if (existing.Count > 0)
                    {
                        // an empty answer is suspicious (it can also be a failed call), so keep the stored games
                        summary.Add(new { vendor = provider.BoltVendorCode, status = "empty result, nothing deactivated", storedGames = existing.Count });
                        continue;
                    }

                    provider.BoltTotalGameCount = incoming.Count;

                    // Save per provider so one failure doesn't roll back the others
                    await context.SaveChangesAsync();

                    totalAdded += added;
                    totalUpdated += updated;
                    totalReactivated += reactivated;
                    totalDeactivated += deactivated;
                    totalCount += incoming.Count;
                    summary.Add(new { vendor = provider.BoltVendorCode, added, updated, reactivated, deactivated, total = incoming.Count });
                }
                catch (Exception e)
                {
                    context.ChangeTracker.Clear();   // drop the failed provider's pending changes
                    _logger.Error($"SyncBoltGames vendor {provider.BoltVendorCode}: {e}");
                    summary.Add(new { vendor = provider.BoltVendorCode, status = "failed" });
                }
            }

            return Ok(new { success = true, vendorCode, totalAdded, totalUpdated, totalReactivated, totalDeactivated, totalCount, providers = summary });
        }

        private static string Key(int productId, int productCode, string currency, string gameType) =>
            $"{productId}|{productCode}|{currency?.ToUpperInvariant()}|{gameType?.ToUpperInvariant()}";

        // Adjust to whatever your byte values mean
        private static byte MapStatus(string status) => status?.ToUpperInvariant() switch
        {
            "ACTIVATED" => 1,
            "MAINTAINED" => 2,
            _ => 0
        };

        private static List<APIGameColumn> GetAPIGameColumns(string apiCode)
        {
            return apiCode switch
            {
                "bolt" => new List<APIGameColumn>
                {
                    new() { Field = "Id", Title = "ID", Type = "number" },
                    new() { Field = "BoltThumbnail", Title = "Thumbnail", Type = "image" },
                    new() { Field = "BoltVendorCode", Title = "Vendor Code" },
                    new() { Field = "BoltGameCode", Title = "Game Code" },
                    new() { Field = "BoltGameName", Title = "Game Name" },
                    new() { Field = "BoltStatus", Title = "API Status", Type = "status" },
                    new() { Field = "BoltIsNew", Title = "Bolt New", Type = "bool" },
                    new() { Field = "SortNumber", Title = "Sort", Type = "number" },
                    new() { Field = "IsNew", Title = "New", Type = "switch" },
                    new() { Field = "IsJackpot", Title = "Jackpot", Type = "switch" },
                    new() { Field = "IsHot", Title = "Hot", Type = "switch" },
                    new() { Field = "Wlstatus", Title = "WL Status", Type = "switch" },
                    new() { Field = "Maintenance", Title = "Maintenance", Type = "switch" },
                },
                //"gamingsoft" => new List<APIGameColumn>
                //{
                //    new() { Field = "Id", Title = "ID", Type = "number" },
                //    new() { Field = "GscimageUrl", Title = "Image", Type = "image" },
                //    new() { Field = "GscgameCode", Title = "Game Code" },
                //    new() { Field = "GscgameName", Title = "Game Name" },
                //    new() { Field = "GscgameType", Title = "Game Type" },
                //    new() { Field = "GscproductId", Title = "Product ID", Type = "number" },
                //    new() { Field = "GscproductCode", Title = "Product Code", Type = "number" },
                //    new() { Field = "GscsupportCurrency", Title = "Currency" },
                //    new() { Field = "Gscstatus", Title = "API Status", Type = "status" },
                //    new() { Field = "Wlstatus", Title = "WL Status", Type = "switch" },
                //    new() { Field = "Maintenance", Title = "Maintenance", Type = "switch" },
                //    new() { Field = "IsHot", Title = "Hot", Type = "switch" },
                //    new() { Field = "IsJackpot", Title = "Jackpot", Type = "switch" },
                //    new() { Field = "IsNew", Title = "New", Type = "switch" },
                //    new() { Field = "SortNumber", Title = "Sort", Type = "number" }
                //},
                _ => throw new ArgumentException(
                    "Unsupported API game configuration.", nameof(apiCode))
            };
        }

        private static Dictionary<string, object?> ToAPIGameRow(object entity, IEnumerable<APIGameColumn> columns)
        {
            var row = new Dictionary<string, object?>
            {
                ["Id"] = entity.GetType().GetProperty("Id")?.GetValue(entity)
            };

            var wlStatusValue = entity.GetType()
                .GetProperty("Wlstatus")?.GetValue(entity);

            var wlStatus = Convert.ToByte(wlStatusValue ?? (byte)0);

            foreach (var column in columns)
            {
                row[column.Field] = column.Field switch
                {
                    "Maintenance" => wlStatus == 2,
                    _ => entity.GetType().GetProperty(column.Field)?.GetValue(entity)
                };
            }

            return row;
        }

        [HttpGet]
        public async Task<IActionResult> APIGames(string apiCode = "bolt", string? providerCode = null)
        {
            apiCode = (apiCode ?? "").Trim().ToLowerInvariant();
            providerCode = providerCode?.Trim();

            var context = _dbStorage.Context;

            var apiList = await context.Apis
                .AsNoTracking()
                .OrderBy(x => x.ApiCode)
                .ToListAsync();

            if (!apiList.Any(x => string.Equals(
                x.ApiCode, apiCode, StringComparison.OrdinalIgnoreCase)))
            {
                return NotFound();
            }

            var model = new APIGamesViewModel
            {
                ApiCode = apiCode,
                ProviderCode = providerCode,
                Title = $"{apiCode.ToUpperInvariant()} Games",
                Columns = GetAPIGameColumns(apiCode)
            };

            switch (apiCode)
            {
                case "bolt":
                    {
                        model.Providers = await context.BoltProviders
                            .AsNoTracking()
                            .OrderBy(x => x.BoltName)
                            .Select(x => new APIGameProviderOption
                            {
                                Value = x.BoltVendorCode,
                                Text = x.BoltName + " (" + x.BoltVendorCode + ")"
                            })
                            .ToListAsync();

                        if (!string.IsNullOrWhiteSpace(providerCode))
                        {
                            var provider = model.Providers.FirstOrDefault(x =>
                                string.Equals(
                                    x.Value,
                                    providerCode,
                                    StringComparison.OrdinalIgnoreCase));

                            if (provider == null)
                            {
                                return BadRequest("Invalid provider selected.");
                            }

                            var games = await context.BoltGames
                                .AsNoTracking()
                                .Where(x => x.BoltVendorCode == provider.Value)
                                .OrderByDescending(x => x.SortNumber)
                                .ThenBy(x => x.BoltGameName)
                                .ToListAsync();

                            model.Rows = games
                                .Select(x => ToAPIGameRow(x, model.Columns))
                                .ToList();
                        }

                        break;
                    }
                //case "gamingsoft":
                //    {
                //        model.Providers = await context.Gscproviders
                //            .OrderBy(x => x.GscproductName)
                //            .Select(x => new APIGameProviderOption
                //            {
                //                Value = x.GscproductCode.ToString(),
                //                Text = x.GscproductName + " (" + x.GscproductCode + ")"
                //            })
                //            .Distinct()
                //            .ToListAsync();

                //        if (!string.IsNullOrWhiteSpace(providerCode))
                //        {
                //            if (!int.TryParse(providerCode, out var productCode))
                //            {
                //                return BadRequest("Invalid GSC product code.");
                //            }

                //            var providerExists = await context.Gscproviders
                //                .AnyAsync(x => x.GscproductCode == productCode);

                //            if (!providerExists)
                //            {
                //                return NotFound("GSC provider not found.");
                //            }

                //            var games = await context.Gscgames
                //                .Where(x => x.GscproductCode == productCode)
                //                .OrderBy(x => x.SortNumber)
                //                .ThenBy(x => x.GscgameName)
                //                .ToListAsync();

                //            model.Rows = games
                //                .Select(x => ToAPIGameRow(x, model.Columns))
                //                .ToList();
                //        }

                //        break;
                //    }
                default:
                    return BadRequest("Game configuration not found for this API.");
            }

            ViewData["ApiList"] = apiList;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SetAPIGameSetting(string apiCode, int id, string setting, bool value)
        {
            apiCode = apiCode?.Trim().ToLowerInvariant() ?? "";

            var allowedSettings = new[]
            {
                "Wlstatus",
                "Maintenance",
                "IsNew",
                "IsJackpot",
                "IsHot"
            };

            if (!allowedSettings.Contains(setting))
                return BadRequest(new { success = false, message = "Invalid setting." });

            switch (apiCode)
            {
                case "bolt":
                    {
                        var game = await _dbStorage.Context.BoltGames
                            .FirstOrDefaultAsync(x => x.Id == id);

                        if (game == null)
                            return NotFound(new { success = false, message = "Game not found." });

                        ApplyGameSetting(game, setting, value);
                        game.UpdatedAt = DateTime.UtcNow;
                        break;
                    }

                //case "gamingsoft":
                //    {
                //        var game = await _dbStorage.Context.Gscgames
                //            .FirstOrDefaultAsync(x => x.Id == id);

                //        if (game == null)
                //            return NotFound(new { success = false, message = "Game not found." });

                //        ApplyGameSetting(game, setting, value);
                //        game.UpdatedAt = DateTime.UtcNow;
                //        break;
                //    }

                default:
                    return BadRequest(new { success = false, message = "Unsupported API." });
            }

            await _dbStorage.Context.SaveChangesAsync();

            return Json(new { success = true, message = "Game setting updated." });
        }

        private static void ApplyGameSetting(object game, string setting, bool value)
        {
            var type = game.GetType();

            switch (setting)
            {
                case "Wlstatus":
                    {
                        var property = type.GetProperty("Wlstatus")!;

                        var currentStatus = Convert.ToByte(property.GetValue(game) ?? 0);

                        property.SetValue(
                            game,
                            Convert.ChangeType(
                                value
                                    ? (currentStatus == 2 ? (byte)2 : (byte)1)
                                    : (byte)0,
                                property.PropertyType));

                        break;
                    }

                case "Maintenance":
                    {
                        var property = type.GetProperty("Wlstatus")!;

                        var currentStatus = Convert.ToByte(property.GetValue(game) ?? 0);

                        var newStatus = value
                            ? (byte)2
                            : currentStatus == 2 ? (byte)1 : currentStatus;

                        property.SetValue(
                            game,
                            Convert.ChangeType(newStatus, property.PropertyType));

                        break;
                    }

                case "IsNew":
                case "IsJackpot":
                case "IsHot":
                    type.GetProperty(setting)!.SetValue(game, value);
                    break;
            }
        }

        [HttpPost]
        public async Task<IActionResult> SetBulkAPIGameSetting(string apiCode, List<int> ids, string setting, bool value)
        {
            apiCode = apiCode?.Trim().ToLowerInvariant() ?? "";

            if (ids == null || ids.Count == 0 || ids.Count > 500)
                return BadRequest(new { success = false, message = "Select between 1 and 500 games." });

            if (ids.Any(id => id <= 0))
                return BadRequest(new { success = false, message = "Invalid game ID." });

            if (ids.Distinct().Count() != ids.Count)
                return BadRequest(new { success = false, message = "Duplicate game IDs." });

            if (!new[] { "Wlstatus", "Maintenance", "IsNew", "IsJackpot", "IsHot" }
                .Contains(setting))
            {
                return BadRequest(new { success = false, message = "Invalid setting." });
            }

            var updatedAt = DateTime.UtcNow;
            var updatedCount = 0;

            switch (apiCode)
            {
                case "bolt":
                    {
                        var games = await _dbStorage.Context.BoltGames
                            .Where(x => ids.Contains(x.Id))
                            .ToListAsync();

                        if (games.Count != ids.Count)
                            return NotFound(new { success = false, message = "One or more games were not found." });

                        foreach (var game in games)
                        {
                            ApplyGameSetting(game, setting, value);
                            game.UpdatedAt = updatedAt;
                        }

                        updatedCount = games.Count;
                        break;
                    }

                //case "gamingsoft":
                //    {
                //        var games = await _dbStorage.Context.Gscgames
                //            .Where(x => ids.Contains(x.Id))
                //            .ToListAsync();

                //        if (games.Count != ids.Count)
                //            return NotFound(new { success = false, message = "One or more games were not found." });

                //        foreach (var game in games)
                //        {
                //            ApplyGameSetting(game, setting, value);
                //            game.UpdatedAt = updatedAt;
                //        }

                //        updatedCount = games.Count;
                //        break;
                //    }

                default:
                    return BadRequest(new { success = false, message = "Unsupported API." });
            }

            await _dbStorage.Context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"{updatedCount} game(s) updated.",
                updatedCount
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAPIGameSortNumber(string apiCode, int id, int sortNumber)
        {
            apiCode = apiCode?.Trim().ToLowerInvariant() ?? "";

            if (sortNumber < 0)
                return BadRequest(new { success = false, message = "Sort number cannot be negative." });

            switch (apiCode)
            {
                case "bolt":
                    {
                        var game = await _dbStorage.Context.BoltGames
                            .FirstOrDefaultAsync(x => x.Id == id);

                        if (game == null)
                            return NotFound(new { success = false, message = "Game not found." });

                        game.SortNumber = sortNumber;
                        game.UpdatedAt = DateTime.UtcNow;
                        break;
                    }

                //case "gamingsoft":
                //    {
                //        var game = await _dbStorage.Context.Gscgames
                //            .FirstOrDefaultAsync(x => x.Id == id);

                //        if (game == null)
                //            return NotFound(new { success = false, message = "Game not found." });

                //        game.SortNumber = sortNumber;
                //        game.UpdatedAt = DateTime.UtcNow;
                //        break;
                //    }

                default:
                    return BadRequest(new { success = false, message = "Unsupported API." });
            }

            await _dbStorage.Context.SaveChangesAsync();

            return Json(new { success = true, message = "Sort number updated." });
        }
    }
}
