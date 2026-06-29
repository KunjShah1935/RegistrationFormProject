using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RegistrationFormProject.Data;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RegistrationFormProject.Filters
{
    public class KycVerificationFilter : IAsyncActionFilter
    {
        private readonly ApplicationDbContext _context;

        public KycVerificationFilter(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            if (user.Identity != null && user.Identity.IsAuthenticated)
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;

                if (roleClaim == "User" && int.TryParse(userIdClaim, out int userId))
                {
                    var userMaster = await _context.UserMasters.FindAsync(userId);
                    if (userMaster != null && !userMaster.IsProfileVerified)
                    {
                        var controller = context.RouteData.Values["controller"]?.ToString();
                        var action = context.RouteData.Values["action"]?.ToString();

                 
                        bool isAllowed = (controller == "Verification") ||
                                         (controller == "Login" && action == "Logout") ||
                                         (controller == "Home" && action == "Settings") ||
                                         (controller == "Home" && action == "DeactivateOwnAccount");

                        if (!isAllowed)
                        {
                            context.Result = new RedirectToActionResult("Status", "Verification", null);
                            return;
                        }
                    }
                }
            }

            await next();
        }
    }
}
