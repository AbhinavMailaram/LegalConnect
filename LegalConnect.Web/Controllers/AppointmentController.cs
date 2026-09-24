using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LegalConnect.Web.Data;
using LegalConnect.Web.Models.Entities;
using LegalConnect.Web.Models.ViewModels;
using LegalConnect.Web.Services;

namespace LegalConnect.Web.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly IAppointmentService _appointmentService;
        private readonly ILawyerService _lawyerService;
        private readonly LegalConnectDbContext _context;

        public AppointmentController(
            IAppointmentService appointmentService,
            ILawyerService lawyerService,
            LegalConnectDbContext context)
        {
            _appointmentService = appointmentService;
            _lawyerService = lawyerService;
            _context = context;
        }

        [Authorize(Roles = "Client")]
        [HttpGet]
        public async Task<IActionResult> Book(int lawyerId)
        {
            var lawyer = await _lawyerService.GetLawyerByIdAsync(lawyerId);
            if (lawyer == null || lawyer.VerificationStatus != VerificationStatus.Approved)
            {
                TempData["ErrorMessage"] = "The requested lawyer is not available for appointments.";
                return RedirectToAction("FindLawyers", "Client");
            }

            var tomorrow = DateTime.Today.AddDays(1);
            var vm = new BookAppointmentViewModel
            {
                LawyerId = lawyer.LawyerId,
                LawyerName = lawyer.User.FullName,
                SpecializationName = lawyer.Specialization?.Name ?? "General Practice",
                ConsultationFee = lawyer.ConsultationFee,
                Location = lawyer.Location,
                AppointmentDate = tomorrow,
                AvailableSlots = lawyer.Availabilities
                    .Where(a => a.IsAvailable)
                    .OrderBy(a => a.DayOfWeek)
                    .ThenBy(a => a.StartTime)
                    .Select(a => new AvailabilitySlotItem
                    {
                        DayOfWeek = a.DayOfWeek,
                        StartTime = a.StartTime,
                        EndTime = a.EndTime,
                        IsAvailable = a.IsAvailable
                    }).ToList()
            };

            return View(vm);
        }

        [Authorize(Roles = "Client")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(BookAppointmentViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var client = await _context.ClientProfiles.FirstOrDefaultAsync(c => c.UserId == userId);
            if (client == null)
            {
                TempData["ErrorMessage"] = "Client profile not found. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            var lawyer = await _lawyerService.GetLawyerByIdAsync(model.LawyerId);
            if (lawyer == null) return NotFound();

            if (!TimeSpan.TryParse(model.AppointmentTimeString, out var timeSlot))
            {
                ModelState.AddModelError("AppointmentTimeString", "Please enter a valid time (e.g. 10:00).");
            }

            if (!ModelState.IsValid)
            {
                model.LawyerName = lawyer.User.FullName;
                model.SpecializationName = lawyer.Specialization?.Name ?? "General Practice";
                model.ConsultationFee = lawyer.ConsultationFee;
                model.Location = lawyer.Location;
                model.AvailableSlots = lawyer.Availabilities
                    .Where(a => a.IsAvailable)
                    .Select(a => new AvailabilitySlotItem
                    {
                        DayOfWeek = a.DayOfWeek,
                        StartTime = a.StartTime,
                        EndTime = a.EndTime,
                        IsAvailable = a.IsAvailable
                    }).ToList();
                return View(model);
            }

            var (success, message, appointment) = await _appointmentService.CreateAppointmentRequestAsync(
                client.ClientId,
                model.LawyerId,
                model.AppointmentDate,
                timeSlot,
                model.CaseTitle,
                model.CaseDescription,
                model.ConsultationMode
            );

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                model.LawyerName = lawyer.User.FullName;
                model.SpecializationName = lawyer.Specialization?.Name ?? "General Practice";
                model.ConsultationFee = lawyer.ConsultationFee;
                model.Location = lawyer.Location;
                model.AvailableSlots = lawyer.Availabilities
                    .Where(a => a.IsAvailable)
                    .Select(a => new AvailabilitySlotItem
                    {
                        DayOfWeek = a.DayOfWeek,
                        StartTime = a.StartTime,
                        EndTime = a.EndTime,
                        IsAvailable = a.IsAvailable
                    }).ToList();
                return View(model);
            }

            TempData["SuccessMessage"] = "Appointment consultation request submitted successfully! Adv. " + lawyer.User.FullName + " will review and confirm.";
            return RedirectToAction("MyAppointments", "Client");
        }

        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var appointment = await _appointmentService.GetAppointmentDetailsAsync(id);
            if (appointment == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isClientOwner = appointment.Client.UserId == currentUserId;
            var isLawyerOwner = appointment.Lawyer.UserId == currentUserId;
            var isAdmin = User.IsInRole("Admin");

            if (!isClientOwner && !isLawyerOwner && !isAdmin)
            {
                return Forbid();
            }

            var vm = new AppointmentDetailsViewModel
            {
                Appointment = appointment,
                CurrentUserId = currentUserId,
                CurrentUserRole = isAdmin ? "Admin" : (isLawyerOwner ? "Lawyer" : "Client")
            };

            return View(vm);
        }

        [Authorize(Roles = "Lawyer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var (success, message) = await _appointmentService.UpdateStatusAsync(id, AppointmentStatus.Accepted, "Lawyer", userId);
            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }
            return RedirectToAction("Dashboard", "Lawyer");
        }

        [Authorize(Roles = "Lawyer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? reason)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var (success, message) = await _appointmentService.UpdateStatusAsync(id, AppointmentStatus.Rejected, "Lawyer", userId, reason);
            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }
            return RedirectToAction("Dashboard", "Lawyer");
        }

        [Authorize(Roles = "Lawyer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var (success, message) = await _appointmentService.UpdateStatusAsync(id, AppointmentStatus.Completed, "Lawyer", userId);
            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }
            return RedirectToAction("Dashboard", "Lawyer");
        }

        [Authorize(Roles = "Client")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var (success, message) = await _appointmentService.UpdateStatusAsync(id, AppointmentStatus.Cancelled, "Client", userId);
            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }
            return RedirectToAction("MyAppointments", "Client");
        }
    }
}
