using System.ComponentModel.DataAnnotations;
using WebApp23621759.Enums;

namespace WebApp23621759.Models.ViewModel
{
    public class CreateTaskModel
    {
        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Due date is required")]
        [DataType(DataType.DateTime)]
        public DateTime DueDate { get; set; }

        [Required(ErrorMessage = "Priority is required")]
        public Priority Priority { get; set; }
    }
}
