using WebApp23621759.Models.Entities;
using WebApp23621759.Models.ViewModel.Tasks;

namespace WebApp23621759.Models.ViewModel.Calendar
{
    //Модел за calendar страницата, който носи месеца, дните и задачите за избраната дата.
    public class CalendarViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;

        public DateTime SelectedDate { get; set; }

        public List<CalendarDayModel> Days { get; set; } = new();
        public List<MyTaskItemViewModel> SelectedDayTasks { get; set; } = new();
    }
}
