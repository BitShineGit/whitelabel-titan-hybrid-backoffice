using Microsoft.AspNetCore.Mvc;

namespace Whitelabel_backoffice.Controllers
{
	public class ReportController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
