using Microsoft.AspNetCore.Mvc;
using RegistrationFormProject.Data;
using RegistrationFormProject.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System;
using RegistrationFormProject.Services;

namespace RegistrationFormProject.Controllers
{
    public class RegisterController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogger _activityLogger;

        public RegisterController(ApplicationDbContext context, IActivityLogger activityLogger)
        {
            _context = context;
            _activityLogger = activityLogger;
        }

        //GET
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(
            UserMaster user, 
            string captchaInput,
            IFormFile AadhaarFile,
            IFormFile PanFile,
            IFormFile? PassportFile,
            IFormFile? DrivingLicenseFile,
            IFormFile? VoterIdFile,
            IFormFile? RationCardFile,
            IFormFile? OthersFile)
        {
            string? sessionCaptcha = HttpContext.Session.GetString("Captcha");
            if (string.IsNullOrEmpty(sessionCaptcha) || string.IsNullOrEmpty(captchaInput) || !sessionCaptcha.Equals(captchaInput, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Captcha", "Invalid CAPTCHA security verification code. Please try again.");
                ViewBag.Message = "Invalid Captcha";
                return View(user);
            }

            // Secure validation for files
            string? fileError = ValidateUploadedFile(AadhaarFile, "Aadhaar Card");
            if (fileError != null) ModelState.AddModelError("", fileError);

            fileError = ValidateUploadedFile(PanFile, "PAN Card");
            if (fileError != null) ModelState.AddModelError("", fileError);

            if (PassportFile != null && PassportFile.Length > 0)
            {
                fileError = ValidateUploadedFile(PassportFile, "Passport");
                if (fileError != null) ModelState.AddModelError("", fileError);
            }

            if (DrivingLicenseFile != null && DrivingLicenseFile.Length > 0)
            {
                fileError = ValidateUploadedFile(DrivingLicenseFile, "Driving License");
                if (fileError != null) ModelState.AddModelError("", fileError);
            }

            if (VoterIdFile != null && VoterIdFile.Length > 0)
            {
                fileError = ValidateUploadedFile(VoterIdFile, "Voter ID");
                if (fileError != null) ModelState.AddModelError("", fileError);
            }

            if (RationCardFile != null && RationCardFile.Length > 0)
            {
                fileError = ValidateUploadedFile(RationCardFile, "Ration Card");
                if (fileError != null) ModelState.AddModelError("", fileError);
            }

            if (OthersFile != null && OthersFile.Length > 0)
            {
                fileError = ValidateUploadedFile(OthersFile, "Others Document");
                if (fileError != null) ModelState.AddModelError("", fileError);
            }

            if (ModelState.IsValid)
            {
                var password = user.Password;
                bool hasMinLength = password.Length >= 8;
                bool hasUpper = password.Any(char.IsUpper);
                bool hasLower = password.Any(char.IsLower);
                bool hasDigit = password.Any(char.IsDigit);
                bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

                if (!hasMinLength || !hasUpper || !hasLower || !hasDigit || !hasSpecial)
                {
                    ModelState.AddModelError("Password", "Password does not meet complexity requirements. It must be at least 8 characters long and contain uppercase, lowercase, numerical, and special characters.");
                    ViewBag.Message = "Validation Failed";
                    return View(user);
                }

                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                
                // STEP 3: Save user details. Admin IsApproved = false, User IsApproved = true. IsProfileVerified = false.
                if (user.RoleId == 1)
                {
                    user.IsApproved = false;
                }
                else
                {
                    user.IsApproved = true;
                }
                
                user.IsProfileVerified = false;

                _context.UserMasters.Add(user);
                _context.SaveChanges(); // Persist user to get UserId

                // Save files
                SaveUserDocument(user.UserId, AadhaarFile, "Aadhaar");
                SaveUserDocument(user.UserId, PanFile, "PAN");

                if (PassportFile != null && PassportFile.Length > 0)
                    SaveUserDocument(user.UserId, PassportFile, "Passport");

                if (DrivingLicenseFile != null && DrivingLicenseFile.Length > 0)
                    SaveUserDocument(user.UserId, DrivingLicenseFile, "Driving License");

                if (VoterIdFile != null && VoterIdFile.Length > 0)
                    SaveUserDocument(user.UserId, VoterIdFile, "Voter ID");

                if (RationCardFile != null && RationCardFile.Length > 0)
                    SaveUserDocument(user.UserId, RationCardFile, "Ration Card");

                if (OthersFile != null && OthersFile.Length > 0)
                    SaveUserDocument(user.UserId, OthersFile, "Others");

                _context.SaveChanges();

                await _activityLogger.LogAsync(user.UserId, user.UserName, "User registered successfully");

                TempData["Success"] = "Registration Successful! Profile pending KYC verification.";
                return RedirectToAction("Index", "Login");
            }
            else
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(error.ErrorMessage);
                }

                ViewBag.Message = "Validation Failed";
            }

            return View(user);
        }

        [HttpGet]
        public JsonResult CheckUsername(string username)
        {
            return Json(
                !_context.UserMasters
                .Any(x => x.UserName == username));
        }

        [HttpGet]
        public JsonResult CheckEmail(string email)
        {
            return Json(
                !_context.UserMasters
                .Any(x => x.EmailID == email));
        }

        [HttpGet]
        public JsonResult CheckMobile(string mobile)
        {
            return Json(
                !_context.UserMasters
                .Any(x => x.MobileNo == mobile));
        }

        // Helper methods for secure file validation & storage
        private string? ValidateUploadedFile(IFormFile? file, string displayName)
        {
            if (file == null || file.Length == 0)
            {
                return $"{displayName} is mandatory and cannot be empty.";
            }

            if (file.Length > 2 * 1024 * 1024)
            {
                return $"{displayName} size must not exceed 2 MB.";
            }

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".pdf")
            {
                return $"{displayName} must be a valid PDF file.";
            }

            try
            {
                using (var stream = file.OpenReadStream())
                {
                    if (stream.Length < 4)
                    {
                        return $"{displayName} is not a valid PDF file.";
                    }

                    byte[] buffer = new byte[4];
                    stream.Read(buffer, 0, 4);

                    // PDF Magic number: %PDF (0x25, 0x50, 0x44, 0x46)
                    if (buffer[0] != 0x25 || buffer[1] != 0x50 || buffer[2] != 0x44 || buffer[3] != 0x46)
                    {
                        return $"{displayName} has an invalid PDF file signature (magic number mismatch).";
                    }
                }
            }
            catch (Exception)
            {
                return $"Unable to verify file signature for {displayName}.";
            }

            return null;
        }

        private void SaveUserDocument(int userId, IFormFile file, string documentType)
        {
            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string uniqueFileName = Guid.NewGuid().ToString() + ".pdf";
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }

            var document = new UserDocument
            {
                UserId = userId,
                DocumentType = documentType,
                FileName = file.FileName,
                FilePath = uniqueFileName,
                UploadedDate = DateTime.Now,
                IsVerified = false,
                NeedsReupload = false,
                ReuploadReason = null
            };

            _context.UserDocuments.Add(document);
        }
    }
}
