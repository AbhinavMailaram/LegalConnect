using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LegalConnect.Web.Data;
using LegalConnect.Web.Models.Entities;
using LegalConnect.Web.Models.ViewModels;
using LegalConnect.Web.Services;

namespace LegalConnect.Web.Controllers
{
    [Authorize]
    public class DocumentController : Controller
    {
        private readonly LegalConnectDbContext _context;
        private readonly IFileService _fileService;
        private readonly IWebHostEnvironment _env;

        public DocumentController(
            LegalConnectDbContext context,
            IFileService fileService,
            IWebHostEnvironment env)
        {
            _context = context;
            _fileService = fileService;
            _env = env;
        }

        public async Task<IActionResult> Index(int? appointmentId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isAdmin = User.IsInRole("Admin");

            IQueryable<Document> query = _context.Documents
                .Include(d => d.Client).ThenInclude(c => c.User)
                .Include(d => d.Appointment);

            if (appointmentId.HasValue)
            {
                query = query.Where(d => d.AppointmentId == appointmentId.Value);
            }

            if (!isAdmin)
            {
                query = query.Where(d => d.Client.UserId == currentUserId || 
                    (d.Appointment != null && d.Appointment.Lawyer.UserId == currentUserId));
            }

            var docs = await query.OrderByDescending(d => d.UploadedAt).ToListAsync();
            ViewBag.AppointmentId = appointmentId;

            return View(docs);
        }

        [Authorize(Roles = "Client")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int? appointmentId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a valid file.";
                return RedirectBack(appointmentId);
            }

            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".docx" };
            if (!_fileService.IsValidFileType(file, allowedExtensions))
            {
                TempData["ErrorMessage"] = "Invalid file type. Only PDF, JPG, PNG, and DOCX documents are allowed.";
                return RedirectBack(appointmentId);
            }

            if (!_fileService.IsValidFileSize(file, 5 * 1024 * 1024))
            {
                TempData["ErrorMessage"] = "File size exceeds the 5MB limit.";
                return RedirectBack(appointmentId);
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var client = await _context.ClientProfiles.FirstOrDefaultAsync(c => c.UserId == currentUserId);
            if (client == null) return Unauthorized();

            var savedPath = await _fileService.SaveFileAsync(file, $"client_{client.ClientId}");

            var doc = new Document
            {
                ClientId = client.ClientId,
                AppointmentId = appointmentId,
                FileName = Path.GetFileName(file.FileName),
                FilePath = savedPath,
                FileSizeBytes = file.Length,
                ContentType = file.ContentType,
                UploadedAt = DateTime.UtcNow
            };

            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Document '{doc.FileName}' uploaded successfully.";
            return RedirectBack(appointmentId);
        }

        public async Task<IActionResult> Download(int id)
        {
            var doc = await _context.Documents
                .Include(d => d.Client)
                .Include(d => d.Appointment).ThenInclude(a => a!.Lawyer)
                .FirstOrDefaultAsync(d => d.DocumentId == id);

            if (doc == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isClient = doc.Client.UserId == currentUserId;
            var isLawyer = doc.Appointment != null && doc.Appointment.Lawyer.UserId == currentUserId;
            var isAdmin = User.IsInRole("Admin");

            if (!isClient && !isLawyer && !isAdmin)
            {
                return Forbid();
            }

            var physicalPath = Path.Combine(_env.WebRootPath, doc.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(physicalPath))
            {
                return NotFound("The requested file was not found on server disk.");
            }

            var mimeType = string.IsNullOrEmpty(doc.ContentType) ? "application/octet-stream" : doc.ContentType;
            return PhysicalFile(physicalPath, mimeType, doc.FileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var doc = await _context.Documents
                .Include(d => d.Client)
                .FirstOrDefaultAsync(d => d.DocumentId == id);

            if (doc == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isClient = doc.Client.UserId == currentUserId;
            var isAdmin = User.IsInRole("Admin");

            if (!isClient && !isAdmin) return Forbid();

            _fileService.DeleteFile(doc.FilePath);
            _context.Documents.Remove(doc);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Document deleted successfully.";
            return RedirectBack(doc.AppointmentId);
        }

        private IActionResult RedirectBack(int? appointmentId)
        {
            if (appointmentId.HasValue)
            {
                return RedirectToAction("Details", "Appointment", new { id = appointmentId.Value });
            }
            return RedirectToAction("Index");
        }
    }
}
