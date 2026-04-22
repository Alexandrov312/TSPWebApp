using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp23621759.Enums;
using WebApp23621759.Helpers;
using WebApp23621759.Models.Entities;
using WebApp23621759.Models.ViewModel.Calendar;
using WebApp23621759.Models.ViewModel.SubTasks;
using WebApp23621759.Models.ViewModel.Tasks;
using WebApp23621759.Services;

namespace WebApp23621759.Controllers
{
    [Authorize]
    public class CalendarController : Controller
    {
        private readonly TaskService _taskService;
        private readonly SubTaskService _subTaskService;

        public CalendarController(TaskService taskService, SubTaskService subTaskService)
        {
            _taskService = taskService;
            _subTaskService = subTaskService;
        }

        //Зарежда основната calendar страница
        public IActionResult Index(int year = 0, int month = 0, string selectedDate = null)
        {
            int userId = UserHelper.GetUserId(User);
            return View(BuildCalendarViewModel(year, month, selectedDate, userId));
        }

        //AJAX заявка; връща списъка със задачи за избрания ден
        [HttpGet]
        public IActionResult DayTasksList(int year = 0, int month = 0, string selectedDate = null)
        {
            int userId = UserHelper.GetUserId(User);
            return PartialView("_DayTasksList", BuildCalendarViewModel(year, month, selectedDate, userId));
        }

        //AJAX заявка; връща панела с подзадачите за конкретна задача
        [HttpGet]
        public IActionResult SubTasksPanel(int taskId)
        {
            int userId = UserHelper.GetUserId(User);
            var task = _taskService.GetById(taskId, userId);
            if (task == null)
            {
                return NotFound();
            }

            //Преизползва partial view-то от MyTasks, за да няма дублиране на UI
            return PartialView("~/Views/MyTasks/_TaskSubTasksPanel.cshtml", BuildTaskItemViewModel(task, userId));
        }

