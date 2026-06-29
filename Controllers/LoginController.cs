using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RegistrationFormProject.Data;
using RegistrationFormProject.Models;
using RegistrationFormProject.Repositories.Interfaces;
using RegistrationFormProject.Services;
using System.Security.Claims;
using Twilio;
using Twilio.Rest.Verify.V2.Service;

namespace RegistrationFormProject.Controllers
{
    public class LoginController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly IUserRepository _repository;

        private readonly IEmailService _emailService;
        private readonly TwilioSettings _twilioSettings;
        private readonly IActivityLogger _activityLogger;

        public LoginController(
            ApplicationDbContext context,
            IUserRepository repository,
            IEmailService emailService,
            IOptions<TwilioSettings> twilioOptions,
            IActivityLogger activityLogger)
        {
            _context = context;
            _repository = repository;
            _emailService = emailService;
            _twilioSettings = twilioOptions.Value;
            _activityLogger = activityLogger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(
    string username,
    string password,
    string captchaInput,
    bool rememberMe)
        {
            string? sessionCaptcha =
                HttpContext.Session.GetString("Captcha");

            if (string.IsNullOrEmpty(sessionCaptcha) ||
                string.IsNullOrEmpty(captchaInput) ||
                !sessionCaptcha.Equals(
                    captchaInput,
                    StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Message = "Invalid Captcha";
                return View();
            }

            var user =
                _context.UserMasters
                .FirstOrDefault(x =>
                    x.UserName == username);

            if (user == null)
            {
                await _activityLogger.LogAsync(null, username, "Failed login attempt: invalid username");
                ViewBag.Message = "Invalid Username or Password";
                return View();
            }

            if (user.RoleId == 1 &&
                !user.IsApproved)
            {
                TempData["Error"] =
        "Your Admin account is pending approval.";

                return RedirectToAction("Index");
            }

            if (user.IsSuspended)
            {
                TempData["Error"] =
        "Your account is suspended.Contact Administrator";

                return RedirectToAction("Index");
            }

            if (!user.IsActive)
            {
                TempData["Error"] = "Your account is inactive. Contact Administrator.";
                return RedirectToAction("Index");
            }

            if (BCrypt.Net.BCrypt.Verify(
                password,
                user.Password))
            {
                user.FailedLoginAttempts = 0;

                _context.SaveChanges();

                var claims =
                    new List<Claim>
                    {
                new Claim(
                    ClaimTypes.Name,
                    user.UserName),

                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.UserId.ToString()),

                new Claim(
                    ClaimTypes.Role,
                    user.RoleId == 1
                        ? "Admin"
                        : "User")
                    };

                var identity =
                    new ClaimsIdentity(
                        claims,
                        CookieAuthenticationDefaults.AuthenticationScheme);

                var principal =
                    new ClaimsPrincipal(identity);

                HttpContext.Session.SetString(
                    "UserName",
                    user.UserName);

                HttpContext.Session.SetInt32(
                    "RoleId",
                    user.RoleId);

                HttpContext.Session.SetInt32(
                    "UserId",
                    user.UserId);

                var authProperties =
    new AuthenticationProperties
    {
        IsPersistent = rememberMe,

        ExpiresUtc = rememberMe
            ? DateTimeOffset.UtcNow.AddDays(30)
            : DateTimeOffset.UtcNow.AddMinutes(30)
    };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    authProperties);

                await _activityLogger.LogAsync(user.UserId, user.UserName, "Logged in successfully");

                TempData["Success"] =
                    "Welcome back, " +
                    user.UserName + "!";

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= 5)
            {
                user.IsSuspended = true;
                await _activityLogger.LogAsync(user.UserId, user.UserName, "Account suspended due to too many failed login attempts");
            }
            else
            {
                await _activityLogger.LogAsync(user.UserId, user.UserName, $"Failed login attempt: wrong password. Attempts Left: {5 - user.FailedLoginAttempts}");
            }

            _context.SaveChanges();

            TempData["Error"] =
                user.IsSuspended
                ? "Your account is suspended. Contact Administrator."
                : $"Invalid Username or Password. Attempts Left: {5 - user.FailedLoginAttempts}";

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Logout()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? userName = HttpContext.Session.GetString("UserName");

            if (userId != null)
            {
                await _activityLogger.LogAsync(userId, userName, "Logged out successfully");
            }

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            HttpContext.Session.Clear();

            return RedirectToAction("Index");
        }

         

         
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> SendOtp(
    string username,
    string method)
        {
            try
            {
                var user =
                    await _repository
                        .GetUserByUsernameAsync(username);

                if (user == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Username not found."
                    });
                }

                string otp =
                    new Random()
                    .Next(100000, 999999)
                    .ToString();

                await _repository.SaveOtpAsync(
                    user.UserId,
                    otp,
                    DateTime.Now.AddMinutes(5),
                    method);

                if (method == "Email")
                {
                    await _emailService.SendOtpEmailAsync(
                        user.EmailID,
                        otp);
                }
                else if (method == "Mobile")
                {
                    TwilioClient.Init(
                        _twilioSettings.AccountSid,
                        _twilioSettings.AuthToken);

                    await VerificationResource.CreateAsync(
                        to: "+91" + user.MobileNo,
                        channel: "sms",
                        pathServiceSid:
                            _twilioSettings.VerifyServiceSid);
                }

