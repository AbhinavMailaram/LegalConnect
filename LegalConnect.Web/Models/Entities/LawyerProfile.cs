using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalConnect.Web.Models.Entities
{
    public class LawyerProfile
    {
        [Key]
        public int LawyerId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [MaxLength(100)]
        public string RegistrationNumber { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Qualification { get; set; } = string.Empty;

        public int ExperienceYears { get; set; }

        public int? SpecializationId { get; set; }

        [ForeignKey("SpecializationId")]
        public virtual Specialization? Specialization { get; set; }

        [MaxLength(150)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(300)]
        public string Address { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ConsultationFee { get; set; }

        [MaxLength(500)]
        public string? ProfilePhoto { get; set; }

        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;

        public string? RejectionReason { get; set; }

        // Navigation
        public virtual ICollection<Availability> Availabilities { get; set; } = new List<Availability>();
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
