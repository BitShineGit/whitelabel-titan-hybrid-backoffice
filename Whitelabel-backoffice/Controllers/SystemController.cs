using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Titan.HybridRepository;
using Whitelabel_backoffice.Models;
using Whitelabel_backoffice.Services.Interfaces;

namespace Whitelabel_backoffice.Controllers
{
	public class SystemController : BaseController
	{
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IWhitelabelDBStorage _dbStorage;
        private readonly IConfiguration _configuration;
        private readonly IGameService _gameService;
        private readonly AppSettings _appSetting;
        public Global.Logging.ILogger _logger;



        public SystemController(
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

        public async Task<IActionResult> ApiSetting()
        {
            var apis =
                await _dbStorage.Context.Apis
                    .AsNoTracking()
                    .OrderBy(x => x.Id)
                    .ToListAsync();


            return View(apis);
        }



        [HttpPost]
        [Route("api_setting/get")]
        public async Task<IActionResult> GetApi(int id)
        {
            var api =
                await _dbStorage.Context.Apis
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id);


            if (api == null)
            {
                return Json(new
                {
                    success = false
                });
            }


            return Json(new
            {
                success = true,

                data = new
                {
                    api.Id,
                    api.OpCode,
                    api.ApiCode,
                    api.ApiUrl,
                    api.Status,
                    createdAt = api.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    updatedAt = api.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                }

            });
        }
    }
}
