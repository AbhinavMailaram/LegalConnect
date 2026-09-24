using System.Collections.Generic;
using System.Threading.Tasks;
using LegalConnect.Web.Models.Entities;

namespace LegalConnect.Web.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(string userId, string title, string message);
        Task<List<Notification>> GetUserNotificationsAsync(string userId, int count = 10);
        Task<int> GetUnreadCountAsync(string userId);
        Task MarkAsReadAsync(int notificationId, string userId);
        Task MarkAllAsReadAsync(string userId);
    }
}
