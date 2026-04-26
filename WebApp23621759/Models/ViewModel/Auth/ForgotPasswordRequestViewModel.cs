using System.ComponentModel.DataAnnotations;

namespace WebApp23621759.Models.ViewModel.Auth
{
    //Модел за заявка за код за смяна на парола.
    public class ForgotPasswordRequestViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
    }
}
