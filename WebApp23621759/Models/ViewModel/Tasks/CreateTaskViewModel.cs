using System.ComponentModel.DataAnnotations;
using WebApp23621759.Enums;
using WebApp23621759.Models.ViewModel.SubTasks;

namespace WebApp23621759.Models.ViewModel.Tasks
{
    //Модел за създаване на главна задача във Views/CreateTask/Index.cshtml и CreateTaskController.
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
