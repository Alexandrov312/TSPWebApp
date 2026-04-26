using Microsoft.Extensions.Options;
using WebApp23621759.Models.Settings;

namespace WebApp23621759.Services
{
    //1. Трие изтекли еднократни кодове.
    //2. Проверява дали има due reminders и ги обработва/изпраща.
    public class ReminderBackgroundService : BackgroundService
    {
        //dependencies на background service-а

        //Използва се, за да може вътре в background service-а да се взимат други service-и
        private readonly IServiceProvider _serviceProvider;
        private readonly ReminderSettings _reminderSettings;
        private readonly ILogger<ReminderBackgroundService> _logger;

        public ReminderBackgroundService(
            IServiceProvider serviceProvider,
            IOptions<ReminderSettings> reminderOptions,
            ILogger<ReminderBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _reminderSettings = reminderOptions.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using IServiceScope scope = _serviceProvider.CreateScope();
                    OneTimeCodeService oneTimeCodeService = scope.ServiceProvider.GetRequiredService<OneTimeCodeService>();
                    ReminderService reminderService = scope.ServiceProvider.GetRequiredService<ReminderService>();

                    oneTimeCodeService.DeleteExpiredCodes();
                    await reminderService.ProcessAllDueRemindersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Reminder background worker failed.");
                }

                int delayMinutes = Math.Max(1, _reminderSettings.NotificationWorkerIntervalMinutes);
                await Task.Delay(TimeSpan.FromMinutes(delayMinutes), stoppingToken);
            }
        }
    }
}
