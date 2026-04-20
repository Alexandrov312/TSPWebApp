using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp23621759.Enums;
using WebApp23621759.Helpers;
using WebApp23621759.Models.ViewModel.Tasks;
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
            NormalizeSubTaskDescriptions(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var validationResult = _subTaskService.ValidateNewSubTasks(model.SubTasks);
            if (!validationResult.IsValid)
            {
                NotificationHelper.AddNotification(
                    TempData,
                    validationResult.ErrorMessage,
                    NotificationType.Error);

                return View(model);
            }

            int userId = UserHelper.GetUserId(HttpContext.User);

            var createdTask = _taskService.CreateTask(
                model.Title,
                model.Description,
                model.DueDate,
                model.Priority,
                userId);

            _subTaskService.CreateSubTasks(model.SubTasks, createdTask.Id, userId);

            NotificationHelper.AddNotification(
                TempData,
                $"Task \"{model.Title}\" created successfully!",
                NotificationType.Success);

            return RedirectToAction("Index");
        }

        private static void NormalizeSubTaskDescriptions(CreateTaskViewModel model)
        {
            foreach (var subTask in model.SubTasks)
            {
                if (string.IsNullOrWhiteSpace(subTask.Description))
                {
                    subTask.Description = "No description";
                }
            }
        }
    }
}
