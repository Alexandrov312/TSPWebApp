using System.ComponentModel.DataAnnotations;

namespace WebApp23621759.Models.ViewModel.Auth
{
    //Модел за потвърждение на имейл чрез 6-цифрен код.
    public class VerifyEmailViewModel
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string EmailFieldName { get; set; } = "Email";
        public string PageTitle { get; set; } = "Verify Email";
        public string SubmitAction { get; set; } = "VerifyEmail";
        public string ResendAction { get; set; } = "ResendVerificationCode";
        public string SubmitButtonText { get; set; } = "Verify account";
        public string ResendButtonText { get; set; } = "Resend code";

        [Required(ErrorMessage = "Verification code is required")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "The code must contain exactly 6 digits")]
        public string Code { get; set; } = string.Empty;
    }
}