                HttpContext.Session.SetInt32(
                    "ResetUserId",
                    user.UserId);
                HttpContext.Session.SetString(
    "OtpMethod",
    method);

                await _activityLogger.LogAsync(user.UserId, user.UserName, $"Requested password reset OTP via {method}");

                return Json(new
                {
                    success = true,
                    message = "OTP sent successfully."
                });
            }
            catch (Exception ex)
            {
                string fullErrorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    fullErrorMessage += " | Inner: " + ex.InnerException.Message;
                    if (ex.InnerException.InnerException != null)
                    {
                        fullErrorMessage += " | InnerInner: " + ex.InnerException.InnerException.Message;
                    }
                }
                var errorLog = new ErrorLog
                {
                    UserId = null,
                    UserName = username,
                    ControllerName = "Login",
                    ActionName = "SendOtp",
                    ErrorMessage = fullErrorMessage,
                    StackTrace = ex.StackTrace,
                    LoggedDate = DateTime.Now
                };
                try
                {
                    _context.ErrorLogs.Add(errorLog);
                    await _context.SaveChangesAsync();
                }
                catch { }

                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        [HttpPost]
        public async Task<IActionResult> VerifyOtp(
   string otp)
        {
            try
            {
                int? userId =
                    HttpContext.Session
                    .GetInt32("ResetUserId");

                if (userId == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Session expired."
                    });
                }

                bool isValid;

                string? method =
                    HttpContext.Session.GetString(
                        "OtpMethod");

                var user =
                    await _repository.GetUserByIdAsync(
                        userId.Value);

                if (user == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "User not found."
                    });
                }

                if (method == "Mobile")
                {
                    TwilioClient.Init(
                        _twilioSettings.AccountSid,
                        _twilioSettings.AuthToken);

                    var verification =
                        await VerificationCheckResource
                        .CreateAsync(
                            to: "+91" + user.MobileNo,
                            code: otp,
                            pathServiceSid:
                                _twilioSettings.VerifyServiceSid);

                    isValid =
                        verification.Status == "approved";
                }
                else
                {
                    isValid =
                        await _repository
                            .ValidateOtpAsync(
                                userId.Value,
                                otp);
                }

                if (!isValid)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid or expired OTP."
                    });
                }

                HttpContext.Session.SetString(
                    "VerifiedOtp",
                    otp);

                await _activityLogger.LogAsync(user.UserId, user.UserName, "Successfully verified password reset OTP");

                return Json(new
                {
                    success = true,
                    message = "OTP verified successfully."
                });
            }
            catch (Exception ex)
            {
                string fullErrorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    fullErrorMessage += " | Inner: " + ex.InnerException.Message;
                    if (ex.InnerException.InnerException != null)
                    {
                        fullErrorMessage += " | InnerInner: " + ex.InnerException.InnerException.Message;
                    }
                }
                var errorLog = new ErrorLog
                {
                    UserId = HttpContext.Session.GetInt32("ResetUserId"),
                    UserName = null,
                    ControllerName = "Login",
                    ActionName = "VerifyOtp",
                    ErrorMessage = fullErrorMessage,
                    StackTrace = ex.StackTrace,
                    LoggedDate = DateTime.Now
                };
                try
                {
                    _context.ErrorLogs.Add(errorLog);
                    await _context.SaveChangesAsync();
                }
                catch { }

                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        [HttpPost]
        public async Task<IActionResult> ResetPassword(
    string newPassword,
    string confirmPassword)
        {
            try
            {
                int? userId =
                    HttpContext.Session.GetInt32("ResetUserId");

                string? verifiedOtp =
                    HttpContext.Session.GetString("VerifiedOtp");

                if (userId == null || verifiedOtp == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Session expired."
                    });
                }

                var user = await _repository.GetUserByIdAsync(userId.Value);
                if (user == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "User not found."
                    });
                }

                if (newPassword != confirmPassword)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Passwords do not match."
                    });
                }

                string hashedPassword =
                    BCrypt.Net.BCrypt.HashPassword(
                        newPassword);

                await _repository.UpdatePasswordAsync(
                    userId.Value,
                    hashedPassword);

                await _repository.MarkOtpUsedAsync(
                    userId.Value,
                    verifiedOtp);

                await _activityLogger.LogAsync(user.UserId, user.UserName, "Successfully reset account password");

                HttpContext.Session.Remove(
                    "ResetUserId");

                HttpContext.Session.Remove(
                    "VerifiedOtp");

                return Json(new
                {
                    success = true,
                    message = "Password reset successfully."
                });
            }
            catch (Exception ex)
            {
                string fullErrorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    fullErrorMessage += " | Inner: " + ex.InnerException.Message;
                    if (ex.InnerException.InnerException != null)
                    {
                        fullErrorMessage += " | InnerInner: " + ex.InnerException.InnerException.Message;
                    }
                }
                var errorLog = new ErrorLog
                {
                    UserId = HttpContext.Session.GetInt32("ResetUserId"),
                    UserName = null,
                    ControllerName = "Login",
                    ActionName = "ResetPassword",
                    ErrorMessage = fullErrorMessage,
                    StackTrace = ex.StackTrace,
                    LoggedDate = DateTime.Now
                };
                try
                {
                    _context.ErrorLogs.Add(errorLog);
                    await _context.SaveChangesAsync();
                }
                catch { }

                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }
}