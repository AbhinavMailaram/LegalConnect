using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LegalConnect.Web.Models.Entities;

namespace LegalConnect.Web.Models.ViewModels
{
    public class BookAppointmentViewModel
    {
        public int LawyerId { get; set; }
        public string LawyerName { get; set; } = string.Empty;
        public string SpecializationName { get; set; } = string.Empty;
        public decimal ConsultationFee { get; set; }
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please choose an appointment date.")]
        [DataType(DataType.Date)]
        [Display(Name = "Consultation Date")]
        public DateTime AppointmentDate { get; set; } = DateTime.Today.AddDays(1);

        [Required(ErrorMessage = "Please select an appointment time slot.")]
        [Display(Name = "Available Time Slot")]
        public string AppointmentTimeString { get; set; } = "10:00";

        [Required(ErrorMessage = "Case title is required.")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Case title must be between 5 and 200 characters.")]
        [Display(Name = "Case / Dispute Title")]
        public string CaseTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Problem description is required.")]
        [StringLength(2000, MinimumLength = 20, ErrorMessage = "Please describe the matter in at least 20 characters.")]
        [Display(Name = "Matter / Case Description")]
        public string CaseDescription { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Consultation Mode")]
        public ConsultationMode ConsultationMode { get; set; } = ConsultationMode.Online;

        public List<AvailabilitySlotItem> AvailableSlots { get; set; } = new List<AvailabilitySlotItem>();
    }

    public class AppointmentDetailsViewModel
    {
        public Appointment Appointment { get; set; } = null!;
        public string CurrentUserId { get; set; } = string.Empty;
        public string CurrentUserRole { get; set; } = string.Empty;
        public string NewMessageText { get; set; } = string.Empty;
    }
}
