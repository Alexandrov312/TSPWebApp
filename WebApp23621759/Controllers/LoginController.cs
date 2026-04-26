using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using WebApp23621759.Enums;
using WebApp23621759.Helpers;
using WebApp23621759.Models.ViewModel.Auth;
using WebApp23621759.Services;

namespace WebApp23621759.Controllers
{
    public class LoginController : Controller
    {
        private readonly UserService _userService;
        private readonly AuthService _authService;
        private readonly OneTimeCodeService _oneTimeCodeService;
        private readonly EmailService _emailService;
        private readonly ReminderService _reminderService;

        public LoginController(
            UserService userService,
            AuthService authService,
            OneTimeCodeService oneTimeCodeService,
            EmailService emailService,
            ReminderService reminderService)
        {
            _userService = userService;
            _authService = authService;
            _oneTimeCodeService = oneTimeCodeService;
            _emailService = emailService;
            _reminderService = reminderService;
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
            if (user == null)
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

            if (!user.IsEmailConfirmed)
            {
                string verificationCode = _oneTimeCodeService.CreateCode(user.Id, user.Email, OneTimeCodeService.EmailVerificationPurpose);
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Verify your account",
                    $@"<p>Hello {user.Username},</p>
                       <p>Your verification code is: <strong>{verificationCode}</strong></p>
                       <p>The code is valid for 5 minutes.</p>");

                NotificationHelper.AddNotification(
                    TempData,
                    "Your email is not confirmed yet. We sent you a new verification code.",
                    NotificationType.Info);

                return RedirectToAction("VerifyEmail", "AccountSecurity", new { userId = user.Id });
            }

            await _authService.SignInAsync(HttpContext, user, model.RememberMe);
            await _reminderService.ProcessUserDueRemindersAsync(user);

            return RedirectToAction("Index", "MyTasks");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "MyTasks");
		}
	}
}
