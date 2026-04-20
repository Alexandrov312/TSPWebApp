using WebApp23621759.Enums;

namespace WebApp23621759.Models.ViewModel.SubTasks
{
    //Модел за визуализиране на една подзадача в My Tasks панела.
    public class MyTaskSubTaskViewModel
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int Depth { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Status Status { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? KanbanColumnId { get; set; }
        public int? BlockedBySubTaskId { get; set; }
        public string? BlockedByTitle { get; set; }
        public List<SubTaskDependencyOptionViewModel> DependencyOptions { get; set; } = new();
    }
}
