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

            if (model.SubTasks.Count > 10)
            {
                ModelState.AddModelError("", "You can add up to 10 subtasks.");
            }

            for (int i = 0; i < model.SubTasks.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(model.SubTasks[i].Title))
                {
                    ModelState.AddModelError($"SubTasks[{i}].Title", "Subtask title is required.");
                }
            }

            bool HasCycle(int start, List<CreateSubTaskInputModel> tasks)
            {
                var visited = new HashSet<int>();

                bool Dfs(int current)
                {
                    if (current == start) return true;
                    if (visited.Contains(current)) return false;

                    visited.Add(current);

                    var dep = tasks[current].BlockedByIndex;
                    if (!dep.HasValue) return false;

                    return Dfs(dep.Value);
                }

                var first = tasks[start].BlockedByIndex;
                if (!first.HasValue) return false;

                return Dfs(first.Value);
            }

            for (int i = 0; i < model.SubTasks.Count; i++)
            {
                if (HasCycle(i, model.SubTasks))
                {
                    ModelState.AddModelError($"SubTasks[{i}].BlockedByIndex", "Circular dependency detected.");
                }
            }

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

            foreach (var subTask in model.SubTasks)
            {
                _subTaskService.CreateSubTask(
                    subTask.Title,
                    subTask.Description,
                    createdTask.Id,
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