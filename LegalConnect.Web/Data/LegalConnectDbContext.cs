using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LegalConnect.Web.Models.Entities;

namespace LegalConnect.Web.Data
{
    public class LegalConnectDbContext : IdentityDbContext<ApplicationUser>
    {
        public LegalConnectDbContext(DbContextOptions<LegalConnectDbContext> options)
            : base(options)
        {
        }

        public DbSet<Specialization> Specializations { get; set; } = null!;
        public DbSet<LawyerProfile> LawyerProfiles { get; set; } = null!;
        public DbSet<ClientProfile> ClientProfiles { get; set; } = null!;
        public DbSet<Availability> Availabilities { get; set; } = null!;
        public DbSet<Appointment> Appointments { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<Document> Documents { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ApplicationUser <-> LawyerProfile (1 to 0..1)
            builder.Entity<LawyerProfile>()
                .HasOne(lp => lp.User)
                .WithOne(u => u.LawyerProfile)
                .HasForeignKey<LawyerProfile>(lp => lp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ApplicationUser <-> ClientProfile (1 to 0..1)
            builder.Entity<ClientProfile>()
                .HasOne(cp => cp.User)
                .WithOne(u => u.ClientProfile)
                .HasForeignKey<ClientProfile>(cp => cp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // LawyerProfile <-> Specialization (Restrict delete to preserve integrity)
            builder.Entity<LawyerProfile>()
                .HasOne(lp => lp.Specialization)
                .WithMany(s => s.Lawyers)
                .HasForeignKey(lp => lp.SpecializationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Availability <-> LawyerProfile
            builder.Entity<Availability>()
                .HasOne(a => a.Lawyer)
                .WithMany(l => l.Availabilities)
                .HasForeignKey(a => a.LawyerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Appointment <-> ClientProfile & LawyerProfile (Must use Restrict to prevent multiple cascade path errors in SQL Server)
            builder.Entity<Appointment>()
                .HasOne(a => a.Client)
                .WithMany(c => c.Appointments)
                .HasForeignKey(a => a.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Appointment>()
                .HasOne(a => a.Lawyer)
                .WithMany(l => l.Appointments)
                .HasForeignKey(a => a.LawyerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Message <-> ApplicationUser (Sender and Receiver)
            builder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Message>()
                .HasOne(m => m.Appointment)
                .WithMany(a => a.Messages)
                .HasForeignKey(m => m.AppointmentId)
                .OnDelete(DeleteBehavior.SetNull);

            // Document <-> Client & Appointment
            builder.Entity<Document>()
                .HasOne(d => d.Client)
                .WithMany(c => c.Documents)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Document>()
                .HasOne(d => d.Appointment)
                .WithMany(a => a.Documents)
                .HasForeignKey(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.SetNull);

            // Notification <-> ApplicationUser
            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
