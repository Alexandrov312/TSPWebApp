using WebApp23621759.Models.Entities;

namespace WebApp23621759.Models.ViewModel.Notifications
{
    //Модел за камбанката и dropdown панела с in-app нотификации.
    public class NotificationBellViewModel
    {
        public int UnreadCount { get; set; }
        public List<AppNotificationItem> Notifications { get; set; } = new();
    }
}
