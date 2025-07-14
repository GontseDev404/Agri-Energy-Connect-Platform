using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ST10038937_prog7311_poe1.Models;
using ST10038937_prog7311_poe1.Services;
using ST10038937_prog7311_poe1.Data;
using Microsoft.EntityFrameworkCore;

namespace ST10038937_prog7311_poe1.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class NotificationApiController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;

        public NotificationApiController(
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService)
        {
            _notificationService = notificationService;
            _userManager = userManager;
            _auditService = auditService;
        }

        // GET: api/notification
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Notification>>> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var notifications = await _notificationService.GetUserNotificationsAsync(user.Id, page, pageSize);
            return Ok(notifications);
        }

        // GET: api/notification/unread-count
        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var count = await _notificationService.GetUnreadNotificationCountAsync(user.Id);
            return Ok(count);
        }

        // GET: api/notification/recent
        [HttpGet("recent")]
        public async Task<ActionResult<IEnumerable<Notification>>> GetRecentNotifications([FromQuery] int count = 5)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var notifications = await _notificationService.GetRecentNotificationsAsync(user.Id, count);
            return Ok(notifications);
        }

        // PUT: api/notification/{id}/mark-read
        [HttpPut("{id}/mark-read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            await _notificationService.MarkAsReadAsync(id, user.Id);
            await _auditService.LogActionAsync(user.Id, "Mark Notification Read", $"Notification ID: {id}");
            
            return NoContent();
        }

        // PUT: api/notification/mark-all-read
        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            await _notificationService.MarkAllAsReadAsync(user.Id);
            await _auditService.LogActionAsync(user.Id, "Mark All Notifications Read", "All notifications marked as read");
            
            return NoContent();
        }

        // DELETE: api/notification/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            await _notificationService.DeleteNotificationAsync(id, user.Id);
            await _auditService.LogActionAsync(user.Id, "Delete Notification", $"Notification ID: {id}");
            
            return NoContent();
        }

        // POST: api/notification (Admin only - for creating notifications)
        [HttpPost]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<ActionResult<Notification>> CreateNotification([FromBody] CreateNotificationRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var notification = await _notificationService.CreateNotificationAsync(
                request.UserId,
                request.Title,
                request.Message,
                request.Type,
                request.Priority,
                request.RelatedEntityType,
                request.RelatedEntityId);

            await _auditService.LogActionAsync(user.Id, "Create Notification", $"Notification for user: {request.UserId}, Title: {request.Title}");

            return CreatedAtAction(nameof(GetNotifications), new { id = notification.Id }, notification);
        }

        // POST: api/notification/notify-all (Admin only)
        [HttpPost("notify-all")]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> NotifyAllUsers([FromBody] NotifyAllRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            await _notificationService.NotifyAllUsersAsync(
                request.Title,
                request.Message,
                request.Type,
                request.Priority);

            await _auditService.LogActionAsync(user.Id, "Notify All Users", $"Title: {request.Title}");

            return NoContent();
        }

        // POST: api/notification/notify-role (Admin only)
        [HttpPost("notify-role")]
        [Authorize(Roles = "Employee,Admin")]
        public async Task<IActionResult> NotifyUsersByRole([FromBody] NotifyRoleRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            await _notificationService.NotifyUsersByRoleAsync(
                request.Title,
                request.Message,
                request.Type,
                request.Role,
                request.Priority);

            await _auditService.LogActionAsync(user.Id, "Notify Users By Role", $"Role: {request.Role}, Title: {request.Title}");

            return NoContent();
        }
    }

    public class CreateNotificationRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        public string? RelatedEntityType { get; set; }
        public string? RelatedEntityId { get; set; }
    }

    public class NotifyAllRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    }

    public class NotifyRoleRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public string Role { get; set; } = string.Empty;
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    }
} 