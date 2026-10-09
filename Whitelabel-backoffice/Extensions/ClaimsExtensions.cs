using System.Security.Claims;

namespace Whitelabel_backoffice.Extensions
{
    public static class ClaimsExtensions
    {

        public static string GetRole(
            this ClaimsPrincipal user)
        {

            return user.Claims
                .FirstOrDefault(
                    x => x.Type == "Role")
                ?.Value;

        }



        public static string GetTheme(
            this ClaimsPrincipal user)
        {

            return user.Claims
                .FirstOrDefault(
                    x => x.Type == "Theme")
                ?.Value;

        }

    }
}
