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
        private readonly SubTaskService _subTaskService;

        public MyTasksController(TaskService taskService, SubTaskService subTaskService)
        {
            _taskService = taskService;
            _subTaskService = subTaskService;
        }

        public IActionResult Index(string sortBy = "dueDate", string direction = "ASC")
        {
            int userId = UserHelper.GetUserId(User);
            var tasks = _taskService.GetAllTasksByUserId(userId, sortBy, direction);
            var taskViewModels = tasks.Select(task => BuildTaskItemViewModel(task, userId)).ToList();

            ViewBag.CurrentSortBy = sortBy;
            ViewBag.CurrentDirection = direction;
            return View("EnhancedIndex", taskViewModels);
        }

        [HttpGet]
        public IActionResult SubTasksPanel(int taskId)
        {
            int userId = UserHelper.GetUserId(User);
            var task = _taskService.GetById(taskId, userId);
            if (task == null)
            {
                return NotFound();
            }

            return PartialView("_TaskSubTasksPanel", BuildTaskItemViewModel(task, userId));
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            int userId = UserHelper.GetUserId(User);
            var task = _taskService.GetById(id, userId);
            if (task == null)
            {
                if (IsAjaxRequest())
                {
                    return Json(new
                    {
                        success = false,
                        message = "Task was not found or does not belong to you.",
                        notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Error)
                    });
                }

                NotificationHelper.AddNotification(TempData, "Task was not found or does not belong to you.", NotificationType.Error);
                return RedirectToAction("Index");
            }

            string title = task.Title;
            _taskService.DeleteTask(id);

            if (IsAjaxRequest())
            {
                return Json(new
                {
                    success = true,
                    taskId = id,
                    message = $"Task \"{title}\" deleted!",
                    notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Success)
                });
            }

            NotificationHelper.AddNotification(TempData, "Task \""+title+"\" deleted!", NotificationType.Success);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Update(EditTaskViewModel model)
        {
            if (!ModelState.IsValid)
            {
                if (IsAjaxRequest())
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid task data.",
                        notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Error)
                    });
                }

                NotificationHelper.AddNotification(TempData, "Invalid task data.", NotificationType.Error);
                return RedirectToAction("Index");
            }

            int userId = UserHelper.GetUserId(User);

            bool updated = _taskService.UpdateTask(model, userId);

            if (!updated)
            {
                if (IsAjaxRequest())
                {
                    return Json(new
                    {
                        success = false,
                        message = "Task was not found or does not belong to you.",
                        notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Error)
                    });
                }

                NotificationHelper.AddNotification(TempData, "Task was not found or does not belong to you.", NotificationType.Error);
                return RedirectToAction("Index");
            }

            if (IsAjaxRequest())
            {
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
            }

            NotificationHelper.AddNotification(TempData, $"Task \"{model.Title}\" updated!", NotificationType.Success);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult MarkAsDone(int id)
        {
            int userId = UserHelper.GetUserId(User);
            var task = _taskService.GetById(id, userId);

            if (task == null)
            {
                if (IsAjaxRequest())
                {
                    return Json(new
                    {
                        success = false,
                        message = "Task was not found or does not belong to you.",
                        notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Error)
                    });
                }

                NotificationHelper.AddNotification(TempData, "Task was not found or does not belong to you.", NotificationType.Error);
                return RedirectToAction("Index");
            }

            _subTaskService.SetAllCompletedForTask(id, userId);

            bool updated = _taskService.SetCompleted(id, userId);

            if (!updated)
            {
                if (IsAjaxRequest())
                {
                    return Json(new
                    {
                        success = false,
                        message = "Task was not found or does not belong to you.",
                        notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Error)
                    });
                }

                NotificationHelper.AddNotification(TempData, "Task was not found or does not belong to you.", NotificationType.Error);
                return RedirectToAction("Index");
            }

            string title = task.Title;

            if (IsAjaxRequest())
            {
                var refreshedTask = _taskService.GetById(id, userId);
                if (refreshedTask != null)
                {
                    var taskViewModel = BuildTaskItemViewModel(refreshedTask, userId);

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
                        completedSubTaskIds = taskViewModel.SubTasks
                            .Where(subTask => subTask.Status == Status.Completed)
                            .Select(subTask => subTask.Id)
                            .ToList()
                    });
                }
            }

            NotificationHelper.AddNotification(TempData, $"Task \""+title+"\" completed!", NotificationType.Success);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateSubTask(SubTaskViewModel model)
        {
            if (!ModelState.IsValid)
            {
                if (IsAjaxRequest())
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid subtask data.",
                        notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Error)
                    });
                }

                NotificationHelper.AddNotification(TempData, "Invalid subtask data.", NotificationType.Error);
                return RedirectToAction("Index");
            }

            int userId = UserHelper.GetUserId(User);
            bool updated = _subTaskService.UpdateTask(model, userId);

            if (!updated)
            {
                if (IsAjaxRequest())
                {
                    return Json(new
                    {
                        success = false,
                        message = "Subtask could not be updated. Check the dependency and try again.",
                        notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Error)
                    });
                }

                NotificationHelper.AddNotification(TempData, "Subtask could not be updated. Check the dependency and try again.", NotificationType.Error);
                return RedirectToAction("Index");
            }

            if (IsAjaxRequest())
            {
                var updatedSubTask = _subTaskService.GetById(model.Id);
                if (updatedSubTask != null)
                {
                    List<SubTaskItem> allSubTasks = _subTaskService.GetAllSubTasks(updatedSubTask.TaskId);
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
                        validDependencyIds = BuildDependencyOptions(updatedSubTask, allSubTasks)
                            .Select(option => option.Id)
                            .ToList()
                    });
                }
            }

            NotificationHelper.AddNotification(TempData, $"Subtask \"{model.Title}\" updated!", NotificationType.Success);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult CycleSubTaskStatus(int id)
        {
            int userId = UserHelper.GetUserId(User);
            var subTask = _subTaskService.GetById(id);

            if (subTask == null || subTask.UserId != userId)
            {
                NotificationHelper.AddNotification(TempData, "Subtask was not found or does not belong to you.", NotificationType.Error);
                return RedirectToAction("Index");
            }

            Status nextStatus = StatusHelper.GetNextStatus(subTask.Status);
            bool updated = _subTaskService.ChangeStatus(id, userId);

            if (!updated)
            {
                if (IsAjaxRequest())
                {
                    return Json(new
                    {
                        success = false,
                        message = "First complete the subtask this one depends on.",
                        notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Info)
                    });
                }

                NotificationHelper.AddNotification(TempData, "First complete the subtask this one depends on.", NotificationType.Info);
                return RedirectToAction("Index");
            }

            if (IsAjaxRequest())
            {
                var refreshedSubTask = _subTaskService.GetById(id);
                TaskItem? parentTask = refreshedSubTask == null
                    ? null
                    : _taskService.GetById(refreshedSubTask.TaskId, userId);

                if (refreshedSubTask != null && parentTask != null)
                {
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
                        completionPercentage = taskViewModel.CompletionPercentage,
                        projectedCompletionPercentage = taskViewModel.ProjectedCompletionPercentage,
                        completedSubTaskCount = taskViewModel.CompletedSubTaskCount,
                        inProgressSubTaskCount = taskViewModel.InProgressSubTaskCount,
                        totalSubTaskCount = taskViewModel.TotalSubTaskCount
                    });
                }
            }

            NotificationHelper.AddNotification(TempData, $"Subtask \"{subTask.Title}\" is now {StatusHelper.GetDisplayName(nextStatus)}.", NotificationType.Success);
            return RedirectToAction("Index");
        }

        private MyTaskItemViewModel BuildTaskItemViewModel(TaskItem task, int userId)
        {
            List<SubTaskItem> subTasks = _subTaskService.GetAllSubTasks(task.Id);
            int totalSubTasks = subTasks.Count;
            int completedSubTasks = subTasks.Count(subTask => subTask.Status == Status.Completed);
            int inProgressSubTasks = subTasks.Count(subTask => subTask.Status == Status.InProgress);
            int completionPercentage = totalSubTasks == 0
                ? 0
                : (int)Math.Round((double)completedSubTasks * 100 / totalSubTasks);
            int projectedCompletionPercentage = totalSubTasks == 0
                ? 0
                : (int)Math.Round((double)(completedSubTasks + inProgressSubTasks) * 100 / totalSubTasks);
            Dictionary<int, int> depthBySubTaskId = BuildSubTaskDepthMap(subTasks);
            List<SubTaskItem> orderedSubTasks = OrderSubTasks(subTasks);

            return new MyTaskItemViewModel
            {
                Task = task,
                CompletionPercentage = completionPercentage,
                ProjectedCompletionPercentage = projectedCompletionPercentage,
                CompletedSubTaskCount = completedSubTasks,
                InProgressSubTaskCount = inProgressSubTasks,
                TotalSubTaskCount = totalSubTasks,
                SubTasks = orderedSubTasks.Select(subTask => new MyTaskSubTaskViewModel
                {
                    Id = subTask.Id,
                    TaskId = subTask.TaskId,
                    Depth = depthBySubTaskId.GetValueOrDefault(subTask.Id),
                    Title = subTask.Title,
                    Description = subTask.Description,
                    Status = subTask.Status,
                    CompletedAt = subTask.CompletedAt,
                    BlockedBySubTaskId = subTask.BlockedBySubTaskId,
                    BlockedByTitle = subTasks.FirstOrDefault(candidate => candidate.Id == subTask.BlockedBySubTaskId)?.Title,
                    DependencyOptions = BuildDependencyOptions(subTask, subTasks)
                }).ToList()
            };
        }

        private static Dictionary<int, int> BuildSubTaskDepthMap(List<SubTaskItem> subTasks)
        {
            Dictionary<int, SubTaskItem> subTasksById = subTasks.ToDictionary(subTask => subTask.Id);
            Dictionary<int, int> depthBySubTaskId = new();

            int GetDepth(SubTaskItem subTask, HashSet<int> chain)
            {
                if (depthBySubTaskId.TryGetValue(subTask.Id, out int cachedDepth))
                {
                    return cachedDepth;
                }

                if (!subTask.BlockedBySubTaskId.HasValue
                    || !subTasksById.TryGetValue(subTask.BlockedBySubTaskId.Value, out SubTaskItem? parentSubTask)
                    || !chain.Add(subTask.Id))
                {
                    depthBySubTaskId[subTask.Id] = 0;
                    return 0;
                }

                int depth = GetDepth(parentSubTask, chain) + 1;
                chain.Remove(subTask.Id);
                depthBySubTaskId[subTask.Id] = depth;
                return depth;
            }

            foreach (SubTaskItem subTask in subTasks)
            {
                GetDepth(subTask, new HashSet<int>());
            }

            return depthBySubTaskId;
        }

        private static List<SubTaskItem> OrderSubTasks(List<SubTaskItem> subTasks)
        {
            HashSet<int> existingSubTaskIds = subTasks.Select(subTask => subTask.Id).ToHashSet();
            List<SubTaskItem> rootSubTasks = subTasks
                .Where(subTask => !subTask.BlockedBySubTaskId.HasValue || !existingSubTaskIds.Contains(subTask.BlockedBySubTaskId.Value))
                .OrderBy(subTask => subTask.Id)
                .ToList();
            Dictionary<int, List<SubTaskItem>> childrenByParentId = subTasks
                .Where(subTask => subTask.BlockedBySubTaskId.HasValue && existingSubTaskIds.Contains(subTask.BlockedBySubTaskId.Value))
                .GroupBy(subTask => subTask.BlockedBySubTaskId!.Value)
                .ToDictionary(group => group.Key, group => group.OrderBy(subTask => subTask.Id).ToList());

            List<SubTaskItem> orderedSubTasks = new();
            HashSet<int> visitedSubTaskIds = new();

            void AppendSubTask(SubTaskItem subTask)
            {
                if (!visitedSubTaskIds.Add(subTask.Id))
                {
                    return;
                }

                orderedSubTasks.Add(subTask);

                if (!childrenByParentId.TryGetValue(subTask.Id, out List<SubTaskItem>? childSubTasks))
                {
                    return;
                }

                foreach (SubTaskItem childSubTask in childSubTasks)
                {
                    AppendSubTask(childSubTask);
                }
            }

            foreach (SubTaskItem rootSubTask in rootSubTasks)
            {
                AppendSubTask(rootSubTask);
            }

            foreach (SubTaskItem subTask in subTasks.OrderBy(subTask => subTask.Id))
            {
                if (visitedSubTaskIds.Add(subTask.Id))
                {
                    orderedSubTasks.Add(subTask);
                }
            }

            return orderedSubTasks;
        }

        private static List<SubTaskDependencyOptionViewModel> BuildDependencyOptions(SubTaskItem currentSubTask, List<SubTaskItem> subTasks)
        {
            return subTasks
                .Where(subTask => subTask.Id != currentSubTask.Id && !CreatesCycle(currentSubTask.Id, subTask.Id, subTasks))
                .Select(subTask => new SubTaskDependencyOptionViewModel
                {
                    Id = subTask.Id,
                    Title = subTask.Title
                })
                .ToList();
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

            var createdSubTask = _subTaskService.CreateSubTask("New subtask", string.Empty, taskId, userId);
            if (createdSubTask == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Subtask could not be created.",
                    notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Error)
                });
            }

            var taskViewModel = BuildTaskItemViewModel(task, userId);

            return Json(new
            {
                success = true,
                taskId,
                subTaskId = createdSubTask.Id,
                message = "New subtask added.",
                notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Success),
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

            var parentTask = _taskService.GetById(subTask.TaskId, userId);
            var taskViewModel = parentTask == null ? null : BuildTaskItemViewModel(parentTask, userId);

            return Json(new
            {
                success = true,
                taskId = subTask.TaskId,
                message = $"Subtask \"{subTask.Title}\" deleted.",
                notificationCssClass = NotificationHelper.GetCssClass(NotificationType.Success),
                completionPercentage = taskViewModel?.CompletionPercentage ?? 0,
                projectedCompletionPercentage = taskViewModel?.ProjectedCompletionPercentage ?? 0,
                completedSubTaskCount = taskViewModel?.CompletedSubTaskCount ?? 0,
                inProgressSubTaskCount = taskViewModel?.InProgressSubTaskCount ?? 0,
                totalSubTaskCount = taskViewModel?.TotalSubTaskCount ?? 0
            });
        }

        private static bool CreatesCycle(int subTaskId, int dependencyId, List<SubTaskItem> subTasks)
        {
            Dictionary<int, int?> dependencyBySubTaskId = subTasks.ToDictionary(
                subTask => subTask.Id,
                subTask => subTask.Id == subTaskId ? dependencyId : subTask.BlockedBySubTaskId);

            int? currentDependencyId = dependencyId;
            HashSet<int> visitedIds = new();

            while (currentDependencyId.HasValue)
            {
                if (!visitedIds.Add(currentDependencyId.Value))
                {
                    return true;
                }

                if (currentDependencyId.Value == subTaskId)
                {
                    return true;
                }

                if (!dependencyBySubTaskId.TryGetValue(currentDependencyId.Value, out int? nextDependencyId))
                {
                    return false;
                }

                currentDependencyId = nextDependencyId;
            }

            return false;
        }

        private bool IsAjaxRequest()
        {
            return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        }
    }
}
