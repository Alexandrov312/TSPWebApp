using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using WebApp23621759.Services;
using System.Security.Claims;
using WebApp23621759.Models.Entities;
using WebApp23621759.Models.ViewModel.Auth;

namespace WebApp23621759.Controllers
{
    public class RegisterController : Controller
    {
        private readonly UserService _userService;
        private readonly AuthService _authService;
        public RegisterController(UserService userService, AuthService authService)
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
        public async Task<IActionResult> Index(RegisterUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var hash = PasswordService.Hash(model.Password);
            User user = _userService.CreateUser(model.Username, model.Email, hash, false);

            if (user == null)
            {
                ViewBag.ErrorMessage = "User could not be created";
                return View(model);
            }

            //Запазване на данните на потребителя в бисквитка
            await _authService.SignInAsync(HttpContext, user);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult CheckUsername(string username)
        {
            return Json(!_userService.UsernameExists(username));
        }

        [HttpGet]
        public IActionResult CheckEmail(string email)
        {
            return Json(!_userService.EmailExists(email));
        }
    }
}
