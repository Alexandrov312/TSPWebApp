using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp23621759.Helpers;
using WebApp23621759.Models.ViewModel.Notifications;
using WebApp23621759.Services;

namespace WebApp23621759.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly AppNotificationService _appNotificationService;

        public NotificationController(AppNotificationService appNotificationService)
        {
            _appNotificationService = appNotificationService;
        }

        [HttpGet]
        public IActionResult Bell()
        {
            int userId = UserHelper.GetUserId(User);
            NotificationBellViewModel model = new()
            {
                UnreadCount = _appNotificationService.GetUnreadCount(userId),
                Notifications = _appNotificationService.GetLatestUnreadNotifications(userId)
            };

            return PartialView("_NotificationBell", model);
        }

        [HttpPost]
        public IActionResult MarkAsRead(int id)
        {
            int userId = UserHelper.GetUserId(User);
            bool success = _appNotificationService.MarkAsRead(id, userId);

            return Json(new
            {
                success,
                unreadCount = _appNotificationService.GetUnreadCount(userId)
            });
        }

        [HttpPost]
        public IActionResult MarkAllAsRead()
        {
            int userId = UserHelper.GetUserId(User);
            _appNotificationService.MarkAllAsRead(userId);

            return Json(new
            {
                success = true,
                unreadCount = 0
            });
        }
    }
}
