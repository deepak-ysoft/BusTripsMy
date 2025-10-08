using BusTrips.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BusTrips.Web.CustomMiddleware
{
    public class RequireWizardCompletionFilter : IAsyncActionFilter
    {
        private readonly UserManager<AppUser> _users;

        public RequireWizardCompletionFilter(UserManager<AppUser> users)
        {
            _users = users;
        }

        // This method checks if the user has completed the initial setup wizard
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = await _users.GetUserAsync(context.HttpContext.User);

            if (user != null && user.IsFirstLogin && !user.WizardCompleted)
            {
                var userRoles = await _users.GetRolesAsync(user);

                // Only apply wizard redirect for users with role "User"
                if (userRoles.Contains("User"))
                {
                    var controller = context.RouteData.Values["controller"]?.ToString();
                    var action = context.RouteData.Values["action"]?.ToString();

                    // allow these routes
                    var allowedControllers = new[] { "FirstTime", "Account" };
                    if (!allowedControllers.Contains(controller))
                    {
                        context.Result = new RedirectToActionResult("Wizard", "FirstTime", null);
                        return;
                    }
                }
            }
            await next();
        }
    }
}
