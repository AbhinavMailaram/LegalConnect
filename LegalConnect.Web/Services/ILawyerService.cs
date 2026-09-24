using System.Collections.Generic;
using System.Threading.Tasks;
using LegalConnect.Web.Models.Entities;

namespace LegalConnect.Web.Services
{
    public interface ILawyerService
    {
        Task<LawyerProfile?> GetLawyerByUserIdAsync(string userId);
        Task<LawyerProfile?> GetLawyerByIdAsync(int lawyerId);
        Task<List<LawyerProfile>> SearchLawyersAsync(string? query, int? specializationId, string? location, int? minExperience);
        Task<bool> UpdateProfileAsync(LawyerProfile profile);
        Task<List<Availability>> GetLawyerAvailabilitiesAsync(int lawyerId);
        Task<bool> SaveAvailabilitySlotsAsync(int lawyerId, List<Availability> slots);
        Task<List<LawyerProfile>> GetPendingVerificationLawyersAsync();
        Task<List<LawyerProfile>> GetAllLawyersAsync();
        Task<(bool Success, string Message)> VerifyLawyerAsync(int lawyerId, bool approve, string? reason);
    }
}
