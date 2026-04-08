using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApp23621759.Models.ViewModel;
using WebApp23621759.Services;

namespace WebApp23621759.Controllers
{
	public class LoginController : Controller
	{
		private readonly UserService _userService;
		private readonly AuthService _authService;
		public LoginController (UserService userService, AuthService authService)
		{
			_userService = userService;
			_authService = authService;
		}
		[HttpGet]
		public IActionResult Index()
		{
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> Index(LoginUserViewModel model)
		{
			if (!ModelState.IsValid)
			{
                return View(model);
			}

			var user = _userService.GetByUsernameOrEmail(model.UsernameOrEmail);

			if(user == null)
			{
				ViewBag.ErrorMessage = "Invalid username or email.";
				return View(model);
			}

			bool isPasswordValid = PasswordService.Verify(user.PasswordHash, model.Password);

			if (!isPasswordValid)
			{
				ViewBag.ErrorMessage = "Invalid password.";
				return View(model);
			}

			//Запазване на данните на потребителя в бисквитка
			await _authService.SignInAsync(HttpContext, user);

            return RedirectToAction("Index", "Home");
        }

		[HttpPost]
		public async Task<IActionResult> Logout()
		{
			//Премахва cookie-то на потребителя
			await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			return RedirectToAction("Index", "Home");
		}
	}
}
