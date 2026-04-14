using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace WebApp23621759.Models.ViewModel.Auth
{
    //Модел за регистрация във Views/Register/Index.cshtml и RegisterController.
    public class RegisterUserViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [Remote(controller: "Register", action: "CheckUsername", ErrorMessage = "Username already exists")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Remote(controller: "Register", action: "CheckEmail", ErrorMessage = "Email already used")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "The password must be at least 8 characters long")]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).+$",
            ErrorMessage = "Password must contain at least one lowercase letter, one uppercase letter, one digit, and one special character")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password confirmation is required")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
