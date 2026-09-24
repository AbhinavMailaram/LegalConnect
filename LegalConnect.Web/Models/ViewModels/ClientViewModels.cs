using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using LegalConnect.Web.Models.Entities;

namespace LegalConnect.Web.Models.ViewModels
{
    public class ClientProfileEditViewModel
    {
        public int ClientId { get; set; }
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Display(Name = "Contact Phone")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Gender")]
        public string? Gender { get; set; }

        [Display(Name = "City / Town")]
        public string? City { get; set; }

        [Display(Name = "Residential Address")]
        public string? Address { get; set; }
    }

    public class ClientDashboardViewModel
    {
        public ClientProfile Client { get; set; } = null!;
        public List<Appointment> PendingRequests { get; set; } = new List<Appointment>();
        public List<Appointment> UpcomingAppointments { get; set; } = new List<Appointment>();
        public List<Appointment> AcceptedAppointments { get; set; } = new List<Appointment>();
        public List<Appointment> CompletedConsultations { get; set; } = new List<Appointment>();
        public List<Appointment> CancelledAppointments { get; set; } = new List<Appointment>();
        public List<Notification> RecentNotifications { get; set; } = new List<Notification>();
        public int UnreadNotificationsCount { get; set; }
        public int UnreadMessagesCount { get; set; }
    }

    public class LawyerSearchViewModel
    {
        public string? Query { get; set; }
        public int? SpecializationId { get; set; }
        public string? Location { get; set; }
        public int? MinExperience { get; set; }

        public List<SelectListItem> SpecializationsList { get; set; } = new List<SelectListItem>();
        public List<LawyerPublicProfileViewModel> Results { get; set; } = new List<LawyerPublicProfileViewModel>();
    }
}
