using System.Net;
using Microsoft.Extensions.Options;
using Npgsql;
using WebApp23621759.Database;
using WebApp23621759.Enums;
using WebApp23621759.Helpers;
using WebApp23621759.Models.Entities;
using WebApp23621759.Models.Settings;

namespace WebApp23621759.Services
{
    public class ReminderService
    {
        private const string UpcomingInAppReminderType = "UpcomingInApp";
        private const string UpcomingEmailReminderType = "UpcomingEmail";
        private const string OverdueInAppReminderType = "OverdueInApp";
        private const string OverdueEmailReminderType = "OverdueEmail";

        private readonly DatabaseService _databaseService;
        private readonly UserService _userService;
        private readonly EmailService _emailService;
        private readonly AppNotificationService _appNotificationService;
        private readonly ReminderSettings _reminderSettings;
        private static readonly TimeZoneInfo SofiaTimeZone = ResolveSofiaTimeZone();

        public ReminderService(
            DatabaseService databaseService,
            UserService userService,
            EmailService emailService,
            AppNotificationService appNotificationService,
            IOptions<ReminderSettings> reminderOptions)
        {
            _databaseService = databaseService;
            _userService = userService;
            _emailService = emailService;
            _appNotificationService = appNotificationService;
            _reminderSettings = reminderOptions.Value;
        }

        //Обхожда всички потвърдени потребители и генерира напомняния само по веднъж за даден краен срок.
        public async Task ProcessAllDueRemindersAsync(CancellationToken cancellationToken = default)
        {
            CleanupOldReminderLogs();

            foreach (User user in _userService.GetAllConfirmedUsers())
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ProcessUserDueRemindersAsync(user, cancellationToken);
            }
        }

        public async Task ProcessUserDueRemindersAsync(User user, CancellationToken cancellationToken = default)
        {
            DateTime now = GetSofiaNow();
            DateTime reminderThreshold = now.AddMinutes(_reminderSettings.ReminderMinutesBeforeDue);

            foreach (TaskItem task in GetReminderCandidates(user.Id))
            {
                cancellationToken.ThrowIfCancellationRequested();

                DateTime dueDate = NormalizeTaskDueDate(task.DueDate);
                bool isUpcoming = dueDate > now && dueDate <= reminderThreshold;
                bool isOverdue = dueDate <= now && task.Status != Status.Completed;

                if (isUpcoming)
                {
                    await SendReminderIfNeededAsync(user, task, UpcomingInAppReminderType, UpcomingEmailReminderType, false);
                    continue;
                }

                if (isOverdue)
                {
                    await SendReminderIfNeededAsync(user, task, OverdueInAppReminderType, OverdueEmailReminderType, true);
                }
            }
        }

        private async Task SendReminderIfNeededAsync(User user, TaskItem task, string inAppType, string emailType, bool isOverdue)
        {
            DateTime normalizedDueDate = NormalizeTaskDueDate(task.DueDate);

            if (!ReminderAlreadySent(task.Id, inAppType, normalizedDueDate))
            {
                _appNotificationService.CreateNotification(
                    user.Id,
                    task.Id,
                    isOverdue ? "overdue" : "upcoming",
                    isOverdue ? "Task overdue" : "Task deadline is approaching",
                    isOverdue
                        ? $"\"{task.Title}\" is overdue since {NormalizeTaskDueDate(task.DueDate):dd.MM.yyyy HH:mm}."
                        : $"\"{task.Title}\" is due on {NormalizeTaskDueDate(task.DueDate):dd.MM.yyyy HH:mm}.",
                    "/MyTasks");

                StoreReminderLog(task.Id, user.Id, inAppType, normalizedDueDate);
            }

            if (!ReminderAlreadySent(task.Id, emailType, normalizedDueDate))
            {
                bool emailSent = await _emailService.SendEmailAsync(
                    user.Email,
                    isOverdue ? $"Task overdue: {task.Title}" : $"Upcoming deadline: {task.Title}",
                    BuildReminderEmailBody(user, task, isOverdue));

                if (emailSent)
                {
                    StoreReminderLog(task.Id, user.Id, emailType, normalizedDueDate);
                }
            }
        }

