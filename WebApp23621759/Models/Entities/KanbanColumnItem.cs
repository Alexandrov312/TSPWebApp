using WebApp23621759.Enums;

namespace WebApp23621759.Models.Entities
{
    public class KanbanColumnItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsCompletedColumn { get; set; }
        public bool IsDefaultColumn { get; set; }
        public Status StatusValue { get; set; }
        public int TaskId { get; set; }
        public int UserId { get; set; }
    }
}
