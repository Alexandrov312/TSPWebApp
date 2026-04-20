using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp23621759.Enums;
using WebApp23621759.Helpers;
using WebApp23621759.Models.Entities;
using WebApp23621759.Models.ViewModel.SubTasks;
using WebApp23621759.Models.ViewModel.Tasks;
using WebApp23621759.Services;

namespace WebApp23621759.Controllers
{
    [Authorize]
    public class MyTasksController : Controller
    {
        private readonly TaskService _taskService;
        private readonly SubTaskService _subTaskService;

        public MyTasksController(TaskService taskService, SubTaskService subTaskService)
        {
            _taskService = taskService;
            _subTaskService = subTaskService;
        }

        //Зарежда страницата с всички задачи на текущия потребител
        public IActionResult Index(string sortBy = "dueDate", string direction = "ASC", int? kanbanTaskId = null, string source = "mytasks", string? returnUrl = null)
        {
            int userId = UserHelper.GetUserId(User);

            //Взима задачите на потребителя според избраната подредба
            var tasks = _taskService.GetAllTasksByUserId(userId, sortBy, direction);

            //Подготвя view model за всяка задача, включително подзадачите ѝ
            var taskViewModels = tasks.Select(task => BuildTaskItemViewModel(task)).ToList();

            ViewBag.CurrentSortBy = sortBy;
            ViewBag.CurrentDirection = direction;
            ViewBag.InitialKanbanTaskId = kanbanTaskId;
            ViewBag.KanbanSource = source;
            ViewBag.KanbanReturnUrl = returnUrl;
            return View(taskViewModels);
        }

        //Зарежда панела с подзадачите на конкретна задача
        [HttpGet]
        public IActionResult SubTasksPanel(int taskId)
        {
            int userId = UserHelper.GetUserId(User);
            var task = _taskService.GetById(taskId, userId);
            if (task == null)
            {
                return NotFound();
            }

            //Връща се малка част от HTML-а, не цялата страница
            return PartialView("_TaskSubTasksPanel", BuildTaskItemViewModel(task));
        }

        [HttpGet]
        public IActionResult KanbanPanel(int taskId, string source = "mytasks", string? returnUrl = null)
        {
            int userId = UserHelper.GetUserId(User);
            var task = _taskService.GetById(taskId, userId);
            if (task == null)
            {
                return NotFound();
            }

            ViewBag.KanbanSource = source;
            ViewBag.KanbanReturnUrl = returnUrl;
            return PartialView("_TaskKanbanPanel", BuildTaskItemViewModel(task));
        }

        [HttpPost]
        public IActionResult Create()
        {
            int userId = UserHelper.GetUserId(User);
            var createdTask = _taskService.CreateTask(
                "New task",
                "Task description",
                DateTime.Now.AddDays(1),
                Priority.Medium,
                userId);

            if (createdTask == null)
            {
                return JsonError("Task could not be created.");
            }

            return Json(new
            {
                success = true,
                taskId = createdTask.Id,
                message = "New task added.",
                notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Success)
            });
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            int userId = UserHelper.GetUserId(User);
            var task = _taskService.GetById(id, userId);
            if (task == null)
            {
                return JsonError("Task was not found or does not belong to you.");
            }

            string title = task.Title;
            _taskService.DeleteTask(id);

