using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LegalConnect.Web.Data;
using LegalConnect.Web.Models.Entities;
using LegalConnect.Web.Models.ViewModels;
using LegalConnect.Web.Services;

namespace LegalConnect.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly LegalConnectDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILawyerService _lawyerService;
        private readonly IAppointmentService _appointmentService;

        public AdminController(
            LegalConnectDbContext context,
            UserManager<ApplicationUser> userManager,
            ILawyerService lawyerService,
            IAppointmentService appointmentService)
        {
            _context = context;
            _userManager = userManager;
            _lawyerService = lawyerService;
            _appointmentService = appointmentService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var totalLawyers = await _context.LawyerProfiles.CountAsync();
            var pendingVerifications = await _context.LawyerProfiles.CountAsync(l => l.VerificationStatus == VerificationStatus.Pending);
            var totalAppointments = await _context.Appointments.CountAsync();

            var recentUsers = await _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .ToListAsync();

            var pendingLawyers = await _lawyerService.GetPendingVerificationLawyersAsync();

            var recentAppointments = await _context.Appointments
                .Include(a => a.Client).ThenInclude(c => c.User)
                .Include(a => a.Lawyer).ThenInclude(l => l.User)
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .ToListAsync();

            var vm = new AdminDashboardViewModel
            {
                TotalUsers = totalUsers,
                TotalLawyers = totalLawyers,
                PendingVerifications = pendingVerifications,
                TotalAppointments = totalAppointments,
                RecentUsers = recentUsers,
                PendingLawyers = pendingLawyers,
                RecentAppointments = recentAppointments
            };

            return View(vm);
        }

        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();
            var list = new List<UserManagementItemViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                list.Add(new UserManagementItemViewModel
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? "",
                    Phone = user.Phone ?? user.PhoneNumber ?? "",
                    Role = roles.FirstOrDefault() ?? "None",
                    CreatedAt = user.CreatedAt,
                    IsActive = user.IsActive
                });
            }

            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // Prevent deactivating own admin account
            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId)
            {
                TempData["ErrorMessage"] = "You cannot deactivate your own administrative account.";
                return RedirectToAction("Users");
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = $"User '{user.FullName}' has been {(user.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction("Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId)
            {
                TempData["ErrorMessage"] = "You cannot delete your own administrative account.";
                return RedirectToAction("Users");
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"User '{user.FullName}' was deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Error deleting user.";
            }

            return RedirectToAction("Users");
        }

        public async Task<IActionResult> Lawyers()
        {
            var lawyers = await _lawyerService.GetAllLawyersAsync();
            return View(lawyers);
        }

        public async Task<IActionResult> VerifyLawyers()
        {
            var pending = await _lawyerService.GetPendingVerificationLawyersAsync();
            return View(pending);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveLawyer(int lawyerId)
        {
            var (success, message) = await _lawyerService.VerifyLawyerAsync(lawyerId, true, null);
            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }
            return RedirectToAction("VerifyLawyers");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectLawyer(int lawyerId, string? reason)
        {
            var (success, message) = await _lawyerService.VerifyLawyerAsync(lawyerId, false, reason);
            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }
            return RedirectToAction("VerifyLawyers");
        }

        public async Task<IActionResult> Appointments()
        {
            var appointments = await _appointmentService.GetAllAppointmentsAsync();
            return View(appointments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int appointmentId, string reason)
        {
            var (success, message) = await _appointmentService.CancelByAdminAsync(appointmentId, reason);
            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }
            return RedirectToAction("Appointments");
        }

        public async Task<IActionResult> Reports()
        {
            ViewBag.TotalUsers = await _userManager.Users.CountAsync();
            ViewBag.TotalLawyers = await _context.LawyerProfiles.CountAsync();
            ViewBag.ApprovedLawyers = await _context.LawyerProfiles.CountAsync(l => l.VerificationStatus == VerificationStatus.Approved);
            ViewBag.PendingLawyers = await _context.LawyerProfiles.CountAsync(l => l.VerificationStatus == VerificationStatus.Pending);

            ViewBag.TotalAppointments = await _context.Appointments.CountAsync();
            ViewBag.PendingAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Pending);
            ViewBag.AcceptedAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Accepted);
            ViewBag.CompletedAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Completed);
            ViewBag.CancelledAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Cancelled || a.Status == AppointmentStatus.Rejected);

            var specCounts = await _context.Specializations
                .Select(s => new { s.Name, Count = s.Lawyers.Count })
                .ToListAsync();

            ViewBag.SpecializationStats = specCounts;

            return View();
        }
    }
}
