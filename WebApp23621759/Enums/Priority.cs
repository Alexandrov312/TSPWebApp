using System.ComponentModel.DataAnnotations;

namespace WebApp23621759.Enums
{
    public enum Priority
    {
        [Display(Name = "Low Priority")]
        Low,

        [Display(Name = "Medium Priority")]
        Medium,

        [Display(Name = "High Priority")]
        High
    }
}
