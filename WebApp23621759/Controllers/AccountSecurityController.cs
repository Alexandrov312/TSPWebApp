using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp23621759.Enums;
using WebApp23621759.Helpers;
using WebApp23621759.Models.Entities;
using WebApp23621759.Models.ViewModel.Auth;
using WebApp23621759.Services;

namespace WebApp23621759.Controllers
{
    [Authorize]
    public class AccountSecurityController : Controller
    {
        private readonly UserService _userService;
        private readonly OneTimeCodeService _oneTimeCodeService;
        private readonly EmailService _emailService;
        private readonly AuthService _authService;

        public AccountSecurityController(
            UserService userService,
            OneTimeCodeService oneTimeCodeService,
            EmailService emailService,
            AuthService authService)
        {
            _userService = userService;
            _oneTimeCodeService = oneTimeCodeService;
            _emailService = emailService;
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Profile()
        {
            User user = _userService.GetById(UserHelper.GetUserId(User));
            if (user == null)
            {
                return RedirectToAction("Index", "Login");
            }

            return View(BuildAccountSettingsModel(user));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUsername(AccountSettingsViewModel model)
        {
            User user = _userService.GetById(UserHelper.GetUserId(User));
            if (user == null)
            {
                return RedirectToAction("Index", "Login");
            }

            if (string.IsNullOrWhiteSpace(model.NewUsername))
            {
                NotificationHelper.AddNotification(TempData, "Username is required.", NotificationType.Error);
                return RedirectToAction(nameof(Profile));
            }

            string normalizedUsername = model.NewUsername.Trim();
            if (string.Equals(normalizedUsername, user.Username, StringComparison.Ordinal))
            {
                NotificationHelper.AddNotification(TempData, "Username was not changed.", NotificationType.Info);
                return RedirectToAction(nameof(Profile));
            }

            bool isUpdated = _userService.UpdateUsername(user.Id, normalizedUsername);
            if (!isUpdated)
            {
                NotificationHelper.AddNotification(TempData, "This username is already taken.", NotificationType.Error);
                return RedirectToAction(nameof(Profile));
            }

            await RefreshSignedInUserAsync(user.Id);
            NotificationHelper.AddNotification(TempData, "Username updated successfully.", NotificationType.Success);
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateEmail(AccountSettingsViewModel model)
        {
            User user = _userService.GetById(UserHelper.GetUserId(User));
            if (user == null)
            {
                return RedirectToAction("Index", "Login");
            }

            if (string.IsNullOrWhiteSpace(model.NewEmail))
            {
                NotificationHelper.AddNotification(TempData, "Email is required.", NotificationType.Error);
                return RedirectToAction(nameof(Profile));
            }

            string normalizedEmail = model.NewEmail.Trim();
            if (string.Equals(normalizedEmail, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                NotificationHelper.AddNotification(TempData, "Email was not changed.", NotificationType.Info);
                return RedirectToAction(nameof(Profile));
            }

            if (_userService.EmailExists(normalizedEmail))
            {
                NotificationHelper.AddNotification(TempData, "This email is already in use.", NotificationType.Error);
                return RedirectToAction(nameof(Profile));
            }

            //Не сменяме email-а веднага, а първо пращаме код към новия адрес.
            string code = _oneTimeCodeService.CreateCode(user.Id, normalizedEmail, OneTimeCodeService.EmailChangePurpose);
            bool emailSent = await _emailService.SendEmailChangeCodeAsync(
                normalizedEmail,
                user.Username,
                code);

            if (!emailSent)
            {
                NotificationHelper.AddNotification(TempData, "The verification code could not be sent to the new email.", NotificationType.Error);
                return RedirectToAction(nameof(Profile));
            }

            NotificationHelper.AddNotification(TempData, "We sent a verification code to your new email address.", NotificationType.Info);
            return RedirectToAction(nameof(VerifyEmailChange), new { userId = user.Id });
        }

        [HttpGet]
        public IActionResult VerifyEmailChange(int userId)
        {
            User user = _userService.GetById(UserHelper.GetUserId(User));
            if (user == null || user.Id != userId)
            {
                return RedirectToAction(nameof(Profile));
            }

            var activeCode = _oneTimeCodeService.GetLatestActiveCode(userId, OneTimeCodeService.EmailChangePurpose);
            if (activeCode == null)
            {
                NotificationHelper.AddNotification(TempData, "There is no pending email change request.", NotificationType.Info);
                return RedirectToAction(nameof(Profile));
            }

            return View("VerifyEmail", BuildEmailChangeVerificationModel(userId, activeCode.Email));
        }

        [HttpPost]
        public async Task<IActionResult> VerifyEmailChange(VerifyEmailChangeViewModel model)
        {
            User user = _userService.GetById(UserHelper.GetUserId(User));
            if (user == null || user.Id != model.UserId)
            {
                return RedirectToAction(nameof(Profile));
            }

            if (!ModelState.IsValid)
            {
                var activeCode = _oneTimeCodeService.GetLatestActiveCode(model.UserId, OneTimeCodeService.EmailChangePurpose);
                model.NewEmail = activeCode?.Email ?? model.NewEmail;
                return View("VerifyEmail", BuildEmailChangeVerificationModel(model.UserId, model.NewEmail, model.Code));
            }

            string? verifiedEmail = _oneTimeCodeService.ValidateCodeAndGetEmail(model.UserId, OneTimeCodeService.EmailChangePurpose, model.Code);
            if (string.IsNullOrWhiteSpace(verifiedEmail))
            {
                var activeCode = _oneTimeCodeService.GetLatestActiveCode(model.UserId, OneTimeCodeService.EmailChangePurpose);
                model.NewEmail = activeCode?.Email ?? model.NewEmail;
                ViewBag.ErrorMessage = "The verification code is invalid or has expired.";
                return View("VerifyEmail", BuildEmailChangeVerificationModel(model.UserId, model.NewEmail, model.Code));
            }

            //Email-ът се обновява чак след успешна верификация на кода.
            bool isUpdated = _userService.UpdateEmail(model.UserId, verifiedEmail);
            if (!isUpdated)
            {
                NotificationHelper.AddNotification(TempData, "This email is already in use.", NotificationType.Error);
                return RedirectToAction(nameof(Profile));
            }

            await RefreshSignedInUserAsync(model.UserId);
            NotificationHelper.AddNotification(TempData, "Email updated successfully.", NotificationType.Success);
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        public async Task<IActionResult> ResendEmailChangeCode(int userId)
        {
            User user = _userService.GetById(UserHelper.GetUserId(User));
            if (user == null || user.Id != userId)
            {
                return RedirectToAction(nameof(Profile));
            }

            var activeCode = _oneTimeCodeService.GetLatestActiveCode(userId, OneTimeCodeService.EmailChangePurpose);
            if (activeCode == null)
            {
                NotificationHelper.AddNotification(TempData, "There is no pending email change request.", NotificationType.Info);
                return RedirectToAction(nameof(Profile));
            }

            string code = _oneTimeCodeService.CreateCode(user.Id, activeCode.Email, OneTimeCodeService.EmailChangePurpose);
            await _emailService.SendEmailChangeCodeAsync(
                activeCode.Email,
                user.Username,
                code,
                true);

            NotificationHelper.AddNotification(TempData, "A new verification code was sent to the new email.", NotificationType.Info);
            return RedirectToAction(nameof(VerifyEmailChange), new { userId });
        }

        [HttpPost]
        public IActionResult UpdatePassword(AccountSettingsViewModel model)
        {
            User user = _userService.GetById(UserHelper.GetUserId(User));
            if (user == null)
            {
                return RedirectToAction("Index", "Login");
            }

            if (string.IsNullOrWhiteSpace(model.CurrentPassword) ||
                string.IsNullOrWhiteSpace(model.NewPassword) ||
                string.IsNullOrWhiteSpace(model.ConfirmNewPassword))
            {
                NotificationHelper.AddNotification(TempData, "Fill in all password fields.", NotificationType.Error);
                return RedirectToAction(nameof(Profile));
            }

            bool isCurrentPasswordValid = PasswordService.Verify(user.PasswordHash, model.CurrentPassword);
            if (!isCurrentPasswordValid)
            {
                NotificationHelper.AddNotification(TempData, "Current password is incorrect.", NotificationType.Error);
                return RedirectToAction(nameof(Profile));
            }

            if (!string.Equals(model.NewPassword, model.ConfirmNewPassword, StringComparison.Ordinal))
            {
                NotificationHelper.AddNotification(TempData, "New passwords do not match.", NotificationType.Error);
                return RedirectToAction(nameof(Profile));
            }

            _userService.UpdatePassword(user.Id, PasswordService.Hash(model.NewPassword));
            NotificationHelper.AddNotification(TempData, "Password updated successfully.", NotificationType.Success);
            return RedirectToAction(nameof(Profile));
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult VerifyEmail(int userId)
        {
            var user = _userService.GetById(userId);
            if (user == null)
            {
                return RedirectToAction("Index", "Register");
            }

            return View(BuildEmailVerificationModel(user));
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            var user = _userService.GetById(model.UserId);
            if (user == null)
            {
                return RedirectToAction("Index", "Register");
            }

            if (!ModelState.IsValid)
            {
                return View(BuildEmailVerificationModel(user, model.Code));
            }

            bool isCodeValid = _oneTimeCodeService.ValidateCode(model.UserId, OneTimeCodeService.EmailVerificationPurpose, model.Code);
            if (!isCodeValid)
            {
                ViewBag.ErrorMessage = "The verification code is invalid or has expired.";
                return View(BuildEmailVerificationModel(user, model.Code));
            }

            _userService.ConfirmEmail(model.UserId);
            user = _userService.GetById(model.UserId);
            await _authService.SignInAsync(HttpContext, user!);

            NotificationHelper.AddNotification(
                TempData,
                "Your email was verified successfully.",
                NotificationType.Success);

            return RedirectToAction("Index", "MyTasks");
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ResendVerificationCode(int userId)
        {
            var user = _userService.GetById(userId);
            if (user == null)
            {
                return RedirectToAction("Index", "Register");
            }

            string code = _oneTimeCodeService.CreateCode(user.Id, user.Email, OneTimeCodeService.EmailVerificationPurpose);
            await _emailService.SendAccountVerificationCodeAsync(
                user.Email,
                user.Username,
                code,
                true);

            NotificationHelper.AddNotification(
                TempData,
                "A new verification code was sent.",
                NotificationType.Info);

            return RedirectToAction(nameof(VerifyEmail), new { userId });
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordRequestViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = _userService.GetByEmail(model.Email);
            if (user != null)
            {
                string code = _oneTimeCodeService.CreateCode(user.Id, user.Email, OneTimeCodeService.PasswordResetPurpose);
                await _emailService.SendPasswordResetCodeAsync(
                    user.Email,
                    user.Username,
                    code);
            }

            NotificationHelper.AddNotification(
                TempData,
                "If an account with this email exists, a reset code was sent.",
                NotificationType.Info);

            return user == null
                ? RedirectToAction("Index", "Login")
                : RedirectToAction(nameof(ResetPassword), new { userId = user.Id });
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetPassword(int userId)
        {
            var user = _userService.GetById(userId);
            if (user == null)
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            return View(new ResetPasswordViewModel
            {
                UserId = user.Id,
                Email = user.Email
            });
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            var user = _userService.GetById(model.UserId);
            if (user == null)
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            if (!ModelState.IsValid)
            {
                model.Email = user.Email;
                return View(model);
            }

            bool isCodeValid = _oneTimeCodeService.ValidateCode(model.UserId, OneTimeCodeService.PasswordResetPurpose, model.Code);
            if (!isCodeValid)
            {
                model.Email = user.Email;
                ViewBag.ErrorMessage = "The reset code is invalid or has expired.";
                return View(model);
            }

            _userService.UpdatePassword(model.UserId, PasswordService.Hash(model.NewPassword));

            NotificationHelper.AddNotification(
                TempData,
                "Your password was updated successfully.",
                NotificationType.Success);

            return RedirectToAction("Index", "Login");
        }

        //Един и същ verification view се използва и за регистрация, и за смяна на email.
        private VerifyEmailViewModel BuildEmailVerificationModel(User user, string code = "")
        {
            return new VerifyEmailViewModel
            {
                UserId = user.Id,
                Email = user.Email,
                Code = code
            };
        }

        private VerifyEmailViewModel BuildEmailChangeVerificationModel(int userId, string email, string code = "")
        {
            return new VerifyEmailViewModel
            {
                UserId = userId,
                Email = email,
                EmailFieldName = "NewEmail",
                PageTitle = "Verify New Email",
                SubmitAction = nameof(VerifyEmailChange),
                ResendAction = nameof(ResendEmailChangeCode),
                SubmitButtonText = "Verify new email",
                ResendButtonText = "Resend code",
                Code = code
            };
        }

        private AccountSettingsViewModel BuildAccountSettingsModel(User user)
        {
            return new AccountSettingsViewModel
            {
                CurrentUsername = user.Username,
                CurrentEmail = user.Email,
                NewUsername = user.Username,
                NewEmail = user.Email
            };
        }

        private async Task RefreshSignedInUserAsync(int userId)
        {
            User updatedUser = _userService.GetById(userId);
            if (updatedUser != null)
            {
                await _authService.SignInAsync(HttpContext, updatedUser);
            }
        }
    }
}