            return Json(new
            {
                success = true,
                taskId = id,
                message = $"Task \"{title}\" deleted!",
                notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Success)
            });
        }

        [HttpPost]
        public IActionResult Update(EditTaskViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return JsonError("Invalid task data.");
            }

            int userId = UserHelper.GetUserId(User);
            var currentTask = _taskService.GetById(model.Id, userId);
            if (currentTask == null)
            {
                return JsonError("Task was not found or does not belong to you.");
            }

            //Запазва текущия статус от базата, вместо да разчита на стойност от клиента
            model.Status = (TaskStatus)(int)currentTask.Status;

            bool updated = _taskService.UpdateTask(model, userId);
            if (!updated)
            {
                return JsonError("Task was not found or does not belong to you.");
            }

            var updatedTask = _taskService.GetById(model.Id, userId);
            if (updatedTask != null)
            {
                return Json(new
                {
                    success = true,
                    taskId = updatedTask.Id,
                    message = $"Task \"{updatedTask.Title}\" updated!",
                    notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Success),
                    title = updatedTask.Title,
                    description = updatedTask.Description,
                    dueDateText = updatedTask.DueDate.ToString("dd.MM.yyyy HH:mm"),
                    dueDateValue = updatedTask.DueDate.ToString("yyyy-MM-ddTHH:mm"),
                    statusDisplayName = StatusHelper.GetDisplayName(updatedTask.Status),
                    statusValue = (int)updatedTask.Status,
                    statusCssClass = StatusHelper.GetCssClass(updatedTask.Status),
                    priorityDisplayName = updatedTask.Priority.ToString(),
                    priorityValue = (int)updatedTask.Priority,
                    priorityCssClass = PriorityHelper.GetCssClass(updatedTask.Priority),
                    completedAtText = updatedTask.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "Not finished",
                    rowDateCssClass = TaskDateHelper.GetRowDateClass(updatedTask)
                });
            }

            return JsonError("Task could not be reloaded after update.");
        }

        [HttpPost]
        public IActionResult MarkAsDone(int id)
        {
            int userId = UserHelper.GetUserId(User);
            var task = _taskService.GetById(id, userId);

            if (task == null)
            {
                return JsonError("Task was not found or does not belong to you.");
            }

            //При завършване на главната задача приключва и всички нейни подзадачи
            _subTaskService.SetAllCompletedForTask(id, userId);

            bool updated = _taskService.SetCompleted(id, userId);
            if (!updated)
            {
                return JsonError("Task was not found or does not belong to you.");
            }

            string title = task.Title;

            var refreshedTask = _taskService.GetById(id, userId);
            if (refreshedTask != null)
            {
                var taskViewModel = BuildTaskItemViewModel(refreshedTask);

                return Json(new
                {
                    success = true,
                    taskId = refreshedTask.Id,
                    message = $"Task \"{title}\" completed!",
                    notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Success),
                    statusDisplayName = StatusHelper.GetDisplayName(refreshedTask.Status),
                    statusValue = (int)refreshedTask.Status,
                    statusCssClass = StatusHelper.GetCssClass(refreshedTask.Status),
                    completedAtText = refreshedTask.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "Not finished",
                    rowDateCssClass = TaskDateHelper.GetRowDateClass(refreshedTask),
                    completionPercentage = taskViewModel.CompletionPercentage,
                    projectedCompletionPercentage = taskViewModel.ProjectedCompletionPercentage,
                    completedSubTaskCount = taskViewModel.CompletedSubTaskCount,
                    inProgressSubTaskCount = taskViewModel.InProgressSubTaskCount,
                    totalSubTaskCount = taskViewModel.TotalSubTaskCount,

                    //Връща id-тата на завършените подзадачи, за да може UI-то да се синхронизира правилно
                    completedSubTaskIds = taskViewModel.SubTasks
                        .Where(subTask => subTask.Status == Status.Completed)
                        .Select(subTask => subTask.Id)
                        .ToList()
                });
            }

            return JsonError("Task could not be reloaded after completion.");
        }

        [HttpPost]
        public IActionResult UpdateSubTask(SubTaskViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return JsonError("Invalid subtask data.");
            }

            int userId = UserHelper.GetUserId(User);
            bool updated = _subTaskService.UpdateTask(model, userId);

            if (!updated)
            {
                return JsonError("Subtask could not be updated. Check the dependency and try again.");
            }

            var updatedSubTask = _subTaskService.GetById(model.Id);
            if (updatedSubTask != null)
            {
                //След промяна на подзадача синхронизира статуса и прогреса на главната задача
                var syncedTask = _taskService.SyncStatusWithSubTasks(updatedSubTask.TaskId, userId);
                if (syncedTask == null)
                {
                    return JsonError("Task could not be reloaded after subtask update.");
                }

                var taskViewModel = BuildTaskItemViewModel(syncedTask);
                List<SubTaskItem> allSubTasks = _subTaskService.GetAllSubTasks(updatedSubTask.TaskId);

                //Намира заглавието на подзадачата, от която зависи текущата
                string? blockedByTitle = allSubTasks
                    .FirstOrDefault(candidate => candidate.Id == updatedSubTask.BlockedBySubTaskId)
                    ?.Title;

                return Json(new
                {
                    success = true,
                    taskId = updatedSubTask.TaskId,
                    subTaskId = updatedSubTask.Id,
                    message = $"Subtask \"{updatedSubTask.Title}\" updated!",
                    notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Success),
                    title = updatedSubTask.Title,
                    description = string.IsNullOrWhiteSpace(updatedSubTask.Description) ? "No description" : updatedSubTask.Description,
                    rawDescription = updatedSubTask.Description ?? string.Empty,
                    blockedBySubTaskId = updatedSubTask.BlockedBySubTaskId,
                    blockedByTitle,
                    taskStatusValue = (int)syncedTask.Status,
                    taskStatusDisplayName = StatusHelper.GetDisplayName(syncedTask.Status),
                    taskStatusCssClass = StatusHelper.GetCssClass(syncedTask.Status),
                    taskCalendarStatusCssClass = StatusHelper.GetCalendarCardClass(syncedTask.Status),
                    taskCompletedAtText = syncedTask.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "Not finished",
                    taskRowDateCssClass = TaskDateHelper.GetRowDateClass(syncedTask),
                    completionPercentage = taskViewModel.CompletionPercentage,
                    projectedCompletionPercentage = taskViewModel.ProjectedCompletionPercentage,
                    completedSubTaskCount = taskViewModel.CompletedSubTaskCount,
                    inProgressSubTaskCount = taskViewModel.InProgressSubTaskCount,
                    totalSubTaskCount = taskViewModel.TotalSubTaskCount,

                    //Връща позволените dependency опции, за да не се допускат невалидни зависимости
                    validDependencyIds = SubTaskHelper.BuildDependencyOptions(updatedSubTask, allSubTasks)
                        .Select(option => option.Id)
                        .ToList()
                });
            }

            return JsonError("Subtask could not be reloaded after update.");
        }

        [HttpPost]
        public IActionResult CycleSubTaskStatus(int id)
        {
            int userId = UserHelper.GetUserId(User);
            var subTask = _subTaskService.GetById(id);

            if (subTask == null || subTask.UserId != userId)
            {
                return JsonError("Subtask was not found or does not belong to you.");
            }

            //Изчислява какъв ще бъде следващият статус за съобщението към клиента
            Status nextStatus = StatusHelper.GetNextStatus(subTask.Status);

            bool updated = _subTaskService.ChangeStatus(id, userId);
            if (!updated)
            {
                return Json(new
                {
                    success = false,
                    message = "First complete the subtask this one depends on.",
                    notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Info)
                });
            }

            var refreshedSubTask = _subTaskService.GetById(id);
            TaskItem? parentTask = refreshedSubTask == null
                ? null
                : _taskService.SyncStatusWithSubTasks(refreshedSubTask.TaskId, userId);

            if (refreshedSubTask != null && parentTask != null)
            {
                var taskViewModel = BuildTaskItemViewModel(parentTask);

                return Json(new
                {
                    success = true,
                    taskId = refreshedSubTask.TaskId,
                    subTaskId = refreshedSubTask.Id,
                    message = $"Subtask \"{subTask.Title}\" is now {StatusHelper.GetDisplayName(nextStatus)}.",
                    notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Success),
                    statusDisplayName = StatusHelper.GetDisplayName(refreshedSubTask.Status),
                    statusCssClass = StatusHelper.GetCssClass(refreshedSubTask.Status),
                    completedAt = refreshedSubTask.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "Not finished",
                    taskStatusValue = (int)parentTask.Status,
                    taskStatusDisplayName = StatusHelper.GetDisplayName(parentTask.Status),
                    taskStatusCssClass = StatusHelper.GetCssClass(parentTask.Status),
                    taskCalendarStatusCssClass = StatusHelper.GetCalendarCardClass(parentTask.Status),
                    taskCompletedAtText = parentTask.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "Not finished",
                    taskRowDateCssClass = TaskDateHelper.GetRowDateClass(parentTask),
                    completionPercentage = taskViewModel.CompletionPercentage,
                    projectedCompletionPercentage = taskViewModel.ProjectedCompletionPercentage,
                    completedSubTaskCount = taskViewModel.CompletedSubTaskCount,
                    inProgressSubTaskCount = taskViewModel.InProgressSubTaskCount,
                    totalSubTaskCount = taskViewModel.TotalSubTaskCount
                });
            }

            return JsonError("Subtask could not be reloaded after status change.");
        }

        [HttpPost]
        public IActionResult Archive(int id)
        {
            int userId = UserHelper.GetUserId(User);
            var task = _taskService.GetById(id, userId);
            if (task == null)
            {
                return JsonError("Task was not found or does not belong to you.");
            }

            if (task.Status != Status.Completed)
            {
                return Json(new
                {
                    success = false,
                    message = "Only completed tasks can be archived.",
                    notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Info)
                });
            }

            if (!_taskService.ArchiveTask(id, userId))
            {
                return JsonError("Task could not be archived.");
            }

            return Json(new
            {
                success = true,
                taskId = id,
                message = $"Task \"{task.Title}\" archived.",
                notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Success)
            });
        }

        [HttpPost]
        public IActionResult MoveSubTaskStatus(int id, Status targetStatus)
        {
            int userId = UserHelper.GetUserId(User);
            var subTask = _subTaskService.GetById(id);

            if (subTask == null || subTask.UserId != userId)
            {
                return JsonError("Subtask was not found or does not belong to you.");
            }

            bool updated = _subTaskService.SetStatus(id, userId, targetStatus);
            if (!updated)
            {
                return Json(new
                {
                    success = false,
                    taskId = subTask.TaskId,
                    message = "First complete the subtask this one depends on.",
                    notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Info)
                });
            }

            var refreshedSubTask = _subTaskService.GetById(id);
            TaskItem? parentTask = refreshedSubTask == null
                ? null
                : _taskService.SyncStatusWithSubTasks(refreshedSubTask.TaskId, userId);

            if (refreshedSubTask != null && parentTask != null)
            {
                var taskViewModel = BuildTaskItemViewModel(parentTask);

                return Json(new
                {
                    success = true,
                    taskId = refreshedSubTask.TaskId,
                    subTaskId = refreshedSubTask.Id,
                    message = $"Subtask \"{refreshedSubTask.Title}\" moved to {StatusHelper.GetDisplayName(refreshedSubTask.Status)}.",
                    notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Success),
                    taskStatusValue = (int)parentTask.Status,
                    taskStatusDisplayName = StatusHelper.GetDisplayName(parentTask.Status),
                    taskStatusCssClass = StatusHelper.GetCssClass(parentTask.Status),
                    taskCalendarStatusCssClass = StatusHelper.GetCalendarCardClass(parentTask.Status),
                    taskCompletedAtText = parentTask.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "Not finished",
                    taskRowDateCssClass = TaskDateHelper.GetRowDateClass(parentTask),
                    completionPercentage = taskViewModel.CompletionPercentage,
                    projectedCompletionPercentage = taskViewModel.ProjectedCompletionPercentage,
                    completedSubTaskCount = taskViewModel.CompletedSubTaskCount,
                    inProgressSubTaskCount = taskViewModel.InProgressSubTaskCount,
                    totalSubTaskCount = taskViewModel.TotalSubTaskCount
                });
            }

            return JsonError("Subtask could not be reloaded after status change.");
        }

        //Строи view model за една задача заедно с всичките ѝ подзадачи
        private MyTaskItemViewModel BuildTaskItemViewModel(TaskItem task)
        {
            List<SubTaskItem> subTasks = _subTaskService.GetAllSubTasks(task.Id);
            return TaskViewModelHelper.BuildTaskItemViewModel(task, subTasks);
        }

        [HttpPost]
        public IActionResult AddSubTask(int taskId)
        {
            int userId = UserHelper.GetUserId(User);
            var task = _taskService.GetById(taskId, userId);

            if (task == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Task was not found or does not belong to you.",
                    notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Error)
                });
            }

            //Създава подзадача с начални стойности
            var createdSubTask = _subTaskService.CreateSubTask("New subtask", string.Empty, null, taskId, userId);
            if (createdSubTask == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Subtask could not be created.",
                    notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Error)
                });
            }

            var syncedTask = _taskService.SyncStatusWithSubTasks(taskId, userId);
            if (syncedTask == null)
            {
                return JsonError("Task could not be reloaded after subtask creation.");
            }

            var taskViewModel = BuildTaskItemViewModel(syncedTask);

            return Json(new
            {
                success = true,
                taskId,
                subTaskId = createdSubTask.Id,
                message = "New subtask added.",
                notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Success),
                taskStatusValue = (int)syncedTask.Status,
                taskStatusDisplayName = StatusHelper.GetDisplayName(syncedTask.Status),
                taskStatusCssClass = StatusHelper.GetCssClass(syncedTask.Status),
                taskCalendarStatusCssClass = StatusHelper.GetCalendarCardClass(syncedTask.Status),
                taskCompletedAtText = syncedTask.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "Not finished",
                taskRowDateCssClass = TaskDateHelper.GetRowDateClass(syncedTask),
                completionPercentage = taskViewModel.CompletionPercentage,
                projectedCompletionPercentage = taskViewModel.ProjectedCompletionPercentage,
                completedSubTaskCount = taskViewModel.CompletedSubTaskCount,
                inProgressSubTaskCount = taskViewModel.InProgressSubTaskCount,
                totalSubTaskCount = taskViewModel.TotalSubTaskCount
            });
        }

        [HttpPost]
        public IActionResult DeleteSubTask(int id)
        {
            int userId = UserHelper.GetUserId(User);
            var subTask = _subTaskService.GetById(id);

            if (subTask == null || subTask.UserId != userId)
            {
                return Json(new
                {
                    success = false,
                    message = "Subtask was not found or does not belong to you.",
                    notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Error)
                });
            }

            if (!_subTaskService.DeleteTask(id, userId))
            {
                return Json(new
                {
                    success = false,
                    message = "Subtask could not be deleted.",
                    notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Error)
                });
            }

            //След изтриване на подзадача преизчислява състоянието на главната задача
            var parentTask = _taskService.SyncStatusWithSubTasks(subTask.TaskId, userId);
            var taskViewModel = parentTask == null ? null : BuildTaskItemViewModel(parentTask);

            return Json(new
            {
                success = true,
                taskId = subTask.TaskId,
                message = $"Subtask \"{subTask.Title}\" deleted.",
                notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Success),

                //Ако родителската задача не може да се зареди, използва fallback Pending стойности
                taskStatusValue = parentTask == null ? (int)Status.Pending : (int)parentTask.Status,
                taskStatusDisplayName = parentTask == null ? StatusHelper.GetDisplayName(Status.Pending) : StatusHelper.GetDisplayName(parentTask.Status),
                taskStatusCssClass = parentTask == null ? StatusHelper.GetCssClass(Status.Pending) : StatusHelper.GetCssClass(parentTask.Status),
                taskCalendarStatusCssClass = parentTask == null ? StatusHelper.GetCalendarCardClass(Status.Pending) : StatusHelper.GetCalendarCardClass(parentTask.Status),
                taskCompletedAtText = parentTask?.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "Not finished",
                taskRowDateCssClass = parentTask == null ? string.Empty : TaskDateHelper.GetRowDateClass(parentTask),
                completionPercentage = taskViewModel?.CompletionPercentage ?? 0,
                projectedCompletionPercentage = taskViewModel?.ProjectedCompletionPercentage ?? 0,
                completedSubTaskCount = taskViewModel?.CompletedSubTaskCount ?? 0,
                inProgressSubTaskCount = taskViewModel?.InProgressSubTaskCount ?? 0,
                totalSubTaskCount = taskViewModel?.TotalSubTaskCount ?? 0
            });
        }

        //Помощен метод за еднакъв JSON отговор при грешка
        private JsonResult JsonError(string message)
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
