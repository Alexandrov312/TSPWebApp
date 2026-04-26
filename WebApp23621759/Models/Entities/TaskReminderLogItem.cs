namespace WebApp23621759.Models.Entities
{
    public class TaskReminderLogItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TaskId { get; set; }
        public string ReminderType { get; set; } = string.Empty;
        public DateTime ReferenceDueDate { get; set; }
        public DateTime SentAt { get; set; }
    }
}
