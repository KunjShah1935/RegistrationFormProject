using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RegistrationFormProject.Data;
using RegistrationFormProject.Models;
using System;
using System.Security.Claims;

namespace RegistrationFormProject.Filters
{
    public class ExceptionLogFilter : IExceptionFilter
    {
        private readonly ApplicationDbContext _context;

        public ExceptionLogFilter(ApplicationDbContext context)
        {
            _context = context;
        }

        public void OnException(ExceptionContext context)
        {
            int? userId = null;
            string? userName = null;

            var user = context.HttpContext.User;
            if (user.Identity != null && user.Identity.IsAuthenticated)
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out int id))
                {
                    userId = id;
                }
                userName = user.Identity.Name;
            }

            if (userId == null)
            {
                userId = context.HttpContext.Session.GetInt32("UserId");
                userName = context.HttpContext.Session.GetString("UserName");
            }

            var controllerName = context.RouteData.Values["controller"]?.ToString() ?? "Unknown";
            var actionName = context.RouteData.Values["action"]?.ToString() ?? "Unknown";

            string fullErrorMessage = context.Exception.Message;
            if (context.Exception.InnerException != null)
            {
                fullErrorMessage += " | Inner: " + context.Exception.InnerException.Message;
                if (context.Exception.InnerException.InnerException != null)
                {
                    fullErrorMessage += " | InnerInner: " + context.Exception.InnerException.InnerException.Message;
                }
            }

            var errorLog = new ErrorLog
            {
                UserId = userId,
                UserName = userName,
                ControllerName = controllerName,
                ActionName = actionName,
                ErrorMessage = fullErrorMessage,
                StackTrace = context.Exception.StackTrace,
                LoggedDate = DateTime.Now
            };

            try
            {
                _context.ErrorLogs.Add(errorLog);
                _context.SaveChanges();
            }
            catch
            {
                // Prevent infinite loop if logging fails (e.g., DB is down)
            }

            context.ExceptionHandled = true;

            // Redirect to standard error page
            context.Result = new RedirectToActionResult("Error", "Home", null);
        }
    }
}
