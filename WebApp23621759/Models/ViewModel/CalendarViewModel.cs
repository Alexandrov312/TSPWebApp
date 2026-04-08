using WebApp23621759.Models.Entities;

namespace WebApp23621759.Models.ViewModel
{
    public class CalendarViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;

        public DateTime SelectedDate { get; set; }

        public List<CalendarDayModel> Days { get; set; } = new();
        public List<TaskItem> SelectedDayTasks { get; set; } = new();
    }
}
