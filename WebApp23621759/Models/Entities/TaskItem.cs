using WebApp23621759.Enums;

namespace WebApp23621759.Models.Entities
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public Status Status{ get; set; }
        public Priority Priority{ get; set; }
        public int UserId { get; set; }
    }
}
