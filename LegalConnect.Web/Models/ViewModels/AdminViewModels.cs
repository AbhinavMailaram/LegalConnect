using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LegalConnect.Web.Models.Entities;

namespace LegalConnect.Web.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalLawyers { get; set; }
        public int PendingVerifications { get; set; }
        public int TotalAppointments { get; set; }
        public List<ApplicationUser> RecentUsers { get; set; } = new List<ApplicationUser>();
        public List<LawyerProfile> PendingLawyers { get; set; } = new List<LawyerProfile>();
        public List<Appointment> RecentAppointments { get; set; } = new List<Appointment>();
    }

    public class UserManagementItemViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class SpecializationCrudViewModel
    {
        public int SpecializationId { get; set; }

        [Required(ErrorMessage = "Specialization name is required.")]
        [StringLength(150, ErrorMessage = "Specialization name cannot exceed 150 characters.")]
        [Display(Name = "Specialization Area Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        [Display(Name = "Practice Description")]
        public string? Description { get; set; }

        public int AssociatedLawyersCount { get; set; }
    }

    public class VerifyLawyerActionViewModel
    {
        public int LawyerId { get; set; }
        public bool Approve { get; set; }
        public string? Reason { get; set; }
    }
}
