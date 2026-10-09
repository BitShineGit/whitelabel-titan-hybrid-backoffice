using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Titan.Repository;

namespace MyStake.Controllers
{
    public class MoreController : Controller
    {
        private readonly ILogger<MoreController> _logger;
        private IWhitelabelDBStorage _dbStorage;
        private readonly IConfiguration _configuration;
        public MoreController(
        ILogger<MoreController> logger,
        IWhitelabelDBStorage dbStorage,
        IConfiguration configuration
        )
        {
            _logger = logger;
            _dbStorage = dbStorage;
            _configuration = configuration;

        }
        public async Task<IActionResult> AboutUs()
        {
            var currencyList = await _dbStorage.Context.Currencies.ToListAsync();
            ViewData["currencylist"] = currencyList;
            return View();
        }

        public async Task<IActionResult> Terms()
        {
            var currencyList = await _dbStorage.Context.Currencies.ToListAsync();
            ViewData["currencylist"] = currencyList;
            return View();
        }

        public async Task<IActionResult> Privacy()
        {
            var currencyList = await _dbStorage.Context.Currencies.ToListAsync();
            ViewData["currencylist"] = currencyList;
            return View();
        }

        public async Task<IActionResult> Responsiblegaming()
        {
            var currencyList = await _dbStorage.Context.Currencies.ToListAsync();
            ViewData["currencylist"] = currencyList;
            return View();
        }
    }
}
