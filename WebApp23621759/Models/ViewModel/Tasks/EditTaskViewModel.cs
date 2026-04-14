using System.ComponentModel.DataAnnotations;
using WebApp23621759.Enums;

namespace WebApp23621759.Models.ViewModel.Tasks
{
    //Модел за редакция на съществуваща главна задача от My Tasks.
    public class EditTaskViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(100)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500)]
        public string Description { get; set; }

        [Required(ErrorMessage = "Due date is required")]
        [DataType(DataType.DateTime)]
        public DateTime DueDate { get; set; }

        [Required]
        public TaskStatus Status { get; set; }

        [Required(ErrorMessage = "Priority is required")]
        public Priority Priority { get; set; }
    }
}
