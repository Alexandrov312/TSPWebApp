using WebApp23621759.Enums;

namespace WebApp23621759.Helpers
{
    public class StatusHelper
    {
        public static string GetCssClass(Status status)
        {
            return status switch
            {
                Status.Pending => "status-pending",
                Status.InProgress => "status-in-progress",
                Status.Completed => "status-completed",
                Status.Overdue => "status-overdue"
            };
        }
    }
}
