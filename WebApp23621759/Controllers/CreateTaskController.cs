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

        public CreateTaskController(TaskService taskService)
        {
            _taskService = taskService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(CreateTaskViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            int userId = UserHelper.GetUserId(HttpContext.User);
            _taskService.CreateTask(model.Title, model.Description, 
                model.DueDate, model.Priority, userId);

            NotificationHelper.AddNotification(TempData, "Task \""+model.Title+"\" created successfully!", NotificationType.Success);

            return RedirectToAction("Index");
        }
    }
}
