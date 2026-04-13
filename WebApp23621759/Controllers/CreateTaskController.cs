using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp23621759.Enums;
using WebApp23621759.Helpers;
using WebApp23621759.Models.ViewModel;
using WebApp23621759.Services;

namespace WebApp23621759.Controllers
{
    [Authorize]
    public class CreateTaskController : Controller
    {
        private readonly TaskService _taskService;
        private readonly SubTaskService _subTaskService;

        public CreateTaskController(TaskService taskService, SubTaskService subTaskService)
        {
            _taskService = taskService;
            _subTaskService = subTaskService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var model = new CreateTaskViewModel
            {
                DueDate = DateTime.Now
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult Index(CreateTaskViewModel model)
        {
            model.SubTasks ??= new List<CreateSubTaskInputModel>();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            int userId = UserHelper.GetUserId(HttpContext.User);

            var createdTask = _taskService.CreateTask(
                model.Title,
                model.Description,
                model.DueDate,
                model.Priority,
                userId);

            var createdSubTasks = new List<WebApp23621759.Models.Entities.SubTaskItem>();

            foreach (var subTask in model.SubTasks)
            {
                createdSubTasks.Add(_subTaskService.CreateSubTask(
                    subTask.Title,
                    subTask.Description,
                    createdTask.Id,
                    userId));
            }

            for (int i = 0; i < model.SubTasks.Count; i++)
            {
                int? blockedByIndex = model.SubTasks[i].BlockedByIndex;
                if (!blockedByIndex.HasValue || blockedByIndex.Value < 0 || blockedByIndex.Value >= createdSubTasks.Count)
                {
                    continue;
                }

                _subTaskService.UpdateDependency(
                    createdSubTasks[i].Id,
                    createdSubTasks[blockedByIndex.Value].Id,
                    userId);
            }

            NotificationHelper.AddNotification(
                TempData,
                $"Task \"{model.Title}\" created successfully!",
                NotificationType.Success);

            return RedirectToAction("Index");
        }
    }
}