        [HttpPost]
        public IActionResult ChangeStatus(int taskId, Status newStatus, int year, int month, DateTime selectedDate, string title)
        {
            int userId = UserHelper.GetUserId(User);

            bool updated = _taskService.ChangeStatus(taskId, userId, newStatus);

            if (!updated)
            {
                //Ако заявката е дошла през JavaScript/fetch, връща JSON вместо redirect
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = false,
                        message = "Task was not found or does not belong to you.",
                        notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Error)
                    });
                }

                //При нормална заявка записва съобщение в TempData и после ще redirect-не
                NotificationHelper.AddNotification(TempData, "Task was not found or does not belong to you.", NotificationType.Error);
            }
            else
            {
                var updatedTask = _taskService.GetById(taskId, userId);

                //AJAX режимът връща готови данни за обновяване на UI-то без презареждане
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var taskViewModel = updatedTask == null
                        ? null
                        : BuildTaskItemViewModel(updatedTask, userId);

                    return Json(new
                    {
                        success = true,
                        taskId,
                        message = $"Task \"{title}\" status updated!",
                        notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Success),

                        //Ако задачата не може да се презареди, връща fallback стойности
                        taskStatusValue = updatedTask == null ? (int)newStatus : (int)updatedTask.Status,
                        taskStatusDisplayName = updatedTask == null ? StatusHelper.GetDisplayName(newStatus) : StatusHelper.GetDisplayName(updatedTask.Status),
                        taskStatusCssClass = updatedTask == null ? string.Empty : StatusHelper.GetCssClass(updatedTask.Status),
                        taskCalendarStatusCssClass = updatedTask == null ? string.Empty : StatusHelper.GetCalendarCardClass(updatedTask.Status),
                        taskCompletedAtText = updatedTask?.CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "Not finished",
                        taskRowDateCssClass = updatedTask == null ? string.Empty : TaskDateHelper.GetRowDateClass(updatedTask),

                        //Данни за прогреса на задачата според подзадачите
                        completionPercentage = taskViewModel?.CompletionPercentage ?? 0,
                        projectedCompletionPercentage = taskViewModel?.ProjectedCompletionPercentage ?? 0,
                        completedSubTaskCount = taskViewModel?.CompletedSubTaskCount ?? 0,
                        inProgressSubTaskCount = taskViewModel?.InProgressSubTaskCount ?? 0,
                        totalSubTaskCount = taskViewModel?.TotalSubTaskCount ?? 0
                    });
                }

                NotificationHelper.AddNotification(TempData, $"Task \"{title}\" status updated!", NotificationType.Success);
            }

            //При не-AJAX заявка връща потребителя към същия месец и избран ден
            return RedirectToAction("Index", new
            {
                year,
                month,
                selectedDate = selectedDate.ToString("yyyy-MM-dd")
            });
        }

        [HttpPost]
        public IActionResult UpdateTask(EditTaskViewModel model, int year, int month, DateTime selectedDate)
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
                return JsonError("Task could not be updated.");
            }

            var updatedTask = _taskService.GetById(model.Id, userId);
            if (updatedTask == null)
            {
                return JsonError("Task could not be reloaded after update.");
            }

            return Json(new
            {
                success = true,
                taskId = updatedTask.Id,
                message = $"Task \"{updatedTask.Title}\" updated!",
                notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Success),
                year,
                month,
                selectedDate = selectedDate.ToString("yyyy-MM-dd")
            });
        }

        [HttpPost]
        public IActionResult MarkAsDone(int id, int year, int month, DateTime selectedDate)
        {
            int userId = UserHelper.GetUserId(User);
            var task = _taskService.GetById(id, userId);
            if (task == null)
            {
                return JsonError("Task was not found or does not belong to you.");
            }

            //При завършване на главната задача приключват и всички нейни подзадачи
            _subTaskService.SetAllCompletedForTask(id, userId);

            bool updated = _taskService.SetCompleted(id, userId);
            if (!updated)
            {
                return JsonError("Task could not be completed.");
            }

            return Json(new
            {
                success = true,
                taskId = id,
                message = $"Task \"{task.Title}\" completed!",
                notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Success),
                year,
                month,
                selectedDate = selectedDate.ToString("yyyy-MM-dd")
            });
        }

        [HttpPost]
        public IActionResult Delete(int id, int year, int month, DateTime selectedDate)
        {
            int userId = UserHelper.GetUserId(User);
            var task = _taskService.GetById(id, userId);
            if (task == null)
            {
                return JsonError("Task was not found or does not belong to you.");
            }

            _taskService.DeleteTask(id);

            return Json(new
            {
                success = true,
                taskId = id,
                message = $"Task \"{task.Title}\" deleted!",
                notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Success),
                year,
                month,
                selectedDate = selectedDate.ToString("yyyy-MM-dd")
            });
        }

        [HttpPost]
        public IActionResult Archive(int id, int year, int month, DateTime selectedDate)
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
                notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Success),
                year,
                month,
                selectedDate = selectedDate.ToString("yyyy-MM-dd")
            });
        }

        [HttpPost]
        public IActionResult MarkAsPending(int id, int year, int month, DateTime selectedDate)
        {
            int userId = UserHelper.GetUserId(User);
            var task = _taskService.GetById(id, userId);
            if (task == null)
            {
                return JsonError("Task was not found or does not belong to you.");
            }

            _subTaskService.SetAllPendingForTask(id, userId);

            bool updated = _taskService.SetPending(id, userId);
            if (!updated)
            {
                return JsonError("Task could not be marked as pending.");
            }

            return Json(new
            {
                success = true,
                taskId = id,
                message = $"Task \"{task.Title}\" marked as pending.",
                notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Success),
                year,
                month,
                selectedDate = selectedDate.ToString("yyyy-MM-dd")
            });
        }

        [HttpPost]
        public IActionResult CreateTask(int year, int month, DateTime selectedDate)
        {
            int userId = UserHelper.GetUserId(User);
            DateTime now = DateTime.Now;

            //Създава задачата за избрания ден, но запазва текущия час и минути
            DateTime dueDate = selectedDate.Date
                .AddHours(now.Hour)
                .AddMinutes(now.Minute);

            var createdTask = _taskService.CreateTask(
                "New task",
                "Task description",
                dueDate,
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
                notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Success),
                year,
                month,
                selectedDate = selectedDate.ToString("yyyy-MM-dd")
            });
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
            if (updatedSubTask == null)
            {
                return JsonError("Subtask could not be reloaded after update.");
            }

            //След промяна на подзадача синхронизира статуса и прогреса на главната задача
            var syncedTask = _taskService.SyncStatusWithSubTasks(updatedSubTask.TaskId, userId);
            if (syncedTask == null)
            {
                return JsonError("Task could not be reloaded after subtask update.");
            }

            var taskViewModel = BuildTaskItemViewModel(syncedTask, userId);
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

            if (refreshedSubTask == null || parentTask == null)
            {
                return JsonError("Subtask could not be reloaded after status change.");
            }

            var taskViewModel = BuildTaskItemViewModel(parentTask, userId);

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

            var taskViewModel = BuildTaskItemViewModel(syncedTask, userId);

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
            var taskViewModel = parentTask == null ? null : BuildTaskItemViewModel(parentTask, userId);

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

        //Строи view model за една задача заедно с всичките ѝ подзадачи
        private MyTaskItemViewModel BuildTaskItemViewModel(TaskItem task, int userId)
        {
            List<SubTaskItem> subTasks = _subTaskService.GetAllSubTasks(task.Id);
            return TaskViewModelHelper.BuildTaskItemViewModel(task, subTasks);
        }

        //Строи целия модел за calendar страницата
        private CalendarViewModel BuildCalendarViewModel(int year, int month, string selectedDate, int userId)
        {
            DateTime now = DateTime.Now;
            int currentYear = year == 0 ? now.Year : year;
            int currentMonth = month == 0 ? now.Month : month;

            DateTime firstDayOfMonth = new DateTime(currentYear, currentMonth, 1);

            //Ако няма избрана дата, използва днешната
            DateTime selected = string.IsNullOrEmpty(selectedDate)
                ? DateTime.Today
                : DateTime.Parse(selectedDate).Date;

            //Взима всички задачи за текущия месец за конкретния потребител
            var monthTasks = _taskService.GetTasksForMonth(userId, currentYear, currentMonth);

            var model = new CalendarViewModel
            {
                Year = currentYear,
                Month = currentMonth,
                MonthName = firstDayOfMonth.ToString("MMMM yyyy"),
                SelectedDate = selected
            };

            //Преобразува DayOfWeek така, че календарът да започва от понеделник
            int offset = ((int)firstDayOfMonth.DayOfWeek + 6) % 7;
            DateTime gridStart = firstDayOfMonth.AddDays(-offset);

            //6 реда по 7 дни = 42 клетки в календарната решетка
            for (int i = 0; i < 42; i++)
            {
                DateTime date = gridStart.AddDays(i);

                var dayTasks = monthTasks
                    .Where(task => task.DueDate.Date == date.Date)
                    .ToList();

                model.Days.Add(new CalendarDayModel
                {
                    Date = date,
                    IsCurrentMonth = date.Month == currentMonth,
                    IsToday = date.Date == DateTime.Today,
                    HasTasks = dayTasks.Any(),
                    Tasks = dayTasks
                });
            }

            //Списъкът със задачи за избрания ден в десния панел
            model.SelectedDayTasks = monthTasks
                .Where(task => task.DueDate.Date == selected.Date)
                .OrderBy(task => task.DueDate)
                .Select(task => BuildTaskItemViewModel(task, userId))
                .ToList();

            return model;
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
