using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LegalConnect.Web.Data;
using LegalConnect.Web.Models.Entities;

namespace LegalConnect.Web.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly LegalConnectDbContext _context;
        private readonly INotificationService _notificationService;

        public AppointmentService(LegalConnectDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<(bool Success, string Message, Appointment? Appointment)> CreateAppointmentRequestAsync(
            int clientId,
            int lawyerId,
            DateTime appointmentDate,
            TimeSpan appointmentTime,
            string caseTitle,
            string caseDescription,
            ConsultationMode mode)
        {
            var dateOnly = appointmentDate.Date;
            if (dateOnly < DateTime.Today)
            {
                return (false, "Appointment date cannot be in the past.", null);
            }

            var lawyer = await _context.LawyerProfiles
                .Include(l => l.User)
                .Include(l => l.Availabilities)
                .FirstOrDefaultAsync(l => l.LawyerId == lawyerId);

            if (lawyer == null || lawyer.VerificationStatus != VerificationStatus.Approved || !lawyer.User.IsActive)
            {
                return (false, "The selected lawyer is not currently available for appointments.", null);
            }

            // Check if lawyer has availability on this day and time
            var targetDay = dateOnly.DayOfWeek;
            var isWithinAvailability = lawyer.Availabilities.Any(a =>
                a.IsAvailable &&
                a.DayOfWeek == targetDay &&
                appointmentTime >= a.StartTime &&
                appointmentTime < a.EndTime);

            if (!isWithinAvailability)
            {
                return (false, $"The lawyer does not have declared availability on {targetDay} at {appointmentTime:hh\\:mm}.", null);
            }

            // Check if already booked at the exact same slot (Pending or Accepted)
            var hasConflict = await _context.Appointments.AnyAsync(a =>
                a.LawyerId == lawyerId &&
                a.AppointmentDate.Date == dateOnly &&
                a.AppointmentTime == appointmentTime &&
                (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Accepted));

            if (hasConflict)
            {
                return (false, "This appointment time slot is already booked. Please select a different time.", null);
            }

            // Check if client already booked this lawyer at this time
            var clientConflict = await _context.Appointments.AnyAsync(a =>
                a.ClientId == clientId &&
                a.AppointmentDate.Date == dateOnly &&
                a.AppointmentTime == appointmentTime &&
                (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Accepted));

            if (clientConflict)
            {
                return (false, "You already have a pending or confirmed appointment at this date and time.", null);
            }

            var appointment = new Appointment
            {
                ClientId = clientId,
                LawyerId = lawyerId,
                AppointmentDate = dateOnly,
                AppointmentTime = appointmentTime,
                CaseTitle = caseTitle.Trim(),
                CaseDescription = caseDescription.Trim(),
                ConsultationMode = mode,
                Status = AppointmentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            // Client and Lawyer user details for notification
            var client = await _context.ClientProfiles.Include(c => c.User).FirstOrDefaultAsync(c => c.ClientId == clientId);
            var clientName = client?.User?.FullName ?? "A client";

            await _notificationService.CreateNotificationAsync(
                lawyer.UserId,
                "New Appointment Request",
                $"{clientName} has requested a consultation on {appointment.AppointmentDate:yyyy-MM-dd} at {appointment.AppointmentTime:hh\\:mm} for '{appointment.CaseTitle}'."
            );

            return (true, "Appointment request sent successfully.", appointment);
        }

        public async Task<List<Appointment>> GetClientAppointmentsAsync(int clientId)
        {
            return await _context.Appointments
                .Include(a => a.Lawyer)
                    .ThenInclude(l => l.User)
                .Include(a => a.Lawyer)
                    .ThenInclude(l => l.Specialization)
                .Where(a => a.ClientId == clientId)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();
        }

        public async Task<List<Appointment>> GetLawyerAppointmentsAsync(int lawyerId)
        {
            return await _context.Appointments
                .Include(a => a.Client)
                    .ThenInclude(c => c.User)
                .Include(a => a.Documents)
                .Where(a => a.LawyerId == lawyerId)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();
        }

        public async Task<Appointment?> GetAppointmentDetailsAsync(int appointmentId)
        {
            return await _context.Appointments
                .Include(a => a.Client)
                    .ThenInclude(c => c.User)
                .Include(a => a.Lawyer)
                    .ThenInclude(l => l.User)
                .Include(a => a.Lawyer)
                    .ThenInclude(l => l.Specialization)
                .Include(a => a.Documents)
                .Include(a => a.Messages)
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);
        }

        public async Task<(bool Success, string Message)> UpdateStatusAsync(int appointmentId, AppointmentStatus newStatus, string userRole, string userId, string? reason = null)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Client).ThenInclude(c => c.User)
                .Include(a => a.Lawyer).ThenInclude(l => l.User)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (appointment == null) return (false, "Appointment not found.");

            if (userRole == "Lawyer")
            {
                if (appointment.Lawyer.UserId != userId) return (false, "Unauthorized action on this appointment.");

                if (newStatus == AppointmentStatus.Accepted)
                {
                    if (appointment.Status != AppointmentStatus.Pending)
                        return (false, "Only pending appointments can be accepted.");

                    appointment.Status = AppointmentStatus.Accepted;
                    await _context.SaveChangesAsync();

                    await _notificationService.CreateNotificationAsync(
                        appointment.Client.UserId,
                        "Appointment Accepted",
                        $"Adv. {appointment.Lawyer.User.FullName} accepted your appointment request for {appointment.AppointmentDate:yyyy-MM-dd} at {appointment.AppointmentTime:hh\\:mm}."
                    );
                    return (true, "Appointment accepted successfully.");
                }
                else if (newStatus == AppointmentStatus.Rejected)
                {
                    if (appointment.Status != AppointmentStatus.Pending)
                        return (false, "Only pending appointments can be rejected.");

                    appointment.Status = AppointmentStatus.Rejected;
                    await _context.SaveChangesAsync();

                    await _notificationService.CreateNotificationAsync(
                        appointment.Client.UserId,
                        "Appointment Rejected",
                        $"Adv. {appointment.Lawyer.User.FullName} was unable to accept your appointment for {appointment.AppointmentDate:yyyy-MM-dd}. Reason: {reason ?? "Schedule conflict"}."
                    );
                    return (true, "Appointment rejected.");
                }
                else if (newStatus == AppointmentStatus.Completed)
                {
                    if (appointment.Status != AppointmentStatus.Accepted)
                        return (false, "Only accepted appointments can be marked as completed.");

                    appointment.Status = AppointmentStatus.Completed;
                    await _context.SaveChangesAsync();

                    await _notificationService.CreateNotificationAsync(
                        appointment.Client.UserId,
                        "Consultation Completed",
                        $"Your consultation with Adv. {appointment.Lawyer.User.FullName} on {appointment.AppointmentDate:yyyy-MM-dd} is now marked completed."
                    );
                    return (true, "Appointment marked as completed.");
                }
            }
            else if (userRole == "Client")
            {
                if (appointment.Client.UserId != userId) return (false, "Unauthorized action on this appointment.");

                if (newStatus == AppointmentStatus.Cancelled)
                {
                    if (appointment.Status != AppointmentStatus.Pending && appointment.Status != AppointmentStatus.Accepted)
                        return (false, "This appointment cannot be cancelled at its current status.");

                    appointment.Status = AppointmentStatus.Cancelled;
                    await _context.SaveChangesAsync();

                    await _notificationService.CreateNotificationAsync(
                        appointment.Lawyer.UserId,
                        "Appointment Cancelled",
                        $"Client {appointment.Client.User.FullName} cancelled the appointment scheduled for {appointment.AppointmentDate:yyyy-MM-dd} at {appointment.AppointmentTime:hh\\:mm}."
                    );
                    return (true, "Appointment cancelled successfully.");
                }
            }

            return (false, "Invalid status transition.");
        }

        public async Task<List<Appointment>> GetAllAppointmentsAsync()
        {
            return await _context.Appointments
                .Include(a => a.Client).ThenInclude(c => c.User)
                .Include(a => a.Lawyer).ThenInclude(l => l.User)
                .Include(a => a.Lawyer).ThenInclude(l => l.Specialization)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();
        }

        public async Task<(bool Success, string Message)> CancelByAdminAsync(int appointmentId, string reason)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Client).ThenInclude(c => c.User)
                .Include(a => a.Lawyer).ThenInclude(l => l.User)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (appointment == null) return (false, "Appointment not found.");

            appointment.Status = AppointmentStatus.Cancelled;
            await _context.SaveChangesAsync();

            var adminNote = string.IsNullOrWhiteSpace(reason) ? "Cancelled by administrator." : $"Cancelled by administrator: {reason}";

            await _notificationService.CreateNotificationAsync(
                appointment.Client.UserId,
                "Appointment Cancelled by Admin",
                adminNote
            );

            await _notificationService.CreateNotificationAsync(
                appointment.Lawyer.UserId,
                "Appointment Cancelled by Admin",
                adminNote
            );

            return (true, "Appointment cancelled by administrator.");
        }
    }
}
