using WebApp23621759.Enums;

namespace WebApp23621759.Helpers
{
    public class PriorityHelper
    {
        public static string GetCssClass(Priority priority)
        {
            return priority switch
            {
                Priority.Low => "priority-low",
                Priority.Medium => "priority-medium",
                Priority.High => "priority-high"
            };
        }
        public static string GetCalendarBorderClass(Priority priority)
        {
            return priority switch
            {
                Priority.Low => "calendar-priority-low",
                Priority.Medium => "calendar-priority-medium",
                Priority.High => "calendar-priority-high"
            };
        }
        public static string GetCalendarBadgeClass(Priority priority)
        {
            return priority switch
            {
                Priority.Low => "calendar-priority-badge-low",
                Priority.Medium => "calendar-priority-badge-medium",
                Priority.High => "calendar-priority-badge-high"
            };
        }
    }
}
