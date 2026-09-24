using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LegalConnect.Web.Models.Entities;

namespace LegalConnect.Web.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LegalConnectDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure database is created and migrations applied
            await context.Database.MigrateAsync();

            // 1. Seed Roles: Admin, Lawyer, Client
            string[] roles = ["Admin", "Lawyer", "Client"];
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Seed Specializations
            var defaultSpecializations = new[]
            {
                new Specialization { Name = "Criminal Law", Description = "Defense and prosecution related to criminal cases and statutory offenses." },
                new Specialization { Name = "Civil Law", Description = "Disputes between individuals or organizations, torts, contract disputes." },
                new Specialization { Name = "Family Law", Description = "Divorce, child custody, adoption, maintenance, and domestic relations." },
                new Specialization { Name = "Property Law", Description = "Real estate transactions, title verification, land disputes, and lease agreements." },
                new Specialization { Name = "Corporate Law", Description = "Corporate governance, mergers and acquisitions, business contracts, and compliance." },
                new Specialization { Name = "Cyber Law", Description = "Data protection, cyber crimes, digital contracts, and intellectual property over internet." },
                new Specialization { Name = "Consumer Law", Description = "Consumer rights protection, unfair trade practices, defect claims, and liability." },
                new Specialization { Name = "Employment Law", Description = "Labor rights, workplace harassment, wrongful termination, and employment contracts." },
                new Specialization { Name = "Tax Law", Description = "Income tax, GST, corporate taxation disputes, and regulatory representation." },
                new Specialization { Name = "Intellectual Property Law", Description = "Trademarks, patents, copyrights, trade secrets, and infringement litigation." }
            };

            foreach (var spec in defaultSpecializations)
            {
                if (!await context.Specializations.AnyAsync(s => s.Name == spec.Name))
                {
                    context.Specializations.Add(spec);
                }
            }
            await context.SaveChangesAsync();

            var criminalSpec = await context.Specializations.FirstAsync(s => s.Name == "Criminal Law");
            var propertySpec = await context.Specializations.FirstAsync(s => s.Name == "Property Law");

            // 3. Seed Admin User
            var adminEmail = "admin@legalconnect.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "System Administrator",
                    Phone = "9876543210",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // 4. Seed Pre-Approved Demo Lawyer
            var lawyerEmail = "lawyer@legalconnect.com";
            var lawyerUser = await userManager.FindByEmailAsync(lawyerEmail);
            LawyerProfile? lawyerProfile = null;
            if (lawyerUser == null)
            {
                lawyerUser = new ApplicationUser
                {
                    UserName = lawyerEmail,
                    Email = lawyerEmail,
                    EmailConfirmed = true,
                    FullName = "Adv. Rahul Sharma",
                    Phone = "9820012345",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                var result = await userManager.CreateAsync(lawyerUser, "Lawyer@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(lawyerUser, "Lawyer");

                    lawyerProfile = new LawyerProfile
                    {
                        UserId = lawyerUser.Id,
                        RegistrationNumber = "BAR/MH/2015/8892",
                        Qualification = "B.A. LL.B (Hons), LL.M (Criminal & Corporate Law)",
                        ExperienceYears = 8,
                        SpecializationId = criminalSpec.SpecializationId,
                        Location = "Mumbai",
                        Address = "402 Nariman Point Chambers, Marine Drive, Mumbai",
                        Description = "Experienced legal counsel with over 8 years of rigorous litigation and advisory experience in criminal defense, property disputes, and corporate compliance.",
                        ConsultationFee = 1500.00m,
                        ProfilePhoto = "/images/lawyer1.jpg",
                        VerificationStatus = VerificationStatus.Approved
                    };
                    context.LawyerProfiles.Add(lawyerProfile);
                    await context.SaveChangesAsync();

                    // Seed Lawyer Availability
                    context.Availabilities.AddRange(
                        new Availability { LawyerId = lawyerProfile.LawyerId, DayOfWeek = DayOfWeek.Monday, StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(13, 0, 0), IsAvailable = true },
                        new Availability { LawyerId = lawyerProfile.LawyerId, DayOfWeek = DayOfWeek.Tuesday, StartTime = new TimeSpan(14, 0, 0), EndTime = new TimeSpan(17, 0, 0), IsAvailable = true },
                        new Availability { LawyerId = lawyerProfile.LawyerId, DayOfWeek = DayOfWeek.Wednesday, StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(12, 0, 0), IsAvailable = true },
                        new Availability { LawyerId = lawyerProfile.LawyerId, DayOfWeek = DayOfWeek.Friday, StartTime = new TimeSpan(15, 0, 0), EndTime = new TimeSpan(18, 0, 0), IsAvailable = true }
                    );
                    await context.SaveChangesAsync();
                }
            }
            else
            {
                lawyerProfile = await context.LawyerProfiles.FirstOrDefaultAsync(l => l.UserId == lawyerUser.Id);
            }

            // 5. Seed Demo Client
            var clientEmail = "client@legalconnect.com";
            var clientUser = await userManager.FindByEmailAsync(clientEmail);
            ClientProfile? clientProfile = null;
            if (clientUser == null)
            {
                clientUser = new ApplicationUser
                {
                    UserName = clientEmail,
                    Email = clientEmail,
                    EmailConfirmed = true,
                    FullName = "Priya Patel",
                    Phone = "9819954321",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                var result = await userManager.CreateAsync(clientUser, "Client@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(clientUser, "Client");

                    clientProfile = new ClientProfile
                    {
                        UserId = clientUser.Id,
                        DateOfBirth = new DateTime(1992, 5, 15),
                        Gender = "Female",
                        City = "Mumbai",
                        Address = "Flat 301, Sunrise Heights, Andheri West, Mumbai"
                    };
                    context.ClientProfiles.Add(clientProfile);
                    await context.SaveChangesAsync();
                }
            }
            else
            {
                clientProfile = await context.ClientProfiles.FirstOrDefaultAsync(c => c.UserId == clientUser.Id);
            }

            // 6. Seed Sample Appointments for populated dashboards
            if (clientProfile != null && lawyerProfile != null)
            {
                if (!await context.Appointments.AnyAsync(a => a.ClientId == clientProfile.ClientId))
                {
                    var pastAppointment = new Appointment
                    {
                        ClientId = clientProfile.ClientId,
                        LawyerId = lawyerProfile.LawyerId,
                        AppointmentDate = DateTime.Today.AddDays(-7),
                        AppointmentTime = new TimeSpan(10, 0, 0),
                        CaseTitle = "Property Title Verification Consultation",
                        CaseDescription = "Preliminary review and verification of ancestral property deed records in Mumbai.",
                        ConsultationMode = ConsultationMode.Online,
                        Status = AppointmentStatus.Completed,
                        CreatedAt = DateTime.UtcNow.AddDays(-10)
                    };

                    var upcomingAppointment = new Appointment
                    {
                        ClientId = clientProfile.ClientId,
                        LawyerId = lawyerProfile.LawyerId,
                        AppointmentDate = DateTime.Today.AddDays(3),
                        AppointmentTime = new TimeSpan(14, 0, 0),
                        CaseTitle = "Commercial Agreement Dispute",
                        CaseDescription = "Discussion regarding breach of service contract terms and compensation clauses.",
                        ConsultationMode = ConsultationMode.Online,
                        Status = AppointmentStatus.Accepted,
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    };

                    context.Appointments.AddRange(pastAppointment, upcomingAppointment);
                    await context.SaveChangesAsync();

                    // Seed a message in the upcoming appointment thread
                    context.Messages.AddRange(
                        new Message
                        {
                            SenderId = clientUser!.Id,
                            ReceiverId = lawyerUser!.Id,
                            AppointmentId = upcomingAppointment.AppointmentId,
                            MessageText = "Hello Adv. Sharma, I have submitted the agreement details. Looking forward to our consultation.",
                            SentAt = DateTime.UtcNow.AddHours(-12),
                            IsRead = true
                        },
                        new Message
                        {
                            SenderId = lawyerUser!.Id,
                            ReceiverId = clientUser.Id,
                            AppointmentId = upcomingAppointment.AppointmentId,
                            MessageText = "Hello Priya, I have reviewed the brief. Please keep the original contract document accessible during our meeting.",
                            SentAt = DateTime.UtcNow.AddHours(-6),
                            IsRead = false
                        }
                    );

                    // Seed notification
                    context.Notifications.AddRange(
                        new Notification
                        {
                            UserId = clientUser.Id,
                            Title = "Appointment Confirmed",
                            Message = "Your appointment on " + upcomingAppointment.AppointmentDate.ToString("yyyy-MM-dd") + " has been accepted by Adv. Rahul Sharma.",
                            CreatedAt = DateTime.UtcNow.AddDays(-1),
                            IsRead = false
                        },
                        new Notification
                        {
                            UserId = lawyerUser.Id,
                            Title = "New Appointment Request",
                            Message = "You accepted an appointment with Priya Patel.",
                            CreatedAt = DateTime.UtcNow.AddDays(-1),
                            IsRead = true
                        }
                    );

                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
