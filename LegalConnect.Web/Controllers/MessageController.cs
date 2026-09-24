using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LegalConnect.Web.Data;
using LegalConnect.Web.Models.Entities;
using LegalConnect.Web.Models.ViewModels;
using LegalConnect.Web.Services;

namespace LegalConnect.Web.Controllers
{
    [Authorize]
    public class MessageController : Controller
    {
        private readonly LegalConnectDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessageController(
            LegalConnectDbContext context,
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _notificationService = notificationService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // Get all appointment threads involving the current user
            var userAppointments = await _context.Appointments
                .Include(a => a.Client).ThenInclude(c => c.User)
                .Include(a => a.Lawyer).ThenInclude(l => l.User)
                .Include(a => a.Messages)
                .Where(a => a.Client.UserId == currentUserId || a.Lawyer.UserId == currentUserId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            ViewBag.CurrentUserId = currentUserId;
            return View(userAppointments);
        }

        public async Task<IActionResult> Thread(int appointmentId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var appointment = await _context.Appointments
                .Include(a => a.Client).ThenInclude(c => c.User)
                .Include(a => a.Lawyer).ThenInclude(l => l.User)
                .Include(a => a.Messages).ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (appointment == null) return NotFound();

            var isClient = appointment.Client.UserId == currentUserId;
            var isLawyer = appointment.Lawyer.UserId == currentUserId;
            var isAdmin = User.IsInRole("Admin");

            if (!isClient && !isLawyer && !isAdmin)
            {
                return Forbid();
            }

            var otherUser = isClient ? appointment.Lawyer.User : appointment.Client.User;
            var otherRole = isClient ? "Lawyer" : "Client";

            // Mark unread messages sent to current user as read
            var unread = appointment.Messages.Where(m => m.ReceiverId == currentUserId && !m.IsRead).ToList();
            if (unread.Any())
            {
                foreach (var msg in unread)
                {
                    msg.IsRead = true;
                }
                await _context.SaveChangesAsync();
            }

            var vm = new MessageThreadViewModel
            {
                AppointmentId = appointmentId,
                Appointment = appointment,
                CurrentUserId = currentUserId,
                OtherUserId = otherUser.Id,
                OtherUserName = otherUser.FullName,
                OtherUserRole = otherRole,
                Messages = appointment.Messages.OrderBy(m => m.SentAt).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int appointmentId, string receiverId, string messageText)
        {
            if (string.IsNullOrWhiteSpace(messageText))
            {
                TempData["ErrorMessage"] = "Message cannot be empty.";
                return RedirectToAction("Thread", new { appointmentId });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            if (currentUser == null) return Unauthorized();

            var message = new Message
            {
                AppointmentId = appointmentId,
                SenderId = currentUserId,
                ReceiverId = receiverId,
                MessageText = messageText.Trim(),
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Trigger notification to recipient
            await _notificationService.CreateNotificationAsync(
                receiverId,
                "New Message Received",
                $"{currentUser.FullName} sent you a message: \"{(messageText.Length > 40 ? messageText.Substring(0, 37) + "..." : messageText)}\""
            );

            return RedirectToAction("Thread", new { appointmentId });
        }
    }
}
