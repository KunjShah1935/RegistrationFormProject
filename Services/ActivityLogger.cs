using Microsoft.AspNetCore.Http;
using RegistrationFormProject.Data;
using RegistrationFormProject.Models;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RegistrationFormProject.Services
{
    public interface IActivityLogger
    {
        Task LogAsync(string activity);
        Task LogAsync(int? userId, string? userName, string activity);
    }

    public class ActivityLogger : IActivityLogger
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ActivityLogger(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string activity)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            int? userId = null;
            string? userName = null;
            string? ipAddress = null;

            if (httpContext != null)
            {
                var user = httpContext.User;
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
                    userId = httpContext.Session.GetInt32("UserId");
                    userName = httpContext.Session.GetString("UserName");
                }

                ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            }

            await LogInternalAsync(userId, userName, activity, ipAddress);
        }

        public async Task LogAsync(int? userId, string? userName, string activity)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            string? ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
            await LogInternalAsync(userId, userName, activity, ipAddress);
        }

        private async Task LogInternalAsync(int? userId, string? userName, string activity, string? ipAddress)
        {
            var log = new ActivityLog
            {
                UserId = userId,
                UserName = userName,
                Activity = activity,
                IpAddress = ipAddress,
                LoggedDate = DateTime.Now
            };

            try
            {
                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Suppress database write errors
            }
        }
    }
}
