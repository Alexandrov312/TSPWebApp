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
    }
}
