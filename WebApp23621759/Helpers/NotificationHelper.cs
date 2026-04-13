using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Text.Json;
using WebApp23621759.Enums;
using WebApp23621759.Models.Common;

namespace WebApp23621759.Helpers
{
    public class NotificationHelper
    {
        //Чрез този ключ се записват нотификациите в TempData
        private const string Key = "Notifications";
        //ITempDataDictionary tempData - съхранява данни между HTTP request-ове
        //server-side
        public static void AddNotification(ITempDataDictionary tempData, string message, NotificationType type)
        {
            List<Notification> notifications;

            if (tempData.ContainsKey(Key))
            {
                notifications = JsonSerializer.Deserialize<List<Notification>>(tempData[Key].ToString());
            }
            else
            {
                notifications = new List<Notification>();
            }

            notifications.Add(new Notification
            {
                Message = message,
                Type = type
            });

            tempData[Key] = JsonSerializer.Serialize(notifications);
        }

        public static string GetCssClass(NotificationType type)
        {
            return type switch
            {
                NotificationType.Success => "toast-success",
                NotificationType.Error => "toast-error",
                NotificationType.Info => "toast-info",
                _ => ""
            };
        }
    }
}
