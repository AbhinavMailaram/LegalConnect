using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LegalConnect.Web.Models.Entities;

namespace LegalConnect.Web.Services
{
    public interface IAppointmentService
    {
        Task<(bool Success, string Message, Appointment? Appointment)> CreateAppointmentRequestAsync(
            int clientId,
            int lawyerId,
            DateTime appointmentDate,
            TimeSpan appointmentTime,
            string caseTitle,
            string caseDescription,
            ConsultationMode mode);

        Task<List<Appointment>> GetClientAppointmentsAsync(int clientId);
        Task<List<Appointment>> GetLawyerAppointmentsAsync(int lawyerId);
        Task<Appointment?> GetAppointmentDetailsAsync(int appointmentId);
        Task<(bool Success, string Message)> UpdateStatusAsync(int appointmentId, AppointmentStatus newStatus, string userRole, string userId, string? reason = null);
        Task<List<Appointment>> GetAllAppointmentsAsync();
        Task<(bool Success, string Message)> CancelByAdminAsync(int appointmentId, string reason);
    }
}
