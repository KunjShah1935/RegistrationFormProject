using Microsoft.AspNetCore.Mvc;
using RegistrationFormProject.Data;
using RegistrationFormProject.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using RegistrationFormProject.Services;

namespace RegistrationFormProject.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogger _activityLogger;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext context,
            IActivityLogger activityLogger)
        {
            _logger = logger;
            _context = context;
            _activityLogger = activityLogger;
        }

        public IActionResult Index()
        {
            ViewBag.TotalUsers =
                _context.UserMasters.Count();

            ViewBag.TotalRoles =
                _context.RoleMasters.Count();

            int userId =
                Convert.ToInt32(
                    User.FindFirstValue(
                        ClaimTypes.NameIdentifier));

            bool isAdmin =
                User.IsInRole("Admin");

            if (isAdmin)
            {
                ViewBag.VerifiedDocuments =
                    _context.UserDocuments
                        .Count(x => x.IsVerified);

                ViewBag.PendingDocuments =
                    _context.UserDocuments
                        .Count(x => !x.IsVerified);

                ViewBag.TotalDocuments =
                    _context.UserDocuments.Count();

                ViewBag.UsersAwaitingVerification =
                    _context.UserDocuments
                        .Where(x => !x.IsVerified)
                        .Select(x => x.UserId)
                        .Distinct()
                        .Count();
            }
            else
            {
                bool aadhaarUploaded =
                    _context.UserDocuments.Any(x =>
                        x.UserId == userId &&
                        x.DocumentType == "Aadhaar");

                bool panUploaded =
                    _context.UserDocuments.Any(x =>
                        x.UserId == userId &&
                        x.DocumentType == "PAN");

                bool isVerified =
                    _context.UserDocuments.Any(x =>
                        x.UserId == userId &&
                        x.IsVerified);

                ViewBag.AadhaarUploaded =
                    aadhaarUploaded;

                ViewBag.PanUploaded =
                    panUploaded;

                ViewBag.IsVerified =
                    isVerified;
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // GET: Home/Settings
        public IActionResult Settings()
        {
            var userIdVal = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdVal) || !int.TryParse(userIdVal, out int userId))
            {
                return RedirectToAction("Index", "Login");
            }

            var user = _context.UserMasters.Find(userId);
            if (user == null)
            {
                return RedirectToAction("Index", "Login");
            }

            return View(user);
        }

        // POST: Home/Settings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(UserMaster model)
        {
            var userIdVal = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdVal) || !int.TryParse(userIdVal, out int userId))
            {
                return RedirectToAction("Index", "Login");
            }

            var existingUser = _context.UserMasters.Find(userId);
            if (existingUser == null)
            {
                return RedirectToAction("Index", "Login");
            }

            existingUser.FullName = model.FullName;
            existingUser.EmailID = model.EmailID;
            existingUser.MobileNo = model.MobileNo;
            existingUser.DOB = model.DOB;
            existingUser.Gender = model.Gender;

            // Remove password verification from model state since we're not changing it here
            ModelState.Remove("Password");
            ModelState.Remove("ConfirmPassword");
            ModelState.Remove("Gender");

            if (ModelState.IsValid)
            {
                _context.SaveChanges();
                await _activityLogger.LogAsync("Updated profile settings");
                TempData["Success"] = "Profile updated successfully.";
                return RedirectToAction("Settings");
            }

            return View(existingUser);
        }

        // POST: Home/DeactivateOwnAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateOwnAccount()
        {
            var userIdVal = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdVal) || !int.TryParse(userIdVal, out int userId))
            {
                return RedirectToAction("Index", "Login");
            }

            var user = _context.UserMasters.Find(userId);
            if (user != null)
            {
                if (user.IsSuperAdmin)
                {
                    TempData["Error"] = "Super Admin cannot be deactivated.";
                    return RedirectToAction("Settings");
                }

                if (user.RoleId == 1)
                {
                    // Verify if this is the last active Admin
                    int activeAdminCount = _context.UserMasters
                        .Count(x => x.RoleId == 1 && x.IsActive);

                    if (activeAdminCount <= 1)
                    {
                        TempData["Error"] = "You cannot deactivate your account as you are the last active Admin.";
                        return RedirectToAction("Settings");
                    }
                }

                user.IsActive = false;
                await _context.SaveChangesAsync();

                await _activityLogger.LogAsync(user.UserId, user.UserName, "Deactivated own account");

                // Sign out
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                HttpContext.Session.Clear();

                TempData["Success"] = "Your account has been deactivated successfully.";
                return RedirectToAction("Index", "Login");
            }

            return RedirectToAction("Index", "Login");
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId =
                    Activity.Current?.Id ??
                    HttpContext.TraceIdentifier
            });
        }
    }
}