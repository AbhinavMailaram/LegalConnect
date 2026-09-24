using System;
using System.Collections.Generic;
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
    public class LawyerController : Controller
    {
        private readonly ILawyerService _lawyerService;
        private readonly IAppointmentService _appointmentService;
        private readonly INotificationService _notificationService;
        private readonly IFileService _fileService;
        private readonly LegalConnectDbContext _context;

        public LawyerController(
            ILawyerService lawyerService,
            IAppointmentService appointmentService,
            INotificationService notificationService,
            IFileService fileService,
            LegalConnectDbContext context)
        {
            _lawyerService = lawyerService;
            _appointmentService = appointmentService;
            _notificationService = notificationService;
            _fileService = fileService;
            _context = context;
        }

        // Public view of lawyer profile
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var lawyer = await _lawyerService.GetLawyerByIdAsync(id);
            if (lawyer == null)
            {
                return NotFound();
            }

            var vm = new LawyerPublicProfileViewModel
            {
                LawyerId = lawyer.LawyerId,
                FullName = lawyer.User?.FullName ?? "Advocate",
                Email = lawyer.User?.Email ?? "",
                Phone = lawyer.User?.Phone ?? "",
                RegistrationNumber = lawyer.RegistrationNumber,
                Qualification = lawyer.Qualification,
                ExperienceYears = lawyer.ExperienceYears,
                SpecializationName = lawyer.Specialization?.Name ?? "General Practice",
                Location = lawyer.Location,
                Address = lawyer.Address,
                Description = lawyer.Description,
                ConsultationFee = lawyer.ConsultationFee,
                ProfilePhoto = lawyer.ProfilePhoto,
                VerificationStatus = lawyer.VerificationStatus,
                Availabilities = lawyer.Availabilities
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

        [Authorize(Roles = "Lawyer")]
        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var lawyer = await _lawyerService.GetLawyerByUserIdAsync(userId);
            if (lawyer == null)
            {
                return RedirectToAction("EditProfile");
            }

            var appointments = await _appointmentService.GetLawyerAppointmentsAsync(lawyer.LawyerId);
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, 5);
            var unreadNotes = await _notificationService.GetUnreadCountAsync(userId);
            var unreadMessages = await _context.Messages.CountAsync(m => m.ReceiverId == userId && !m.IsRead);

            var vm = new LawyerDashboardViewModel
            {
                Lawyer = lawyer,
                PendingRequests = appointments.Where(a => a.Status == AppointmentStatus.Pending).ToList(),
                UpcomingAppointments = appointments.Where(a => a.Status == AppointmentStatus.Accepted).ToList(),
                CompletedConsultations = appointments.Where(a => a.Status == AppointmentStatus.Completed).ToList(),
                CancelledAppointments = appointments.Where(a => a.Status == AppointmentStatus.Cancelled || a.Status == AppointmentStatus.Rejected).ToList(),
                RecentNotifications = notifications,
                UnreadNotificationsCount = unreadNotes,
                UnreadMessagesCount = unreadMessages
            };

            return View(vm);
        }

        [Authorize(Roles = "Lawyer")]
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var lawyer = await _lawyerService.GetLawyerByUserIdAsync(userId);
            if (lawyer == null)
            {
                return NotFound();
            }

            var specializations = await _context.Specializations.OrderBy(s => s.Name).ToListAsync();

            var vm = new LawyerProfileEditViewModel
            {
                LawyerId = lawyer.LawyerId,
                UserId = lawyer.UserId,
                FullName = lawyer.User.FullName,
                Email = lawyer.User.Email ?? "",
                Phone = lawyer.User.Phone ?? "",
                RegistrationNumber = lawyer.RegistrationNumber,
                Qualification = lawyer.Qualification,
                ExperienceYears = lawyer.ExperienceYears,
                SpecializationId = lawyer.SpecializationId ?? 0,
                Location = lawyer.Location,
                Address = lawyer.Address,
                Description = lawyer.Description,
                ConsultationFee = lawyer.ConsultationFee,
                ExistingPhotoPath = lawyer.ProfilePhoto,
                VerificationStatus = lawyer.VerificationStatus,
                RejectionReason = lawyer.RejectionReason,
                SpecializationsList = specializations.Select(s => new SelectListItem
                {
                    Value = s.SpecializationId.ToString(),
                    Text = s.Name,
                    Selected = (lawyer.SpecializationId == s.SpecializationId)
                }).ToList()
            };

            return View(vm);
        }

        [Authorize(Roles = "Lawyer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(LawyerProfileEditViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var lawyer = await _lawyerService.GetLawyerByUserIdAsync(userId);
            if (lawyer == null || lawyer.LawyerId != model.LawyerId)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                var specializations = await _context.Specializations.OrderBy(s => s.Name).ToListAsync();
                model.SpecializationsList = specializations.Select(s => new SelectListItem
                {
                    Value = s.SpecializationId.ToString(),
                    Text = s.Name,
                    Selected = (model.SpecializationId == s.SpecializationId)
                }).ToList();
                model.ExistingPhotoPath = lawyer.ProfilePhoto;
                return View(model);
            }

            // Update User details
            lawyer.User.FullName = model.FullName.Trim();
            lawyer.User.Phone = model.Phone.Trim();

            // Handle Photo upload
            if (model.ProfilePhotoFile != null && model.ProfilePhotoFile.Length > 0)
            {
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                if (_fileService.IsValidFileType(model.ProfilePhotoFile, allowed) &&
                    _fileService.IsValidFileSize(model.ProfilePhotoFile, 5 * 1024 * 1024))
                {
                    var savedPath = await _fileService.SaveFileAsync(model.ProfilePhotoFile, "lawyers");
                    lawyer.ProfilePhoto = savedPath;
                }
                else
                {
                    ModelState.AddModelError("ProfilePhotoFile", "Photo must be JPG or PNG and under 5MB.");
                    var specializations = await _context.Specializations.OrderBy(s => s.Name).ToListAsync();
                    model.SpecializationsList = specializations.Select(s => new SelectListItem
                    {
                        Value = s.SpecializationId.ToString(),
                        Text = s.Name,
                        Selected = (model.SpecializationId == s.SpecializationId)
                    }).ToList();
                    return View(model);
                }
            }

            lawyer.RegistrationNumber = model.RegistrationNumber.Trim();
            lawyer.Qualification = model.Qualification.Trim();
            lawyer.ExperienceYears = model.ExperienceYears;
            lawyer.SpecializationId = model.SpecializationId;
            lawyer.Location = model.Location.Trim();
            lawyer.Address = model.Address.Trim();
            lawyer.Description = model.Description.Trim();
            lawyer.ConsultationFee = model.ConsultationFee;

            // If lawyer was rejected and updates their profile, reset status to Pending for re-review
            if (lawyer.VerificationStatus == VerificationStatus.Rejected)
            {
                lawyer.VerificationStatus = VerificationStatus.Pending;
                lawyer.RejectionReason = null;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Profile details updated successfully!";
            return RedirectToAction("Dashboard");
        }

        [Authorize(Roles = "Lawyer")]
        [HttpGet]
        public async Task<IActionResult> Availability()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var lawyer = await _lawyerService.GetLawyerByUserIdAsync(userId);
            if (lawyer == null) return NotFound();

            var currentSlots = await _lawyerService.GetLawyerAvailabilitiesAsync(lawyer.LawyerId);

            var vm = new LawyerAvailabilityViewModel
            {
                LawyerId = lawyer.LawyerId,
                Slots = Enum.GetValues<DayOfWeek>().Select(day =>
                {
                    var existing = currentSlots.FirstOrDefault(s => s.DayOfWeek == day);
                    return new AvailabilityInputItem
                    {
                        DayOfWeek = day,
                        StartTimeString = existing != null ? existing.StartTime.ToString(@"hh\:mm") : "10:00",
                        EndTimeString = existing != null ? existing.EndTime.ToString(@"hh\:mm") : "14:00",
                        IsAvailable = existing?.IsAvailable ?? (day != DayOfWeek.Sunday && day != DayOfWeek.Saturday)
                    };
                }).ToList()
            };

            return View(vm);
        }

        [Authorize(Roles = "Lawyer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Availability(LawyerAvailabilityViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var lawyer = await _lawyerService.GetLawyerByUserIdAsync(userId);
            if (lawyer == null || lawyer.LawyerId != model.LawyerId) return Forbid();

            var newSlots = new List<Availability>();
            foreach (var slot in model.Slots)
            {
                if (slot.IsAvailable)
                {
                    if (TimeSpan.TryParse(slot.StartTimeString, out var start) && TimeSpan.TryParse(slot.EndTimeString, out var end))
                    {
                        if (end <= start)
                        {
                            ModelState.AddModelError(string.Empty, $"{slot.DayOfWeek}: End time must be later than start time.");
                            return View(model);
                        }

                        newSlots.Add(new Availability
                        {
                            LawyerId = lawyer.LawyerId,
                            DayOfWeek = slot.DayOfWeek,
                            StartTime = start,
                            EndTime = end,
                            IsAvailable = true
                        });
                    }
                }
            }

            await _lawyerService.SaveAvailabilitySlotsAsync(lawyer.LawyerId, newSlots);
            TempData["SuccessMessage"] = "Weekly availability schedule updated successfully!";
            return RedirectToAction("Dashboard");
        }

        [Authorize(Roles = "Lawyer")]
        public async Task<IActionResult> Appointments()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var lawyer = await _lawyerService.GetLawyerByUserIdAsync(userId);
            if (lawyer == null) return NotFound();

            var appointments = await _appointmentService.GetLawyerAppointmentsAsync(lawyer.LawyerId);
            return View(appointments);
        }
    }
}
