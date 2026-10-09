using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Titan.Repository;

namespace MyStake.Controllers
{
    public class PromotionsController : Controller
    {
        private readonly ILogger<PromotionsController> _logger;
        private IWhitelabelDBStorage _dbStorage;
        private readonly IConfiguration _configuration;
        public PromotionsController(
        ILogger<PromotionsController> logger,
        IWhitelabelDBStorage dbStorage,
        IConfiguration configuration
        )
        {
            _logger = logger;
            _dbStorage = dbStorage;
            _configuration = configuration;

        }
        public async Task<IActionResult> Index()
        {
            var currencyList = await _dbStorage.Context.Currencies.ToListAsync();
            ViewData["currencylist"] = currencyList;
            return View();
        }
    }
}
