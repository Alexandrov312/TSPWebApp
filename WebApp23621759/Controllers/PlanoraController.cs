using Microsoft.AspNetCore.Mvc;
using WebApp23621759.Enums;
using WebApp23621759.Helpers;

namespace WebApp23621759.Controllers
{
    public abstract class PlanoraController : Controller
    {
        //Еднакъв JSON формат за AJAX грешки, за да не се дублира във всеки контролер.
        protected JsonResult JsonError(string message)
        {
            return Json(new
            {
                success = false,
                message,
                notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Error)
            });
        }
    }
}
