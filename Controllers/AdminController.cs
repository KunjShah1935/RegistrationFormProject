using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RegistrationFormProject.Data;
using RegistrationFormProject.Models;
using RegistrationFormProject.Repositories.Interfaces;
using RegistrationFormProject.ViewModels;
using RegistrationFormProject.Services;

namespace RegistrationFormProject.Controllers
{
    public class AdminController : Controller
    {
        private readonly DapperContext _dapper;
        private readonly ApplicationDbContext _context;
        private readonly IUserRepository _userRepository;
        private readonly IActivityLogger _activityLogger;

        public AdminController(
            ApplicationDbContext context,
            DapperContext dapper,
            IUserRepository userRepository,
            IActivityLogger activityLogger)
        {
            _context = context;
            _dapper = dapper;
            _userRepository = userRepository;
            _activityLogger = activityLogger;
        }

        public override void OnActionExecuting(
        Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (roleId != 1)
            {
                context.Result = RedirectToAction("Index", "Login");
            }

            base.OnActionExecuting(context);
        }
        public async Task<IActionResult> UserList()
        {
            var users =
                await _userRepository
                    .GetAllUsersAsync();
                
            return View(users);
        }
        public async Task<IActionResult> Delete(int id)
        {
            var user = _context.UserMasters
                .FirstOrDefault(x => x.UserId == id);

            if (user == null)
                return RedirectToAction("UserList");

            if (user.IsSuperAdmin)
            {
                TempData["Error"] = "Super Admin cannot be inactivated.";
                return RedirectToAction("UserList");
            }

            if (user.RoleId == 1 && user.IsActive)
            {
                // Check if this is the last active Admin
                int activeAdminCount = _context.UserMasters
                    .Count(x => x.RoleId == 1 && x.IsActive);

                if (activeAdminCount <= 1)
                {
                    TempData["Error"] = "Cannot deactivate the last active Admin.";
                    return RedirectToAction("UserList");
                }
            }

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync($"Toggled active status of user '{user.UserName}' to {(user.IsActive ? "Active" : "Inactive")}");

            TempData["Success"] = $"Account status for {user.UserName} updated successfully to {(user.IsActive ? "Active" : "Inactive")}.";
            return RedirectToAction("UserList");
        }
        public IActionResult Edit(int id)
        {
            var user = _context.UserMasters.Find(id);

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(UserMaster user)
        {
            var existingUser = _context.UserMasters.Find(user.UserId);

            if (existingUser != null)
            {
                existingUser.FullName = user.FullName;
                existingUser.UserName = user.UserName;
                existingUser.EmailID = user.EmailID;
                existingUser.MobileNo = user.MobileNo;
                existingUser.DOB = user.DOB;
                existingUser.RoleId = user.RoleId;
                existingUser.Gender = user.Gender;

                await _context.SaveChangesAsync();
                await _activityLogger.LogAsync($"Admin updated profile details for user: {user.UserName}");
            }

            return RedirectToAction("UserList");
        }
        public IActionResult RoleList()
        {
            using var connection =
                _dapper.CreateConnection();

            string sql =
                @"SELECT *
          FROM ""RoleMasters""
          ORDER BY ""RoleId""";

            var roles =
                connection.Query<RoleMaster>(sql)
                          .ToList();

            return View(roles);
        }
        public async Task<IActionResult> DeleteRole(int RoleId)
        {
            var role = _context.RoleMasters.Find(RoleId);

            if (role != null)
            {
                string roleName = role.RoleName;
                _context.RoleMasters.Remove(role);

                await _context.SaveChangesAsync();
                await _activityLogger.LogAsync($"Admin deleted role: {roleName}");
            }

            return RedirectToAction("RoleList");
        }

        public async Task<IActionResult> EditRole(int RoleId)
        {
            var role = _context.RoleMasters.Find(RoleId);

            return View(role);
        }

        [HttpPost]
        public async Task<IActionResult> EditRole(RoleMaster role)
        {
            var existingUser = _context.RoleMasters.Find(role.RoleId);

            if (existingUser != null)
            {
                existingUser.RoleId = role.RoleId;

                existingUser.RoleName = role.RoleName;

                await _context.SaveChangesAsync();
                await _activityLogger.LogAsync($"Admin updated role: {role.RoleName}");
            }

            return RedirectToAction("RoleList");
        }

        public IActionResult AddRole()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddRole(RoleMaster role)
        {
            if (ModelState.IsValid)
            {
                _context.RoleMasters.Add(role);
                await _context.SaveChangesAsync();

                await _activityLogger.LogAsync($"Admin added role: {role.RoleName}");

                TempData["Success"] =
                    "Role Added Successfully";

                return RedirectToAction("RoleList");
            }

            return View(role);
        }

        public IActionResult AddUser()
        {
            ViewBag.Roles = new SelectList(
        _context.RoleMasters.ToList(),
        "RoleId",
        "RoleName");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddUser(UserMaster user)
        {
            if (ModelState.IsValid)
            {
                user.CreatedDate = DateTime.Now;
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

                _context.UserMasters.Add(user);
                await _context.SaveChangesAsync();

                await _activityLogger.LogAsync($"Admin created user account: {user.UserName}");

                TempData["Success"] =
                    "User Added Successfully";

                return RedirectToAction("UserList");
            }
            ViewBag.Roles = new SelectList(
        _context.RoleMasters.ToList(),
        "RoleId",
        "RoleName");

            return View(user);
        }

        public IActionResult DocumentVerification()
        {
            var documents =
                _context.UserDocuments
                .Join(
                    _context.UserMasters,
                    d => d.UserId,
                    u => u.UserId,
                    (d, u) => new DocumentVerificationVM
                    {
                        DocumentId = d.DocumentId,
                        FullName = u.FullName,
                        DocumentType = d.DocumentType,
                        FileName = d.FileName,
                        IsVerified = d.IsVerified,
                        UploadedDate = d.UploadedDate
                    })
                .OrderBy(x => x.IsVerified)
                .ToList();

            return View(documents);
        }
        public async Task<IActionResult> VerifyDocument(int id)
        {
            var document = _context.UserDocuments.FirstOrDefault(x => x.DocumentId == id);

            if (document != null)
            {
                document.IsVerified = true;
                document.NeedsReupload = false;
                document.ReuploadReason = null;
                document.VerifiedDate = DateTime.Now;
                document.VerifiedBy = HttpContext.Session.GetInt32("UserId");

                await _context.SaveChangesAsync();
                CheckAndVerifyUserProfile(document.UserId);

                await _activityLogger.LogAsync($"Admin approved document of type '{document.DocumentType}' for UserId: {document.UserId}");

                TempData["Success"] = "Document verified successfully.";
                return RedirectToAction("ReviewUserDocuments", new { id = document.UserId });
            }

            return RedirectToAction("VerificationDashboard");
        }
        public IActionResult PendingAdmins()
        {
            var users =
                _context.UserMasters
                .Where(x =>
                    x.RoleId == 1 &&
                    !x.IsApproved)
                .ToList();

            return View(users);
        }
        public async Task<IActionResult> ApproveAdmin(int id)
        {
            var user = _context.UserMasters.Find(id);
            if (user != null)
            {
                user.IsApproved = true;
                await _context.SaveChangesAsync();

                await _activityLogger.LogAsync($"Admin approved pending admin account: {user.UserName}");
            }

            return RedirectToAction("PendingAdmins");
        }

        public IActionResult SuspendedUsers()
        {
            return View(
                _context.UserMasters
                .Where(x => x.IsSuspended)
                .ToList());
        }
        public IActionResult ReviewUserDocuments(int id)
        {
            var documents = _context.UserDocuments
                .Where(d => d.UserId == id)
                .Join(
                    _context.UserMasters,
                    d => d.UserId,
                    u => u.UserId,
                    (d, u) => new DocumentVerificationVM
                    {
                        DocumentId = d.DocumentId,
                        FullName = u.FullName,
                        DocumentType = d.DocumentType,
                        FileName = d.FileName,
                        IsVerified = d.IsVerified,
                        UploadedDate = d.UploadedDate,
                        NeedsReupload = d.NeedsReupload,
                        ReuploadReason = d.ReuploadReason
                    })
                .ToList();

            var user = _context.UserMasters.Find(id);
            ViewBag.UserId = id;
            ViewBag.FullName = user?.FullName;

            return View(documents);
        }
        public async Task<IActionResult> UnlockUser(int id)
        {
            var user = _context.UserMasters.Find(id);
            if (user != null)
            {
                user.IsSuspended = false;
                user.FailedLoginAttempts = 0;
                await _context.SaveChangesAsync();

                await _activityLogger.LogAsync($"Admin unlocked suspended user account: {user.UserName}");
            }

            return RedirectToAction("SuspendedUsers");
        }

        public async Task<IActionResult>
    VerificationDashboard()
        {
            var users =
                await _userRepository
                    .GetVerificationDashboardAsync();

            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> RequestReupload(int documentId, string reason)
        {
            var document = _context.UserDocuments.FirstOrDefault(x => x.DocumentId == documentId);
            if (document != null)
            {
                document.NeedsReupload = true;
                document.ReuploadReason = reason;
                document.IsVerified = false;

                var user = _context.UserMasters.Find(document.UserId);
                if (user != null)
                {
                    user.IsProfileVerified = false;
                }

                await _context.SaveChangesAsync();

                await _activityLogger.LogAsync($"Admin requested document re-upload for UserId: {document.UserId}, Type: {document.DocumentType} (Reason: {reason})");

                TempData["Success"] = "Re-upload request sent successfully.";
                return RedirectToAction("ReviewUserDocuments", new { id = document.UserId });
            }
            return RedirectToAction("VerificationDashboard");
        }

        public IActionResult PreviewDocument(int id)
        {
            var doc = _context.UserDocuments.FirstOrDefault(x => x.DocumentId == id);
            if (doc == null)
            {
                return NotFound();
            }

            ViewBag.FilePath = !string.IsNullOrEmpty(doc.CloudinaryUrl)
                ? doc.CloudinaryUrl
                : ("/uploads/" + doc.FilePath);
            ViewBag.FileName = doc.FileName;
            return View(doc);
        }

        public async Task<IActionResult> DownloadDocument(int id)
        {
            var doc = await _context.UserDocuments.FirstOrDefaultAsync(x => x.DocumentId == id);
            if (doc == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(doc.CloudinaryUrl))
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    var stream = await httpClient.GetStreamAsync(doc.CloudinaryUrl);
                    var memoryStream = new System.IO.MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;
                    return File(memoryStream, "application/pdf", doc.FileName);
                }
            }

            string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", doc.FilePath);
            if (!System.IO.File.Exists(path))
            {
                return NotFound();
            }

            return PhysicalFile(path, "application/pdf", doc.FileName);
        }

        private void CheckAndVerifyUserProfile(int userId)
        {
            var user = _context.UserMasters.Find(userId);
            if (user != null)
            {
                var documents = _context.UserDocuments.Where(d => d.UserId == userId).ToList();
                var aadhaar = documents.FirstOrDefault(d => d.DocumentType == "Aadhaar");
                var pan = documents.FirstOrDefault(d => d.DocumentType == "PAN");

                bool hasPendingReupload = documents.Any(d => d.NeedsReupload);
                if (aadhaar != null && aadhaar.IsVerified && pan != null && pan.IsVerified && !hasPendingReupload)
                {
                    user.IsProfileVerified = true;
                }
                else
                {
                    user.IsProfileVerified = false;
                }
                _context.SaveChanges();
            }
        }

        public IActionResult ErrorLogs()
        {
            var logs = _context.ErrorLogs.OrderByDescending(x => x.LoggedDate).ToList();
            return View(logs);
        }

        public IActionResult ActivityLogs()
        {
            var logs = _context.ActivityLogs.OrderByDescending(x => x.LoggedDate).ToList();
            return View(logs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearErrorLogs()
        {
            _context.ErrorLogs.RemoveRange(_context.ErrorLogs);
            await _context.SaveChangesAsync();
            await _activityLogger.LogAsync("Admin cleared all unhandled error logs");
            TempData["Success"] = "Error logs cleared successfully.";
            return RedirectToAction("ErrorLogs");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearActivityLogs()
        {
            _context.ActivityLogs.RemoveRange(_context.ActivityLogs);
            await _context.SaveChangesAsync();
            await _activityLogger.LogAsync("Admin cleared all user activity logs");
            TempData["Success"] = "Activity logs cleared successfully.";
            return RedirectToAction("ActivityLogs");
        }
    }
}