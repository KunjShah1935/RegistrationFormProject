using Microsoft.AspNetCore.Mvc;
using RegistrationFormProject.Data;
using RegistrationFormProject.Models;
using RegistrationFormProject.Services;

namespace RegistrationFormProject.Controllers
{
    public class DocumentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogger _activityLogger;

        public DocumentController(ApplicationDbContext context, IActivityLogger activityLogger)
        {
            _context = context;
            _activityLogger = activityLogger;
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

            SaveDocument(file, userId.Value, documentType);

            await _activityLogger.LogAsync("Uploaded document of type: " + documentType);

            TempData["Success"] = "Document uploaded successfully.";
            return RedirectToAction("Index");
        }

        private void SaveDocument(
            IFormFile file,
            int userId,
            string documentType)
        {
            string uploadsFolder =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(
                    uploadsFolder);
            }

            string fileName =
                Guid.NewGuid().ToString() +
                Path.GetExtension(file.FileName);

            string fullPath =
                Path.Combine(
                    uploadsFolder,
                    fileName);

            using (var stream =
                   new FileStream(
                       fullPath,
                       FileMode.Create))
            {
                file.CopyTo(stream);
            }

            UserDocument document =
                new UserDocument
                {
                    UserId = userId,
                    DocumentType = documentType,
                    FileName = file.FileName,
                    FilePath = fileName,
                    UploadedDate = DateTime.Now
                };

            _context.UserDocuments.Add(document);

            _context.SaveChanges();
        }

        public IActionResult ViewDocument(int id)
        {
            var doc =
                _context.UserDocuments
                .FirstOrDefault(x => x.DocumentId == id);

            if (doc == null)
            {
                return NotFound();
            }

            string path =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/uploads",
                    doc.FilePath);

            return PhysicalFile(
                path,
                "application/pdf");
        }

        public async Task<IActionResult> Delete(int id)
        {
            var doc =
                _context.UserDocuments.Find(id);

            if (doc == null)
            {
                return RedirectToAction("Index");
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

            _context.SaveChanges();

            await _activityLogger.LogAsync("Deleted document of type: " + documentType);

            TempData["Success"] =
                "Document deleted successfully.";

            return RedirectToAction("Index");
        }
        public IActionResult DownloadDocument(int id)
        {
            var doc =
                _context.UserDocuments
                .FirstOrDefault(x => x.DocumentId == id);

            if (doc == null)
            {
                return NotFound();
            }

            string path =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    doc.FilePath);

            return PhysicalFile(
                path,
                "application/pdf",
                doc.FileName);
        }
        public IActionResult PreviewDocument(int id)
        {
            var doc = _context.UserDocuments
                .FirstOrDefault(x => x.DocumentId == id);

            if (doc == null)
            {
                return NotFound();
            }

            ViewBag.FilePath = "/uploads/" + doc.FilePath;
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