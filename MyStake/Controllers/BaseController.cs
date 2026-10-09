using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Globalization;

namespace MyStake.Controllers
{
    public class BaseController : Controller
    {
        public IStringLocalizer<SharedResource> Lang;

        public BaseController(
                IStringLocalizer<SharedResource> lang)
        {
            Lang = lang;
        }

        public IActionResult ChangeLanguage(string lang)
        {
            var names = typeof(Program).Assembly.GetManifestResourceNames();
            foreach (var name in names)
            {
                Console.WriteLine(name);
            }

            HttpContext myContext = this.HttpContext;
            ChangeLanguage_SetCookie(myContext, lang);

            return Json(new
            {
                status = 1,
            });
        }
        private void ChangeLanguage_SetCookie(HttpContext myContext, string culture)
        {
            if (culture == null) { throw new Exception("culture == null"); };

            //this code sets .AspNetCore.Culture cookie
            myContext.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddMonths(1) }
            );
        }

        public string GetLanguage()
        {
            string lang = CultureInfo.CurrentUICulture.Name;
            return lang == "en" ? lang : lang.Split('-')[0];
        }
    }
}
