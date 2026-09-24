using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LegalConnect.Web.Data;
using LegalConnect.Web.Models.Entities;

namespace LegalConnect.Web.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class LawyersApiController : ControllerBase
    {
        private readonly LegalConnectDbContext _context;

        public LawyersApiController(LegalConnectDbContext context)
        {
            _context = context;
        }

        // GET: api/LawyersApi
        [HttpGet]
        public async Task<IActionResult> GetApprovedLawyers([FromQuery] string? search, [FromQuery] int? specializationId)
        {
            var query = _context.LawyerProfiles
                .Include(l => l.User)
                .Include(l => l.Specialization)
                .Where(l => l.VerificationStatus == VerificationStatus.Approved && l.User.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(l => l.User.FullName.ToLower().Contains(term) || l.Location.ToLower().Contains(term));
            }

            if (specializationId.HasValue && specializationId.Value > 0)
            {
                query = query.Where(l => l.SpecializationId == specializationId.Value);
            }

            var results = await query.Select(l => new
            {
                l.LawyerId,
                FullName = l.User.FullName,
                Specialization = l.Specialization != null ? l.Specialization.Name : null,
                l.ExperienceYears,
                l.Location,
                l.ConsultationFee,
                l.Qualification,
                l.Description
            }).ToListAsync();

            return Ok(results);
        }

        // GET: api/LawyersApi/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetLawyer(int id)
        {
            var lawyer = await _context.LawyerProfiles
                .Include(l => l.User)
                .Include(l => l.Specialization)
                .Include(l => l.Availabilities)
                .Where(l => l.LawyerId == id && l.VerificationStatus == VerificationStatus.Approved)
                .Select(l => new
                {
                    l.LawyerId,
                    FullName = l.User.FullName,
                    Email = l.User.Email,
                    Phone = l.User.Phone,
                    Specialization = l.Specialization != null ? l.Specialization.Name : null,
                    l.ExperienceYears,
                    l.Location,
                    l.Address,
                    l.ConsultationFee,
                    l.Qualification,
                    l.Description,
                    Availabilities = l.Availabilities.Where(a => a.IsAvailable).Select(a => new
                    {
                        DayOfWeek = a.DayOfWeek.ToString(),
                        StartTime = a.StartTime.ToString(@"hh\:mm"),
                        EndTime = a.EndTime.ToString(@"hh\:mm")
                    })
                })
                .FirstOrDefaultAsync();

            if (lawyer == null) return NotFound();
            return Ok(lawyer);
        }
    }
}
