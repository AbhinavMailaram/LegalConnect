using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LegalConnect.Web.Data;
using LegalConnect.Web.Models.Entities;

namespace LegalConnect.Web.Services
{
    public class LawyerService : ILawyerService
    {
        private readonly LegalConnectDbContext _context;
        private readonly INotificationService _notificationService;

        public LawyerService(LegalConnectDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<LawyerProfile?> GetLawyerByUserIdAsync(string userId)
        {
            return await _context.LawyerProfiles
                .Include(l => l.User)
                .Include(l => l.Specialization)
                .Include(l => l.Availabilities)
                .FirstOrDefaultAsync(l => l.UserId == userId);
        }

        public async Task<LawyerProfile?> GetLawyerByIdAsync(int lawyerId)
        {
            return await _context.LawyerProfiles
                .Include(l => l.User)
                .Include(l => l.Specialization)
                .Include(l => l.Availabilities)
                .FirstOrDefaultAsync(l => l.LawyerId == lawyerId);
        }

        public async Task<List<LawyerProfile>> SearchLawyersAsync(string? query, int? specializationId, string? location, int? minExperience)
        {
            var q = _context.LawyerProfiles
                .Include(l => l.User)
                .Include(l => l.Specialization)
                .Include(l => l.Availabilities)
                .Where(l => l.VerificationStatus == VerificationStatus.Approved && l.User.IsActive);

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim().ToLower();
                q = q.Where(l => l.User.FullName.ToLower().Contains(term) ||
                                 l.Description.ToLower().Contains(term) ||
                                 l.Qualification.ToLower().Contains(term) ||
                                 (l.Specialization != null && l.Specialization.Name.ToLower().Contains(term)));
            }

            if (specializationId.HasValue && specializationId.Value > 0)
            {
                q = q.Where(l => l.SpecializationId == specializationId.Value);
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                var loc = location.Trim().ToLower();
                q = q.Where(l => l.Location.ToLower().Contains(loc) || l.Address.ToLower().Contains(loc));
            }

            if (minExperience.HasValue && minExperience.Value > 0)
            {
                q = q.Where(l => l.ExperienceYears >= minExperience.Value);
            }

            return await q.OrderByDescending(l => l.ExperienceYears).ToListAsync();
        }

        public async Task<bool> UpdateProfileAsync(LawyerProfile profile)
        {
            var existing = await _context.LawyerProfiles.FirstOrDefaultAsync(l => l.LawyerId == profile.LawyerId);
            if (existing == null) return false;

            existing.RegistrationNumber = profile.RegistrationNumber;
            existing.Qualification = profile.Qualification;
            existing.ExperienceYears = profile.ExperienceYears;
            existing.SpecializationId = profile.SpecializationId;
            existing.Location = profile.Location;
            existing.Address = profile.Address;
            existing.Description = profile.Description;
            existing.ConsultationFee = profile.ConsultationFee;
            if (!string.IsNullOrEmpty(profile.ProfilePhoto))
            {
                existing.ProfilePhoto = profile.ProfilePhoto;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Availability>> GetLawyerAvailabilitiesAsync(int lawyerId)
        {
            return await _context.Availabilities
                .Where(a => a.LawyerId == lawyerId)
                .OrderBy(a => a.DayOfWeek)
                .ThenBy(a => a.StartTime)
                .ToListAsync();
        }

        public async Task<bool> SaveAvailabilitySlotsAsync(int lawyerId, List<Availability> slots)
        {
            var existing = await _context.Availabilities.Where(a => a.LawyerId == lawyerId).ToListAsync();
            _context.Availabilities.RemoveRange(existing);

            foreach (var slot in slots)
            {
                slot.LawyerId = lawyerId;
                _context.Availabilities.Add(slot);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<LawyerProfile>> GetPendingVerificationLawyersAsync()
        {
            return await _context.LawyerProfiles
                .Include(l => l.User)
                .Include(l => l.Specialization)
                .Where(l => l.VerificationStatus == VerificationStatus.Pending)
                .OrderBy(l => l.User.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<LawyerProfile>> GetAllLawyersAsync()
        {
            return await _context.LawyerProfiles
                .Include(l => l.User)
                .Include(l => l.Specialization)
                .OrderByDescending(l => l.LawyerId)
                .ToListAsync();
        }

        public async Task<(bool Success, string Message)> VerifyLawyerAsync(int lawyerId, bool approve, string? reason)
        {
            var lawyer = await _context.LawyerProfiles
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.LawyerId == lawyerId);

            if (lawyer == null)
            {
                return (false, "Lawyer not found.");
            }

            if (approve)
            {
                lawyer.VerificationStatus = VerificationStatus.Approved;
                lawyer.RejectionReason = null;
                await _context.SaveChangesAsync();

                await _notificationService.CreateNotificationAsync(
                    lawyer.UserId,
                    "Profile Verified and Approved",
                    "Congratulations! Your lawyer profile has been verified and approved by administration. You are now publicly searchable for clients."
                );

                return (true, "Lawyer profile approved successfully.");
            }
            else
            {
                lawyer.VerificationStatus = VerificationStatus.Rejected;
                lawyer.RejectionReason = reason ?? "Profile verification did not meet platform guidelines.";
                await _context.SaveChangesAsync();

                await _notificationService.CreateNotificationAsync(
                    lawyer.UserId,
                    "Profile Verification Update",
                    $"Your lawyer profile verification was not approved. Reason: {lawyer.RejectionReason}"
                );

                return (true, "Lawyer profile rejected.");
            }
        }
    }
}
