using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalConnect.Web.Models.Entities
{
    public class Appointment
    {
        [Key]
        public int AppointmentId { get; set; }

        [Required]
        public int ClientId { get; set; }

        [ForeignKey("ClientId")]
        public virtual ClientProfile Client { get; set; } = null!;

        [Required]
        public int LawyerId { get; set; }

        [ForeignKey("LawyerId")]
        public virtual LawyerProfile Lawyer { get; set; } = null!;

        [Required]
        public DateTime AppointmentDate { get; set; }

        [Required]
        public TimeSpan AppointmentTime { get; set; }

        [Required]
        [MaxLength(200)]
        public string CaseTitle { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string CaseDescription { get; set; } = string.Empty;

        public ConsultationMode ConsultationMode { get; set; } = ConsultationMode.Online;

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
