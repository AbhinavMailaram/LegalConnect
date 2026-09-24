using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace LegalConnect.Web.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Navigation
        public virtual ClientProfile? ClientProfile { get; set; }
        public virtual LawyerProfile? LawyerProfile { get; set; }
        public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();
        public virtual ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
