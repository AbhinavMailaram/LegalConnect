using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LegalConnect.Web.Data;
using LegalConnect.Web.Models;
using LegalConnect.Web.Models.Entities;
using LegalConnect.Web.Services;

namespace LegalConnect.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly LegalConnectDbContext _context;
        private readonly INotificationService _notificationService;

        public HomeController(LegalConnectDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            var specializations = await _context.Specializations.OrderBy(s => s.Name).Take(8).ToListAsync();
            var verifiedLawyerCount = await _context.LawyerProfiles.CountAsync(l => l.VerificationStatus == VerificationStatus.Approved && l.User.IsActive);
            var completedConsultationsCount = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Completed);

            ViewBag.Specializations = specializations;
            ViewBag.VerifiedLawyersCount = verifiedLawyerCount;
            ViewBag.ConsultationsCount = completedConsultationsCount;

            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Contact(string name, string email, string subject, string message)
        {
            TempData["SuccessMessage"] = "Thank you for reaching out! Our team has received your message and will respond shortly.";
            return RedirectToAction("Contact");
        }

        [HttpGet]
        public async Task<IActionResult> Notifications()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var notifications = await _notificationService.GetUserNotificationsAsync(userId, 50);
            await _notificationService.MarkAllAsReadAsync(userId);

            return View(notifications);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
