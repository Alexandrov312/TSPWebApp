namespace WebApp23621759.Models.Settings
{
    public class ReminderSettings
    {
        public int EmailVerificationCodeMinutes { get; set; } = 5;
        public int PasswordResetCodeMinutes { get; set; } = 10;
        public int ReminderMinutesBeforeDue { get; set; } = 60;
        public int NotificationWorkerIntervalMinutes { get; set; } = 2;
        public int ReminderLogRetentionDays { get; set; } = 90;
    }
}
