using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BusTrips.Web.CustomMiddleware
{
    public class GlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
        {
            _logger = logger;
        }

        // This method is called when an unhandled exception occurs in the application
        public void OnException(ExceptionContext context)
        {
            _logger.LogError(context.Exception, "Unhandled exception occurred.");

            bool isAjax = context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

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
                    ViewName = "Error" // A generic error page
                };
            }

            context.ExceptionHandled = true;
        }
    }
}
