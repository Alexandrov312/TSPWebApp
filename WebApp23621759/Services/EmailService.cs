using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using WebApp23621759.Models.Settings;

namespace WebApp23621759.Services
{
    public class EmailService
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<SmtpSettings> smtpOptions, ILogger<EmailService> logger)
        {
            _smtpSettings = smtpOptions.Value;
            _logger = logger;
        }

        //Събира на едно място email шаблоните за кодове, за да не се повтаря HTML логика в controller-ите.
        public Task<bool> SendAccountVerificationCodeAsync(string toEmail, string username, string code, bool isResend = false)
        {
            string subject = "Verify your account";
            string intro = isResend
                ? "Your new verification code is:"
                : "Your verification code is:";

            return SendCodeEmailAsync(toEmail, username, subject, intro, code, 5);
        }

        public Task<bool> SendEmailChangeCodeAsync(string toEmail, string username, string code, bool isResend = false)
        {
            string subject = "Confirm your new email";
            string intro = isResend
                ? "Your new email change verification code is:"
                : "Your email change verification code is:";

            return SendCodeEmailAsync(toEmail, username, subject, intro, code, 5);
        }

        public Task<bool> SendPasswordResetCodeAsync(string toEmail, string username, string code)
        {
            return SendCodeEmailAsync(toEmail, username, "Reset your password", "Your password reset code is:", code, 10);
        }

        //Изпраща имейл само ако SMTP настройките са налични и валидни.
        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(_smtpSettings.Host)
                || string.IsNullOrWhiteSpace(_smtpSettings.FromEmail))
            {
                _logger.LogWarning("SMTP settings are incomplete. Email to {Email} was skipped.", toEmail);
                return false;
            }

            try
            {
                //Клас от .NET, който представлява самото имейл съобщение
                using var message = new MailMessage
                {
                    From = new MailAddress(_smtpSettings.FromEmail, _smtpSettings.FromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                message.To.Add(toEmail);

                //Клас от .NET, който представлява клиент за изпращане на имейл чрез SMTP сървър
                using var client = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
                {
                    //Secure Sockets Layer - връзката между приложение и SMTP сървъра е криптирана
                    EnableSsl = _smtpSettings.EnableSsl
                };

                if (!string.IsNullOrWhiteSpace(_smtpSettings.Username))
                {
                    //Данни за удостоверяване - login данните, с които приложение се представя пред SMTP сървъра
                    client.Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password);
                }
                else
                {
                    client.UseDefaultCredentials = true;
                }

                await client.SendMailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email to {Email} could not be sent.", toEmail);
                return false;
            }
        }

        private Task<bool> SendCodeEmailAsync(string toEmail, string username, string subject, string intro, string code, int validMinutes)
        {
            string htmlBody = $@"<p>Hello {username},</p>
                   <p>{intro} <strong>{code}</strong></p>
                   <p>The code is valid for {validMinutes} minutes.</p>";

            return SendEmailAsync(toEmail, subject, htmlBody);
        }
    }
}
