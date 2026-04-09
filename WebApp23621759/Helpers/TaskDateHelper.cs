using WebApp23621759.Enums;
using WebApp23621759.Models.Entities;

namespace WebApp23621759.Helpers
{
    public class TaskDateHelper
    {
        public static string GetRowDateClass(TaskItem task)
        {
            DateTime today = DateTime.Today;
            DateTime dueDate = task.DueDate.Date;

            if (dueDate == today)
                return "task-row-due-today";

            if (dueDate < today && task.Status == Status.Completed)
                return "task-row-completed-past";

            if (dueDate < today)
                return "task-row-overdue";

            if (dueDate > today)
                return "task-row-upcoming";

            return "";
        }
    }
}