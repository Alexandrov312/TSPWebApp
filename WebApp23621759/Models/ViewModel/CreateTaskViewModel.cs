using System.ComponentModel.DataAnnotations;
using WebApp23621759.Enums;

namespace WebApp23621759.Models.ViewModel
{
    public class CreateTaskViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(100)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2500)]
        public string Description { get; set; }

        [Required(ErrorMessage = "Due date is required")]
        [DataType(DataType.DateTime)]
        public DateTime DueDate { get; set; }

        [Required(ErrorMessage = "Priority is required")]
        public Priority Priority { get; set; }
        public List<CreateSubTaskInputModel> SubTasks { get; set; } = new();
    }
}
