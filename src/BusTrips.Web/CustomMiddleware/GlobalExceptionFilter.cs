using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog; // Use Serilog directly

namespace BusTrips.Web.CustomMiddleware
{
    public class GlobalExceptionFilter : IExceptionFilter
    {
        // You can keep ILogger for DI if needed, but Serilog works globally
        private readonly ILogger<GlobalExceptionFilter> _logger;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            var httpContext = context.HttpContext;

            // Get request info
            var path = httpContext.Request.Path;
            var query = httpContext.Request.QueryString.ToString();
            var user = httpContext.User.Identity?.IsAuthenticated == true
                ? httpContext.User.Identity.Name
                : "Anonymous";

            // Compose detailed exception info
            var errorMessage = $@"
                Unhandled Exception Occurred!
                User: {user}
                Path: {path}
                Query: {query}
                Exception: {context.Exception}";

            // Log to Serilog file
            Log.Error(context.Exception, errorMessage);

            // Also log using DI logger if needed
            _logger.LogError(context.Exception, "Unhandled exception occurred for user {User} at {Path}", user, path);

            bool isAjax = httpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (isAjax)
            {
                context.Result = new JsonResult(new
                {
                    success = false,
                    message = "An unexpected error occurred. Please try again later."
                });
            }
            else
            {
                context.Result = new ViewResult
                {
                    ViewName = "Error" // Generic error page
                };
            }

            context.ExceptionHandled = true; // Mark as handled
        }
    }
}
