using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RegistrationFormProject.Data;
using RegistrationFormProject.Models;
using RegistrationFormProject.Services;
using System.Security.Claims;

namespace RegistrationFormProject.Controllers
{
    [Authorize]
    public class VerificationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogger _activityLogger;

        public VerificationController(ApplicationDbContext context, IActivityLogger activityLogger)
        {
            _context = context;
            _activityLogger = activityLogger;
        }

        // GET: Verification/Status
        public IActionResult Status()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return RedirectToAction("Index", "Login");
            }

            var documents = _context.UserDocuments
                .Where(d => d.UserId == userId)
                .OrderBy(d => d.DocumentType)
                .ToList();

            var user = _context.UserMasters.Find(userId);
            ViewBag.IsProfileVerified = user?.IsProfileVerified ?? false;

            return View(documents);
        }

        // POST: Verification/Reupload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reupload(int documentId, IFormFile file)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return RedirectToAction("Index", "Login");
            }

            var document = await _context.UserDocuments.FindAsync(documentId);
            if (document == null || document.UserId != userId)
            {
                TempData["Error"] = "Document not found or access denied.";
                return RedirectToAction("Status");
            }

            if (!document.NeedsReupload)
            {
                TempData["Error"] = "This document does not require re-upload.";
                return RedirectToAction("Status");
            }

            // Secure validation for file
            string? fileError = ValidateUploadedFile(file, document.DocumentType);
            if (fileError != null)
            {
                TempData["Error"] = fileError;
                return RedirectToAction("Status");
            }

            try
            {
                // Delete old file
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!string.IsNullOrEmpty(document.FilePath))
                {
                    string oldFilePath = Path.Combine(uploadsFolder, document.FilePath);
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Save new file
                string uniqueFileName = Guid.NewGuid().ToString() + ".pdf";
                string newFilePath = Path.Combine(uploadsFolder, uniqueFileName);

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                using (var fileStream = new FileStream(newFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // Update database record
                document.FileName = file.FileName;
                document.FilePath = uniqueFileName;
                document.UploadedDate = DateTime.Now;
                document.IsVerified = false;
                document.NeedsReupload = false;
                document.ReuploadReason = null;

                await _context.SaveChangesAsync();

                await _activityLogger.LogAsync($"Re-uploaded document of type: {document.DocumentType}");

                TempData["Success"] = $"{document.DocumentType} document re-uploaded successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("Status");
        }

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
                        return $"{displayName} has an invalid PDF file signature.";
                    }
                }
            }
            catch (Exception)
            {
                return $"Unable to verify file signature for {displayName}.";
            }

            return null;
        }
    }
}
