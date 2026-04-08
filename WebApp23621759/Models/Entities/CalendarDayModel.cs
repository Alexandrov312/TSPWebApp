namespace WebApp23621759.Models.Entities
{
    public class CalendarDayModel
    {
        public DateTime Date { get; set; }
        public bool IsCurrentMonth { get; set; }
        public bool IsToday { get; set; }
        public bool HasTasks { get; set; }
        public List<TaskItem> Tasks { get; set; }
    }
}
