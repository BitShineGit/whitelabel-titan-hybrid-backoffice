using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace MyStake.Attributes
{
    public class AdminOnlyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.User;

            // Check if user is not authenticated OR user is not in Admin role
            if (!user.Identity.IsAuthenticated || !user.IsInRole("Admin"))
            {
                // Return 404 Not Found
                context.Result = new NotFoundResult();
            }

            base.OnActionExecuting(context);
        }
    }
}