        private List<TaskItem> GetReminderCandidates(int userId)
        {
            var tasks = new List<TaskItem>();

            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT ""Id"", ""Title"", ""Description"", ""DueDate"", ""CreatedAt"", ""CompletedAt"", ""Status"", ""Priority"", ""IsArchived"", ""UserId""
                FROM ""Tasks""
                WHERE ""UserId"" = @userId
                  AND ""IsArchived"" = FALSE
                  AND ""Status"" <> @completedStatus
                ORDER BY ""DueDate"" ASC;";

            command.Parameters.AddWithValue("userId", userId);
            command.Parameters.AddWithValue("completedStatus", (int)Status.Completed);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                tasks.Add(MapTask(reader));
            }

            return tasks;
        }

        private bool ReminderAlreadySent(int taskId, string reminderType, DateTime referenceDueDate)
        {
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT EXISTS (
                    SELECT 1
                    FROM ""TaskReminderLogs""
                    WHERE ""TaskId"" = @taskId
                      AND ""ReminderType"" = @reminderType
                      AND ""ReferenceDueDate"" = @referenceDueDate
                );";

            command.Parameters.AddWithValue("taskId", taskId);
            command.Parameters.AddWithValue("reminderType", reminderType);
            command.Parameters.AddWithValue("referenceDueDate", referenceDueDate);
            return (bool)(command.ExecuteScalar() ?? false);
        }

        private void StoreReminderLog(int taskId, int userId, string reminderType, DateTime referenceDueDate)
        {
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO ""TaskReminderLogs"" (""UserId"", ""TaskId"", ""ReminderType"", ""ReferenceDueDate"", ""SentAt"")
                VALUES (@userId, @taskId, @reminderType, @referenceDueDate, @sentAt)
                ON CONFLICT (""TaskId"", ""ReminderType"", ""ReferenceDueDate"") DO NOTHING;";

            command.Parameters.AddWithValue("userId", userId);
            command.Parameters.AddWithValue("taskId", taskId);
            command.Parameters.AddWithValue("reminderType", reminderType);
            command.Parameters.AddWithValue("referenceDueDate", referenceDueDate);
            command.Parameters.AddWithValue("sentAt", GetSofiaNow());
            command.ExecuteNonQuery();
        }

        //Пази логовете само докато са полезни за dedup; старите записи вече не влияят на текущите срокове.
        public int CleanupOldReminderLogs()
        {
            int retentionDays = Math.Max(1, _reminderSettings.ReminderLogRetentionDays);

            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM ""TaskReminderLogs""
                WHERE ""SentAt"" < @cutoff;";

            command.Parameters.AddWithValue("cutoff", GetSofiaNow().AddDays(-retentionDays));

            return command.ExecuteNonQuery();
        }

        private static string BuildReminderEmailBody(User user, TaskItem task, bool isOverdue)
        {
            return $@"
                <p>Hello {WebUtility.HtmlEncode(user.Username)},</p>
                <p>
                    {(isOverdue
                        ? $"Your task <strong>{WebUtility.HtmlEncode(task.Title)}</strong> is overdue."
                        : $"Your task <strong>{WebUtility.HtmlEncode(task.Title)}</strong> is approaching its deadline.")}
                </p>
                <p>Due date: <strong>{NormalizeTaskDueDate(task.DueDate):dd.MM.yyyy HH:mm}</strong></p>
                <p>Please log in to review it.</p>";
        }

        private static TaskItem MapTask(NpgsqlDataReader reader)
        {
            TaskItem task = new()
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.GetString(2),
                DueDate = reader.GetDateTime(3),
                CreatedAt = reader.GetDateTime(4),
                CompletedAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                Status = (Status)reader.GetInt32(6),
                Priority = (Priority)reader.GetInt32(7),
                IsArchived = reader.GetBoolean(8),
                UserId = reader.GetInt32(9)
            };

            if (task.Status != Status.Completed && !task.IsArchived && NormalizeTaskDueDate(task.DueDate) < GetSofiaNow())
            {
                task.Status = Status.Overdue;
            }

            return task;
        }

        private static DateTime GetSofiaNow()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, SofiaTimeZone);
        }

        private static DateTime NormalizeTaskDueDate(DateTime dueDate)
        {
            if (dueDate.Kind == DateTimeKind.Utc)
            {
                return TimeZoneInfo.ConvertTimeFromUtc(dueDate, SofiaTimeZone);
            }

            if (dueDate.Kind == DateTimeKind.Local)
            {
                return TimeZoneInfo.ConvertTime(dueDate, SofiaTimeZone);
            }

            return DateTime.SpecifyKind(dueDate, DateTimeKind.Unspecified);
        }

        private static TimeZoneInfo ResolveSofiaTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Sofia");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time");
            }
        }
    }
}
