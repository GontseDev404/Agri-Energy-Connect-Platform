using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ST10038937_prog7311_poe1.Models;
using ST10038937_prog7311_poe1.Services;
using ST10038937_prog7311_poe1.Data;

namespace ST10038937_prog7311_poe1.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;

        public NotificationController(
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService)
        {
            _notificationService = notificationService;
            _userManager = userManager;
            _auditService = auditService;
        }

        // GET: Notification
        public async Task<IActionResult> Index(int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var notifications = await _notificationService.GetUserNotificationsAsync(user.Id, page, 20);
            var unreadCount = await _notificationService.GetUnreadNotificationCountAsync(user.Id);

            ViewBag.CurrentPage = page;
            ViewBag.UnreadCount = unreadCount;
            ViewBag.TotalPages = (int)Math.Ceiling((double)unreadCount / 20);

            return View(notifications);
        }

        // GET: Notification/Partial
        [HttpGet]
        public async Task<IActionResult> Partial()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var notifications = await _notificationService.GetRecentNotificationsAsync(user.Id, 5);
            var unreadCount = await _notificationService.GetUnreadNotificationCountAsync(user.Id);

            ViewBag.UnreadCount = unreadCount;
            return PartialView("_NotificationPartial", notifications);
        }

        // POST: Notification/MarkAsRead
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            await _notificationService.MarkAsReadAsync(id, user.Id);
            await _auditService.LogActionAsync(user.Id, "Mark Notification Read", $"Notification ID: {id}");

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var unreadCount = await _notificationService.GetUnreadNotificationCountAsync(user.Id);
                return Json(new { success = true, unreadCount });
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Notification/MarkAllAsRead
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            await _notificationService.MarkAllAsReadAsync(user.Id);
            await _auditService.LogActionAsync(user.Id, "Mark All Notifications Read", "All notifications marked as read");

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, unreadCount = 0 });
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Notification/Delete
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            await _notificationService.DeleteNotificationAsync(id, user.Id);
            await _auditService.LogActionAsync(user.Id, "Delete Notification", $"Notification ID: {id}");

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var unreadCount = await _notificationService.GetUnreadNotificationCountAsync(user.Id);
                return Json(new { success = true, unreadCount });
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Notification/UnreadCount (AJAX)
        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var count = await _notificationService.GetUnreadNotificationCountAsync(user.Id);
            return Json(new { count });
        }
    }
} 