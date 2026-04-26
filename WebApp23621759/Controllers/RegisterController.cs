using Microsoft.AspNetCore.Mvc;
using WebApp23621759.Enums;
using WebApp23621759.Helpers;
using WebApp23621759.Models.Entities;
using WebApp23621759.Models.ViewModel.Auth;
using WebApp23621759.Services;

namespace WebApp23621759.Controllers
{
    public class RegisterController : Controller
    {
        private readonly UserService _userService;
        private readonly OneTimeCodeService _oneTimeCodeService;
        private readonly EmailService _emailService;

        public RegisterController(
            UserService userService,
            OneTimeCodeService oneTimeCodeService,
            EmailService emailService)
        {
            _userService = userService;
            _oneTimeCodeService = oneTimeCodeService;
            _emailService = emailService;
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
            User user = _userService.CreateUser(model.Username, model.Email, hash);

            if (user == null)
            {
                ViewBag.ErrorMessage = "Username or email already exists.";
                return View(model);
            }

            string verificationCode = _oneTimeCodeService.CreateCode(user.Id, user.Email, OneTimeCodeService.EmailVerificationPurpose);
            bool emailSent = await _emailService.SendAccountVerificationCodeAsync(
                user.Email,
                user.Username,
                verificationCode);

            if (!emailSent)
            {
                _userService.DeleteUser(user.Id);
                ViewBag.ErrorMessage = "The verification email could not be sent, so the account was not created.";
                return View(model);
            }

            NotificationHelper.AddNotification(
                TempData,
                "Your account was created. Check your email and enter the verification code.",
                NotificationType.Success);

            return RedirectToAction("VerifyEmail", "AccountSecurity", new { userId = user.Id });
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
