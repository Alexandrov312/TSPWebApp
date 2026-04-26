using System.ComponentModel.DataAnnotations;

namespace WebApp23621759.Models.ViewModel.Auth
{
    //Модел за нулиране на парола чрез код по имейл.
    public class ResetPasswordViewModel
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Reset code is required")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "The code must contain exactly 6 digits")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "The password must be at least 8 characters long")]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).+$",
            ErrorMessage = "Password must contain at least one lowercase letter, one uppercase letter, one digit, and one special character")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password confirmation is required")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
