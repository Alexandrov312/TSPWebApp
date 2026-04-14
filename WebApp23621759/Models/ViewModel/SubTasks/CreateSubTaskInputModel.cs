using System.ComponentModel.DataAnnotations;

namespace WebApp23621759.Models.ViewModel.SubTasks
{
    //Модел за нова подзадача в Create Task екрана преди да бъде записана в базата.
    public class CreateSubTaskInputModel
    {
        [Required(ErrorMessage = "Subtask title is required")]
        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(2500)]
        public string Description { get; set; }
        public int? BlockedByIndex { get; set; }
    }
}
