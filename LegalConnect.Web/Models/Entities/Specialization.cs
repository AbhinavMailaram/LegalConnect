using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LegalConnect.Web.Models.Entities
{
    public class Specialization
    {
        [Key]
        public int SpecializationId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public virtual ICollection<LawyerProfile> Lawyers { get; set; } = new List<LawyerProfile>();
    }
}
