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
                Priority.High => "priority-high",
                _ => ""
            };
        }
        public static string GetCalendarBorderClass(Priority priority)
        {
            return priority switch
            {
                Priority.Low => "priority-border-low",
                Priority.Medium => "priority-border-medium",
                Priority.High => "priority-border-high",
                _ => ""
            };
        }
    }
}
