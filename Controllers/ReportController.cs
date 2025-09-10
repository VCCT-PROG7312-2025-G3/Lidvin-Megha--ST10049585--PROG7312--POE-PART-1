using Microsoft.AspNetCore.Mvc;
using PROG7312_POEPART1.Models;
using PROG7312_POEPART1.Services;

namespace PROG7312_POEPART1.Controllers
{
    public class ReportController : Controller
    {
        private readonly IssueStore _store;
        private readonly IWebHostEnvironment _env;

        // Define categories as a constant to avoid duplication
        private static readonly string[] Categories = { "Sanitation", "Roads", "Utilities", "Potholes", "Streetlight", "Other" };

        // Define allowed file extensions and max size
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx" };
        private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

        public ReportController(IssueStore store, IWebHostEnvironment env)
        {
            _store = store;
            _env = env;
        }

        // GET: Display the form
        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.Categories = Categories;
            return View(new Issue());
        }

        // POST: Process the form submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(Issue model, IFormFile? media)
        {
            ViewBag.Categories = Categories;

            if (!ModelState.IsValid)
            {
                TempData["Feedback"] = "Please fix the form errors and try again.";
                return View(model);
            }

            // Handle file upload if provided
            if (media != null && media.Length > 0)
            {
                var validationResult = ValidateFile(media);
                if (!validationResult.IsValid)
                {
                    ModelState.AddModelError("media", validationResult.ErrorMessage);
                    TempData["Feedback"] = "File upload error: " + validationResult.ErrorMessage;
                    return View(model);
                }

                try
                {
                    var fileName = await SaveFileAsync(media);
                    model.MediaFileName = fileName;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("media", "Failed to save file.");
                    TempData["Feedback"] = "Failed to upload file. Please try again.";
                    // Log the exception here
                    return View(model);
                }
            }

            _store.Add(model);
            TempData["SuccessMessage"] = "Thank you — your issue has been submitted!";
            return RedirectToAction("Success");
        }

        public IActionResult Success()
        {
            return View();
        }

        private (bool IsValid, string ErrorMessage) ValidateFile(IFormFile file)
        {
            // Check file size
            if (file.Length > MaxFileSize)
            {
                return (false, $"File size cannot exceed {MaxFileSize / 1024 / 1024}MB.");
            }

            // Check file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return (false, $"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", AllowedExtensions)}");
            }

            // Check for empty filename
            if (string.IsNullOrWhiteSpace(file.FileName))
            {
                return (false, "File must have a valid name.");
            }

            return (true, string.Empty);
        }

        private async Task<string> SaveFileAsync(IFormFile file)
        {
            var uploads = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploads))
                Directory.CreateDirectory(uploads);

            // Create safe filename with timestamp and GUID
            var extension = Path.GetExtension(file.FileName);
            var safeFileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploads, safeFileName);

            using var stream = System.IO.File.Create(filePath);
            await file.CopyToAsync(stream);

            return safeFileName;
        }
    }
}
