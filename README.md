# PROG7312-POEPART1
This repository contains a minimal, ready-to-run ASP.NET Core MVC application that implements the Reports Issues functionality. The two menu items (Local Events and Announcements, Service Request Status) are present but disabled.

Project Structure
PROG7312-POEPART1/
├─ Controllers/
│  ├─ HomeController.cs
│  └─ ReportController.cs
├─ Models/
│  └─ Issue.cs
├─ Services/
│  └─ IssueStore.cs
├─ Views/
│  ├─ Shared/_Layout.cshtml
│  ├─ Home/Index.cshtml
│  ├─ Report/Index.cshtml
│  └─ Report/Success.cshtml
├─ wwwroot/
│  └─ css/site.css
├─ appsettings.json
├─ PROG7312-POEPART1.csproj
└─ Program.cs

README.md

Project Files

PROG7312-POEPART1.csproj
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>PROG7312_POEPART1</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc" Version="2.3.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.9" />
  </ItemGroup>

</Project>

Program.cs
using PROG7312_POEPART1.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<IssueStore>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

Model/Issue.cs
using System.ComponentModel.DataAnnotations;

namespace PROG7312_POEPART1.Models
{
    public class Issue
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        public string? Location { get; set; }
        [Required]
        public string? Category { get; set; }
        [Required]
        public string? Description { get; set; }
        public string? MediaFileName { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}

Services/IssueService.cs
using PROG7312_POEPART1.Models;

namespace PROG7312_POEPART1.Services
{
    public interface IssueService
    {
        void Add(Issue issue);
        IEnumerable<Issue> GetAll();
    }

    public class IssueStore : IssueService
    {
        private readonly List<Issue> _issues = new();

        public void Add(Issue issue)
        {
            _issues.Add(issue);
        }


        public IEnumerable<Issue> GetAll() => _issues;
    }
}

Controllers/HomeController.cs
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PROG7312_POEPART1.Models;

namespace PROG7312_POEPART1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

Controllers/ReportController.cs
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

Views/Shared/_Layout.cshtml
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - PROG7312_POEPART1</title>
    <link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
    <link rel="stylesheet" href="~/PROG7312_POEPART1.styles.css" asp-append-version="true" />
</head>
<body>
    <header>
        <nav class="navbar navbar-expand-sm navbar-toggleable-sm navbar-light bg-white border-bottom box-shadow mb-3">
            <div class="container-fluid">
                <a class="navbar-brand" asp-area="" asp-controller="Home" asp-action="Index">PROG7312_POEPART1</a>
                <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target=".navbar-collapse" aria-controls="navbarSupportedContent"
                        aria-expanded="false" aria-label="Toggle navigation">
                    <span class="navbar-toggler-icon"></span>
                </button>
                <div class="navbar-collapse collapse d-sm-inline-flex justify-content-between">
                    <ul class="navbar-nav flex-grow-1">
                        <li class="nav-item">
                            <a class="nav-link text-dark" asp-area="" asp-controller="Home" asp-action="Index">Home</a>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link text-dark" asp-area="" asp-controller="Home" asp-action="Privacy">Privacy</a>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link text-dark" asp-area="" asp-controller="Report" asp-action="Index">Report Issues</a>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link text-dark" asp-area="" asp-controller="Report" asp-action="Success">Success</a>
                        </li>
                        <header class="site-header">
                            <h1>Municipal Services — Citizen Portal</h1>
                        </header>
                        <main class="container">
                            @RenderBody()
                        </main>
                        <footer class="site-footer">
                            <small>Municipal Report Issues — Demo App</small>
                        </footer>
                    </ul>
                </div>
            </div>
        </nav>
    </header>
    <div class="container">
        <main role="main" class="pb-3">
            @RenderBody()
        </main>
    </div>

    <footer class="border-top footer text-muted">
        <div class="container">
            &copy; 2025 - PROG7312_POEPART1 - <a asp-area="" asp-controller="Home" asp-action="Privacy">Privacy</a>
        </div>
    </footer>
    <script src="~/lib/jquery/dist/jquery.min.js"></script>
    <script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
    <script src="~/js/site.js" asp-append-version="true"></script>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>

Views/Home/Index.cshtml
@{
    ViewData["Title"] = "Home Page";
}

<div class="text-center">
    <h1 class="display-4">Welcome to Municipal Report Issues</h1>
    <p class="lead">
        This platform is designed to provide citizens with a simple and effective way to interact with their municipality.
        By reporting issues directly online, residents can help improve their community while ensuring transparency and accountability
        in the way service requests are managed.
    </p>
</div>

<div class="card shadow-lg mt-4">
    <div class="card-body">
        <h2 class="card-title">Main Menu</h2>
        <p>
            Please select from the options below. Currently, only the "Report Issues" feature is available, which allows me to submit
            detailed information about challenges such as potholes, water leaks, power outages, and other service-related concerns.
            My reports will be captured and forwarded to the relevant municipal departments for review and resolution.
        </p>
        <ul class="list-unstyled">
            <li class="mb-2">
                <a class="btn btn-primary w-100" asp-controller="Report" asp-action="Index">Report Issues</a>
                <p class="description">
                    Submit new issues in your neighborhood. Provide a description, location details, and optional attachments to
                    help municipal staff resolve the matter quickly.
                </p>
            </li>
            <li class="mb-2">
                <button class="btn btn-secondary w-100" disabled>Local Events & Announcements (Coming soon)</button>
                <p class="description">
                    This feature will allow to stay updated on community activities, municipal announcements, and
                    important local events. It is currently under development.
                </p>
            </li>
            <li class="mb-2">
                <button class="btn btn-secondary w-100" disabled>Service Request Status (Coming soon)</button>
                <p class="description">
                    Once available, this option will let me track the progress of my submitted requests,
                    providing real-time updates and estimated resolution times.
                </p>
            </li>
        </ul>
        <p class="text-muted">Only Report Issues is available for now.</p>
    </div>
</div>

Views/Report/Index.cshtml
@model PROG7312_POEPART1.Models.Issue
@{
    ViewData["Title"] = "Report Issues";
    var categories = ViewBag.Categories as string[] ?? new string[0];
}

<h2>Report an Issue</h2>

@if (TempData["Feedback"] != null)
{
    <div class="alert">@TempData["Feedback"]</div>
}

<form asp-action="Index" method="post" enctype="multipart/form-data">
    <div class="form-row">
        <label>Location</label>
        <input asp-for="Location" />
        <span class="field-validation">@Html.ValidationMessageFor(m => m.Location)</span>
    </div>

