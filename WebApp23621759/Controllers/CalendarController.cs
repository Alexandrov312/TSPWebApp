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
    public class CalendarController : Controller
    {
        private readonly TaskService _taskService;

        public CalendarController(TaskService taskService)
        {
            _taskService = taskService;
        }

        public IActionResult Index(int year = 0, int month = 0, string selectedDate = null)
        {
            int userId = UserHelper.GetUserId(User);

            DateTime now = DateTime.Now;
            int currentYear = year == 0 ? now.Year : year;
            int currentMonth = month == 0 ? now.Month : month;

            DateTime firstDayOfMonth = new DateTime(currentYear, currentMonth, 1);
            DateTime selected;
            if (string.IsNullOrEmpty(selectedDate))
            {
                selected = DateTime.Today;
            }
            else
            {
                selected = DateTime.Parse(selectedDate).Date;
            }

            var monthTasks = _taskService.GetTasksForMonth(userId, currentYear, currentMonth);

            var model = new CalendarViewModel
            {
                Year = currentYear,
                Month = currentMonth,
                MonthName = firstDayOfMonth.ToString("MMMM yyyy"),
                SelectedDate = selected
            };

            int offset = ((int)firstDayOfMonth.DayOfWeek + 6) % 7; //Понеделник да е първи
            DateTime gridStart = firstDayOfMonth.AddDays(-offset);

            for (int i = 0; i < 42; i++)
            {
                DateTime date = gridStart.AddDays(i);

                //Извилича всички задачи за конкретния ден
                var dayTasks = monthTasks
                    .Where(t => t.DueDate.Date == date.Date)
                    .ToList();

                //Добавя се ден в календара
                model.Days.Add(new CalendarDayModel
                {
                    Date = date,
                    IsCurrentMonth = date.Month == currentMonth,
                    IsToday = date.Date == DateTime.Today,
                    HasTasks = dayTasks.Any(),
                    Tasks = dayTasks
                });
            }

            //Задачите от селектирания ден се извеждат
            model.SelectedDayTasks = monthTasks
                .Where(t => t.DueDate.Date == selected.Date)
                .OrderBy(t => t.DueDate)
                .ToList();

            return View(model);
        }

        [HttpPost]
        public IActionResult ChangeStatus(int taskId, Status newStatus, int year, int month, DateTime selectedDate)
        {
            int userId = UserHelper.GetUserId(User);

            bool updated = _taskService.ChangeStatus(taskId, userId, newStatus);

            if (!updated)
            {
                NotificationHelper.AddNotification(TempData, "Task was not found or does not belong to you.", NotificationType.Error);
            }
            else
            {
                NotificationHelper.AddNotification(TempData, "Task status updated!", NotificationType.Success);
            }

            return RedirectToAction("Index", new
            {
                year = year,
                month = month,
                selectedDate = selectedDate.ToString("yyyy-MM-dd")
            });
        }
    }
}
