using System.ComponentModel.DataAnnotations;

namespace WebApp23621759.Enums
{
    public enum Status
    {
        Pending,
        [Display(Name = "In Progress")]
        InProgress,
        Completed,
        Overdue
    }
}