    <div class="form-row">
        <label>Category</label>
        <select asp-for="Category">
            <option value="">-- Select Category --</option>
            @foreach (var c in categories)
            {
                <option value="@c">@c</option>
            }
        </select>
        <span class="field-validation">@Html.ValidationMessageFor(m => m.Category)</span>
    </div>

    <div class="form-row">
        <label>Description</label>
        <textarea asp-for="Description" rows="6"></textarea>
        <span class="field-validation">@Html.ValidationMessageFor(m => m.Description)</span>
    </div>

    <div class="form-row">
        <label>Attach image or document</label>
        <input type="file" name="media" />
    </div>

    <div class="form-row">
        <button type="submit" class="btn">Submit</button>
        <a class="btn secondary" href="/">Back to Main Menu</a>
    </div>

    <div class="engagement">
        <label id="encourage">Be a civic hero — thank you for reporting!</label>
        <progress id="reportProgress" max="100" value="25"></progress>
    </div>
</form>

@section Scripts {
    <script>
        // Simple engagement: rotate encouraging messages and animate progress.
        const messages = [
          'Be a civic hero — thank you for reporting!',
          'Your voice matters — keep reporting issues!',
          'Small reports = big improvements in our community!',
          'Thanks! You help make the municipality better.'
        ];
        let idx = 0;
        setInterval(() => {
          idx = (idx + 1) % messages.length;
          document.getElementById('encourage').innerText = messages[idx];
          const p = document.getElementById('reportProgress');
          p.value = 25 + ((idx + 1) * 15) % 100;
        }, 3500);
    </script>
}

Views/Report/Success.cshtml
@{
    ViewData["Title"] = "Success";
}

<h2>Report Submitted</h2>
<p>Thank you — your issue has been submitted. Our team will review it.</p>
<p><a href="/">Back to Main Menu</a></p>

wwwroot/css/site.css
html {
  font-size: 14px;
}

@media (min-width: 768px) {
  html {
    font-size: 16px;
  }
}

.btn:focus, .btn:active:focus, .btn-link.nav-link:focus, .form-control:focus, .form-check-input:focus {
  box-shadow: 0 0 0 0.1rem white, 0 0 0 0.25rem #258cfb;
}

html {
  position: relative;
  min-height: 100%;
}

body {
    margin-bottom: 60px;
    font-family: Arial, sans-serif;
    margin: 0;
    padding: 0;
    background: #f8f9fb;
    color: #222;
}

.site-header {
    background: #2a6f97;
    color: #fff;
    padding: 12px 20px;
}

.container {
    padding: 20px;
    max-width: 900px;
    margin: 0 auto;
}

.menu ul {
    list-style: none;
    padding: 0;
}

.menu li {
    margin: 8px 0;
}

.btn {
    background: #2a6f97;
    color: white;
    border: none;
    padding: 10px 14px;
    text-decoration: none;
    display: inline-block;
    border-radius: 6px;
}

    .btn.secondary {
        background: #6c757d;
    }

    .btn.disabled {
        opacity: 0.6;
    }

.form-row {
    margin-bottom: 12px;
}

    .form-row label {
        display: block;
        margin-bottom: 4px;
        font-weight: bold;
    }

    .form-row input[type="text"], .form-row select, .form-row textarea {
        width: 100%;
        padding: 8px;
        border-radius: 4px;
        border: 1px solid #ccc;
    }

.engagement {
    margin-top: 16px;
}

.alert {
    background: #ffdede;
    padding: 10px;
    border-radius: 4px;
}

.field-validation {
    color: red;
    font-size: 0.9em;
}

appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
        "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*"
}

Setup and Usage
Requirements

.NET 8 SDK (or compatible .NET 7/8)
Visual Studio 2022+ or VS Code

How to Build and Run

Create a new folder and copy all project files (preserve folder structure)
From terminal, cd into PROG7312-POEPART1
Run dotnet restore to restore packages
Run dotnet run to start the app
Open the URL shown in the console (typically http://localhost:7101)
