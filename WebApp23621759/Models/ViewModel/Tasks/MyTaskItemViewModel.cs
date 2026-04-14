using WebApp23621759.Models.Entities;
using WebApp23621759.Models.ViewModel.SubTasks;

namespace WebApp23621759.Models.ViewModel.Tasks
{
    //Модел за една главна задача в My Tasks заедно с progress информацията и подзадачите й.
    public class MyTaskItemViewModel
    {
        public TaskItem Task { get; set; } = null!;
        public List<MyTaskSubTaskViewModel> SubTasks { get; set; } = new();
        public int CompletionPercentage { get; set; }
        public int ProjectedCompletionPercentage { get; set; }
        public int CompletedSubTaskCount { get; set; }
        public int InProgressSubTaskCount { get; set; }
        public int TotalSubTaskCount { get; set; }
    }
}
