using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp23621759.Enums;
using WebApp23621759.Helpers;
using WebApp23621759.Models.Entities;
using WebApp23621759.Services;

namespace WebApp23621759.Controllers
{
    [Authorize]
    public class MyTasksController : Controller
    {
        private readonly TaskService _taskService;
        public MyTasksController (TaskService taskService)
        {
            _taskService = taskService;
        }
        public IActionResult Index(string sortBy = "dueDate", string direction = "ASC")
        {
            int userId = UserHelper.GetUserId(User);
            var tasks = _taskService.GetAllTasksByUserId(userId, sortBy, direction);

            ViewBag.CurrentSortBy = sortBy;
            ViewBag.CurrentDirection = direction;
            return View(tasks);
        }

        [HttpPost]
        public IActionResult Delete(int id, string title)
        {
            _taskService.DeleteTask(id);
            NotificationHelper.AddNotification(TempData, "Task \""+title+"\" deleted!", NotificationType.Success);
            return RedirectToAction("Index");
        }
    }
}
