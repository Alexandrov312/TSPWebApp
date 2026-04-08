using System.ComponentModel.DataAnnotations;

namespace WebApp23621759.Models.ViewModel
{
	public class LoginUserViewModel
	{
		[Required(ErrorMessage = "Username or email is required")]
		public string UsernameOrEmail { get; set; } = string.Empty;
		[Required(ErrorMessage = "Password is required")]
		public string Password {  get; set; } = string.Empty;
	}
}
