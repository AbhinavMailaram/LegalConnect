using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using LegalConnect.Web.Models.Entities;

namespace LegalConnect.Web.Models.ViewModels
{
    public class LawyerProfileEditViewModel
    {
        public int LawyerId { get; set; }
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Display(Name = "Contact Phone")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bar registration number is required.")]
        [Display(Name = "Bar / Professional Registration Number")]
        public string RegistrationNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Qualifications are required (e.g. LL.B, LL.M).")]
        [Display(Name = "Educational Qualifications")]
        public string Qualification { get; set; } = string.Empty;

        [Required(ErrorMessage = "Years of experience is required.")]
        [Range(0, 70, ErrorMessage = "Experience must be between 0 and 70 years.")]
        [Display(Name = "Years of Experience")]
        public int ExperienceYears { get; set; }

        [Required(ErrorMessage = "Please select a specialization area.")]
        [Display(Name = "Primary Specialization")]
        public int SpecializationId { get; set; }

        [Required(ErrorMessage = "City/Location is required.")]
        [Display(Name = "City / Region")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Office address is required.")]
        [Display(Name = "Office Chamber / Address")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Professional summary is required.")]
        [StringLength(2000, MinimumLength = 20, ErrorMessage = "Summary should be at least 20 characters.")]
        [Display(Name = "Professional Bio / About")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Consultation fee is required.")]
        [Range(0, 1000000, ErrorMessage = "Consultation fee must be non-negative.")]
        [Display(Name = "Standard Consultation Fee (₹)")]
        public decimal ConsultationFee { get; set; }

        [Display(Name = "Profile Photo")]
        public IFormFile? ProfilePhotoFile { get; set; }

        public string? ExistingPhotoPath { get; set; }

        public VerificationStatus VerificationStatus { get; set; }
        public string? RejectionReason { get; set; }

        public List<SelectListItem> SpecializationsList { get; set; } = new List<SelectListItem>();
    }

    public class LawyerPublicProfileViewModel
    {
        public int LawyerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public string Qualification { get; set; } = string.Empty;
        public int ExperienceYears { get; set; }
        public string SpecializationName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal ConsultationFee { get; set; }
        public string? ProfilePhoto { get; set; }
        public VerificationStatus VerificationStatus { get; set; }
        public List<AvailabilitySlotItem> Availabilities { get; set; } = new List<AvailabilitySlotItem>();
    }

    public class AvailabilitySlotItem
    {
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsAvailable { get; set; }
    }

    public class LawyerAvailabilityViewModel
    {
        public int LawyerId { get; set; }
        public List<AvailabilityInputItem> Slots { get; set; } = new List<AvailabilityInputItem>();
    }

    public class AvailabilityInputItem
    {
        public DayOfWeek DayOfWeek { get; set; }
        public string StartTimeString { get; set; } = "10:00";
        public string EndTimeString { get; set; } = "13:00";
        public bool IsAvailable { get; set; }
    }

    public class LawyerDashboardViewModel
    {
        public LawyerProfile Lawyer { get; set; } = null!;
        public List<Appointment> PendingRequests { get; set; } = new List<Appointment>();
        public List<Appointment> UpcomingAppointments { get; set; } = new List<Appointment>();
        public List<Appointment> CompletedConsultations { get; set; } = new List<Appointment>();
        public List<Appointment> CancelledAppointments { get; set; } = new List<Appointment>();
        public List<Notification> RecentNotifications { get; set; } = new List<Notification>();
        public int UnreadNotificationsCount { get; set; }
        public int UnreadMessagesCount { get; set; }
    }
}
