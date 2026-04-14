using System.ComponentModel.DataAnnotations;

namespace WebApp23621759.Models.ViewModel.SubTasks
{
    //Модел за редакция на съществуваща подзадача от My Tasks.
    public class SubTaskViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(100)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2500)]
        public string Description { get; set; }
        public int? BlockedBySubTaskId { get; set; }
    }
}
