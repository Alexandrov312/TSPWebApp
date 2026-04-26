using WebApp23621759.Enums;

namespace WebApp23621759.Models.Entities
{
    public class SubTaskItem
    {
        public int Id { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }

        public Status Status { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? BlockedBySubTaskId { get; set; }

        public int TaskId { get; set; }
        public int UserId { get; set; }
    }
}
