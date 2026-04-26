using System.ComponentModel.DataAnnotations;

namespace WebApp23621759.Models.ViewModel.Auth
{
    //Модел за потвърждение на смяна на имейл чрез 6-цифрен код.
    public class VerifyEmailChangeViewModel
    {
        public int UserId { get; set; }
        public string NewEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Verification code is required")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "The code must contain exactly 6 digits")]
        public string Code { get; set; } = string.Empty;
    }
}
