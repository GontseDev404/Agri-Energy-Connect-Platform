using Microsoft.EntityFrameworkCore;
using ST10038937_prog7311_poe1.Data;
using ST10038937_prog7311_poe1.Models;
using Microsoft.AspNetCore.Identity;

namespace ST10038937_prog7311_poe1.Services
{
    public interface INotificationService
    {
        Task<Notification> CreateNotificationAsync(string userId, string title, string message, NotificationType type, NotificationPriority priority = NotificationPriority.Normal, string? relatedEntityType = null, string? relatedEntityId = null);
        Task<List<Notification>> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 20);
        Task<int> GetUnreadNotificationCountAsync(string userId);
        Task MarkAsReadAsync(int notificationId, string userId);
        Task MarkAllAsReadAsync(string userId);
        Task DeleteNotificationAsync(int notificationId, string userId);
        Task<List<Notification>> GetRecentNotificationsAsync(string userId, int count = 5);
        Task NotifyAllUsersAsync(string title, string message, NotificationType type, NotificationPriority priority = NotificationPriority.Normal);
        Task NotifyUsersByRoleAsync(string title, string message, NotificationType type, string role, NotificationPriority priority = NotificationPriority.Normal);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<Notification> CreateNotificationAsync(string userId, string title, string message, NotificationType type, NotificationPriority priority = NotificationPriority.Normal, string? relatedEntityType = null, string? relatedEntityId = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                Priority = priority,
                RelatedEntityType = relatedEntityType,
                RelatedEntityId = relatedEntityId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return notification;
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 20)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetUnreadNotificationCountAsync(string userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();
        }

        public async Task MarkAsReadAsync(int notificationId, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteNotificationAsync(int notificationId, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Notification>> GetRecentNotificationsAsync(string userId, int count = 5)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task NotifyAllUsersAsync(string title, string message, NotificationType type, NotificationPriority priority = NotificationPriority.Normal)
        {
            var allUsers = await _userManager.Users.ToListAsync();
            
            foreach (var user in allUsers)
            {
                await CreateNotificationAsync(user.Id, title, message, type, priority);
            }
        }

        public async Task NotifyUsersByRoleAsync(string title, string message, NotificationType type, string role, NotificationPriority priority = NotificationPriority.Normal)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role);
            
            foreach (var user in usersInRole)
            {
                await CreateNotificationAsync(user.Id, title, message, type, priority);
            }
        }
    }
} 