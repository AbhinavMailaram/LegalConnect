using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LegalConnect.Web.Data;
using LegalConnect.Web.Models.Entities;
using LegalConnect.Web.Models.ViewModels;

namespace LegalConnect.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SpecializationController : Controller
    {
        private readonly LegalConnectDbContext _context;

        public SpecializationController(LegalConnectDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _context.Specializations
                .Include(s => s.Lawyers)
                .OrderBy(s => s.Name)
                .Select(s => new SpecializationCrudViewModel
                {
                    SpecializationId = s.SpecializationId,
                    Name = s.Name,
                    Description = s.Description,
                    AssociatedLawyersCount = s.Lawyers.Count
                })
                .ToListAsync();

            return View(list);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new SpecializationCrudViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SpecializationCrudViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var exists = await _context.Specializations.AnyAsync(s => s.Name.ToLower() == model.Name.Trim().ToLower());
            if (exists)
            {
                ModelState.AddModelError("Name", "A specialization with this name already exists.");
                return View(model);
            }

            var spec = new Specialization
            {
                Name = model.Name.Trim(),
                Description = model.Description?.Trim()
            };

            _context.Specializations.Add(spec);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Specialization '{spec.Name}' created successfully.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var spec = await _context.Specializations.FindAsync(id);
            if (spec == null) return NotFound();

            var vm = new SpecializationCrudViewModel
            {
                SpecializationId = spec.SpecializationId,
                Name = spec.Name,
                Description = spec.Description
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SpecializationCrudViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var spec = await _context.Specializations.FindAsync(model.SpecializationId);
            if (spec == null) return NotFound();

            var duplicate = await _context.Specializations.AnyAsync(s => s.SpecializationId != model.SpecializationId && s.Name.ToLower() == model.Name.Trim().ToLower());
            if (duplicate)
            {
                ModelState.AddModelError("Name", "Another specialization with this name already exists.");
                return View(model);
            }

            spec.Name = model.Name.Trim();
            spec.Description = model.Description?.Trim();

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Specialization '{spec.Name}' updated successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var spec = await _context.Specializations.Include(s => s.Lawyers).FirstOrDefaultAsync(s => s.SpecializationId == id);
            if (spec == null) return NotFound();

            if (spec.Lawyers.Any())
            {
                TempData["ErrorMessage"] = $"Cannot delete '{spec.Name}' because it is linked to {spec.Lawyers.Count} lawyer profile(s). Reassign them first.";
                return RedirectToAction("Index");
            }

            _context.Specializations.Remove(spec);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Specialization '{spec.Name}' was deleted.";
            return RedirectToAction("Index");
        }
    }
}
