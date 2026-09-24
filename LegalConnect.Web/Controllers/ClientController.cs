using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LegalConnect.Web.Data;
using LegalConnect.Web.Models.Entities;
using LegalConnect.Web.Models.ViewModels;
using LegalConnect.Web.Services;

namespace LegalConnect.Web.Controllers
{
    public class ClientController : Controller
    {
        private readonly LegalConnectDbContext _context;
        private readonly ILawyerService _lawyerService;
        private readonly IAppointmentService _appointmentService;
        private readonly INotificationService _notificationService;

        public ClientController(
            LegalConnectDbContext context,
            ILawyerService lawyerService,
            IAppointmentService appointmentService,
            INotificationService notificationService)
        {
            _context = context;
            _lawyerService = lawyerService;
            _appointmentService = appointmentService;
            _notificationService = notificationService;
        }

        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var client = await _context.ClientProfiles
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null)
            {
                client = new ClientProfile { UserId = userId };
                _context.ClientProfiles.Add(client);
                await _context.SaveChangesAsync();
                client = await _context.ClientProfiles.Include(c => c.User).FirstAsync(c => c.UserId == userId);
            }

            var appointments = await _appointmentService.GetClientAppointmentsAsync(client.ClientId);
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, 5);
            var unreadNotes = await _notificationService.GetUnreadCountAsync(userId);
            var unreadMessages = await _context.Messages.CountAsync(m => m.ReceiverId == userId && !m.IsRead);

            var vm = new ClientDashboardViewModel
            {
                Client = client,
                PendingRequests = appointments.Where(a => a.Status == AppointmentStatus.Pending).ToList(),
                UpcomingAppointments = appointments.Where(a => a.Status == AppointmentStatus.Accepted).ToList(),
                AcceptedAppointments = appointments.Where(a => a.Status == AppointmentStatus.Accepted).ToList(),
                CompletedConsultations = appointments.Where(a => a.Status == AppointmentStatus.Completed).ToList(),
                CancelledAppointments = appointments.Where(a => a.Status == AppointmentStatus.Cancelled || a.Status == AppointmentStatus.Rejected).ToList(),
                RecentNotifications = notifications,
                UnreadNotificationsCount = unreadNotes,
                UnreadMessagesCount = unreadMessages
            };

            return View(vm);
        }

        [Authorize(Roles = "Client")]
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var client = await _context.ClientProfiles
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null) return NotFound();

            var vm = new ClientProfileEditViewModel
            {
                ClientId = client.ClientId,
                UserId = client.UserId,
                FullName = client.User.FullName,
                Email = client.User.Email ?? "",
                Phone = client.User.Phone ?? "",
                DateOfBirth = client.DateOfBirth,
                Gender = client.Gender,
                City = client.City,
                Address = client.Address
            };

            return View(vm);
        }

        [Authorize(Roles = "Client")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(ClientProfileEditViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var client = await _context.ClientProfiles
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null || client.ClientId != model.ClientId) return Forbid();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            client.User.FullName = model.FullName.Trim();
            client.User.Phone = model.Phone.Trim();
            client.DateOfBirth = model.DateOfBirth;
            client.Gender = model.Gender;
            client.City = model.City?.Trim();
            client.Address = model.Address?.Trim();

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Dashboard");
        }

        // Public/Client Lawyer Search
        [AllowAnonymous]
        public async Task<IActionResult> FindLawyers(string? query, int? specializationId, string? location, int? minExperience)
        {
            var specializations = await _context.Specializations.OrderBy(s => s.Name).ToListAsync();
            var lawyers = await _lawyerService.SearchLawyersAsync(query, specializationId, location, minExperience);

            var vm = new LawyerSearchViewModel
            {
                Query = query,
                SpecializationId = specializationId,
                Location = location,
                MinExperience = minExperience,
                SpecializationsList = specializations.Select(s => new SelectListItem
                {
                    Value = s.SpecializationId.ToString(),
                    Text = s.Name,
                    Selected = (specializationId == s.SpecializationId)
                }).ToList(),
                Results = lawyers.Select(l => new LawyerPublicProfileViewModel
                {
                    LawyerId = l.LawyerId,
                    FullName = l.User.FullName,
                    Email = l.User.Email ?? "",
                    Phone = l.User.Phone ?? "",
                    RegistrationNumber = l.RegistrationNumber,
                    Qualification = l.Qualification,
                    ExperienceYears = l.ExperienceYears,
                    SpecializationName = l.Specialization?.Name ?? "General Practice",
                    Location = l.Location,
                    Address = l.Address,
                    Description = l.Description,
                    ConsultationFee = l.ConsultationFee,
                    ProfilePhoto = l.ProfilePhoto,
                    VerificationStatus = l.VerificationStatus,
                    Availabilities = l.Availabilities
                        .Where(a => a.IsAvailable)
                        .OrderBy(a => a.DayOfWeek)
                        .Select(a => new AvailabilitySlotItem
                        {
                            DayOfWeek = a.DayOfWeek,
                            StartTime = a.StartTime,
                            EndTime = a.EndTime,
                            IsAvailable = a.IsAvailable
                        }).ToList()
                }).ToList()
            };

            return View(vm);
        }

        [Authorize(Roles = "Client")]
        public async Task<IActionResult> MyAppointments()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var client = await _context.ClientProfiles.FirstOrDefaultAsync(c => c.UserId == userId);
            if (client == null) return NotFound();

            var appointments = await _appointmentService.GetClientAppointmentsAsync(client.ClientId);
            return View(appointments);
        }
    }
}
