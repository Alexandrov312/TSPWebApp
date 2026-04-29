using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp23621759.Enums;
using WebApp23621759.Helpers;
using WebApp23621759.Models.Entities;
using WebApp23621759.Models.ViewModel.Tasks;
using WebApp23621759.Services;

namespace WebApp23621759.Controllers
{
    [Authorize]
    public class ArchiveController : PlanoraController
    {
        private readonly TaskService _taskService;
        private readonly SubTaskService _subTaskService;

        public ArchiveController(TaskService taskService, SubTaskService subTaskService)
        {
            _taskService = taskService;
            _subTaskService = subTaskService;
        }

        //ÐŸÐ¾ÐºÐ°Ð·Ð²Ð° ÑÐ°Ð¼Ð¾ Ð°Ñ€Ñ…Ð¸Ð²Ð¸Ñ€Ð°Ð½Ð¸Ñ‚Ðµ Ð³Ð»Ð°Ð²Ð½Ð¸ Ð·Ð°Ð´Ð°Ñ‡Ð¸ Ð½Ð° Ñ‚ÐµÐºÑƒÑ‰Ð¸Ñ Ð¿Ð¾Ñ‚Ñ€ÐµÐ±Ð¸Ñ‚ÐµÐ».
        public IActionResult Index(string sortBy = "completedAt", string direction = "DESC", string? sort = null)
        {
            int userId = UserHelper.GetUserId(User);
            string sortRules = string.IsNullOrWhiteSpace(sort) ? $"{sortBy}:{direction}" : sort;
            var tasks = _taskService.GetArchivedTasksByUserId(userId, sortRules)
                .Select(BuildTaskItemViewModel)
                .ToList();

            ViewBag.CurrentSortBy = sortBy;
            ViewBag.CurrentDirection = direction;
            ViewBag.CurrentSort = sortRules;
            return View(tasks);
        }

        [HttpPost]
        public IActionResult Restore(int id)
        {
            int userId = UserHelper.GetUserId(User);
            var task = _taskService.GetById(id, userId);
            if (task == null || !task.IsArchived)
            {
                return JsonError("Archived task was not found.");
            }

            if (!_taskService.RestoreTask(id, userId))
            {
                return JsonError("Task could not be restored.");
            }

            return Json(new
            {
                success = true,
                taskId = id,
                message = $"Task \"{task.Title}\" restored.",
                notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Success)
            });
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            int userId = UserHelper.GetUserId(User);
            var task = _taskService.GetById(id, userId);
            if (task == null || !task.IsArchived)
            {
                return JsonError("Archived task was not found.");
            }

            _taskService.DeleteTask(id);

            return Json(new
            {
                success = true,
                taskId = id,
                message = $"Task \"{task.Title}\" deleted.",
                notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Success)
            });
        }

        private MyTaskItemViewModel BuildTaskItemViewModel(TaskItem task)
        {
            List<SubTaskItem> subTasks = _subTaskService.GetAllSubTasks(task.Id);
            return TaskViewModelHelper.BuildTaskItemViewModel(task, subTasks);
        }

    }
}
