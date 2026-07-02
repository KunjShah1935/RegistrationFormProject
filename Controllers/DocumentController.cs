using Microsoft.AspNetCore.Mvc;
using RegistrationFormProject.Data;
using RegistrationFormProject.Models;
using RegistrationFormProject.Services;
using RegistrationFormProject.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace RegistrationFormProject.Controllers
{
    public class DocumentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogger _activityLogger;
        private readonly ICloudinaryService _cloudinaryService;

        public DocumentController(ApplicationDbContext context, IActivityLogger activityLogger, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _activityLogger = activityLogger;
            _cloudinaryService = cloudinaryService;
        }

        public IActionResult Index()
        {
            int? userId =
                HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction(
                    "Index",
                    "Login");
            }

            var documents = _context.UserDocuments
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.UploadedDate)
                .ToList();

            return View(documents);
        }

        [HttpPost]
        public async Task<IActionResult> UploadDocument(
            IFormFile file,
            string documentType)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Index", "Login");
            }

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file.";
                return RedirectToAction("Index");
            }

            // Secure validation for file
            string? fileError = ValidateUploadedFile(file, documentType);
            if (fileError != null)
            {
                TempData["Error"] = fileError;
                return RedirectToAction("Index");
            }

            if (documentType != "Other")
            {
                bool alreadyExists = _context.UserDocuments.Any(x =>
                    x.UserId == userId &&
                    x.DocumentType == documentType);

                if (alreadyExists)
                {
                    TempData["Error"] = documentType + " document already uploaded.";
                    return RedirectToAction("Index");
                }
            }

            await SaveDocumentAsync(file, userId.Value, documentType);

            await _activityLogger.LogAsync("Uploaded document of type: " + documentType);

            TempData["Success"] = "Document uploaded successfully.";
            return RedirectToAction("Index");
        }

        private async Task SaveDocumentAsync(
            IFormFile file,
            int userId,
            string documentType)
        {
            var uploadResult = await _cloudinaryService.UploadPdfAsync(file);

            UserDocument document =
                new UserDocument
                {
                    UserId = userId,
                    DocumentType = documentType,
                    FileName = file.FileName,
                    FilePath = file.FileName,
                    CloudinaryUrl = uploadResult.SecureUrl,
                    CloudinaryPublicId = uploadResult.PublicId,
                    UploadedDate = DateTime.Now
                };

            _context.UserDocuments.Add(document);

            await _context.SaveChangesAsync();
        }

        public async Task<IActionResult> ViewDocument(int id)
        {
            var doc =
                await _context.UserDocuments
                .FirstOrDefaultAsync(x => x.DocumentId == id);

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
                    return File(memoryStream, "application/pdf");
                }
            }

            string path =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/uploads",
                    doc.FilePath);

            if (!System.IO.File.Exists(path))
            {
                return NotFound();
            }

            return PhysicalFile(
                path,
                "application/pdf");
        }

        public async Task<IActionResult> Delete(int id)
        {
            var doc =
                await _context.UserDocuments.FindAsync(id);

            if (doc == null)
            {
                return RedirectToAction("Index");
            }

            if (!string.IsNullOrEmpty(doc.CloudinaryPublicId))
            {
                await _cloudinaryService.DeletePdfAsync(doc.CloudinaryPublicId);
            }

            string path =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    doc.FilePath);

            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }

            string documentType = doc.DocumentType;

            _context.UserDocuments.Remove(doc);

            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync("Deleted document of type: " + documentType);

            TempData["Success"] =
                "Document deleted successfully.";

            return RedirectToAction("Index");
        }
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var doc =
                await _context.UserDocuments
                .FirstOrDefaultAsync(x => x.DocumentId == id);

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

            string path =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    doc.FilePath);

            if (!System.IO.File.Exists(path))
            {
                return NotFound();
            }

            return PhysicalFile(
                path,
                "application/pdf",
                doc.FileName);
        }
        public async Task<IActionResult> PreviewDocument(int id)
        {
            var doc = await _context.UserDocuments
                .FirstOrDefaultAsync(x => x.DocumentId == id);

            if (doc == null)
            {
                return NotFound();
            }

            ViewBag.FilePath = !string.IsNullOrEmpty(doc.CloudinaryUrl)
                ? doc.CloudinaryUrl
                : ("/uploads/" + doc.FilePath);
            ViewBag.FileName = doc.FileName;

            return View();
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