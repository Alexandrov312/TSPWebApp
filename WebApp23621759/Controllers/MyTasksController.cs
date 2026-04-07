using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp23621759.Enums;
using WebApp23621759.Helpers;
using WebApp23621759.Models.Entities;
using WebApp23621759.Models.ViewModel;
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
        public IActionResult Delete(int id)
        {
            int userId = UserHelper.GetUserId(User);
            string title = _taskService.GetById(id, userId).Title;
            _taskService.DeleteTask(id);
            NotificationHelper.AddNotification(TempData, "Task \""+title+"\" deleted!", NotificationType.Success);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Update(EditTaskModel model)
        {
            if (!ModelState.IsValid)
            {
                NotificationHelper.AddNotification(TempData, "Invalid task data.", NotificationType.Error);
                return RedirectToAction("Index");
            }

            int userId = UserHelper.GetUserId(User);

            bool updated = _taskService.UpdateTask(model, userId);

            if (!updated)
            {
                NotificationHelper.AddNotification(TempData, "Task was not found or does not belong to you.", NotificationType.Error);
                return RedirectToAction("Index");
            }

            NotificationHelper.AddNotification(TempData, $"Task \"{model.Title}\" updated!", NotificationType.Success);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult MarkAsDone(int id)
        {
            int userId = UserHelper.GetUserId(User);

            bool updated = _taskService.SetCompleted(id, userId);

            if (!updated)
            {
                NotificationHelper.AddNotification(TempData, "Task was not found or does not belong to you.", NotificationType.Error);
                return RedirectToAction("Index");
            }

            string title = _taskService.GetById(id, userId).Title;

            NotificationHelper.AddNotification(TempData, $"Task \""+title+"\" completed!", NotificationType.Success);
            return RedirectToAction("Index");
        }
    }
}
