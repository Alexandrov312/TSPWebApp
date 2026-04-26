using Npgsql;
using WebApp23621759.Database;
using WebApp23621759.Models.Entities;

namespace WebApp23621759.Services
{
    public class AppNotificationService
    {
        private readonly DatabaseService _databaseService;

        public AppNotificationService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public void CreateNotification(int userId, int? taskId, string type, string title, string message, string? targetUrl = null)
        {
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO ""AppNotifications""
                    (""UserId"", ""TaskId"", ""Type"", ""Title"", ""Message"", ""TargetUrl"", ""IsRead"", ""CreatedAt"", ""ReadAt"")
                VALUES
                    (@userId, @taskId, @type, @title, @message, @targetUrl, FALSE, @createdAt, NULL);";

            command.Parameters.AddWithValue("userId", userId);
            command.Parameters.AddWithValue("taskId", taskId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("type", type);
            command.Parameters.AddWithValue("title", title);
            command.Parameters.AddWithValue("message", message);
            command.Parameters.AddWithValue("targetUrl", (object?)targetUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            command.ExecuteNonQuery();
        }

        public int GetUnreadCount(int userId)
        {
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*)
                FROM ""AppNotifications""
                WHERE ""UserId"" = @userId
                  AND ""IsRead"" = FALSE;";

            command.Parameters.AddWithValue("userId", userId);
            return Convert.ToInt32(command.ExecuteScalar() ?? 0);
        }

        public List<AppNotificationItem> GetLatestUnreadNotifications(int userId, int limit = 8)
        {
            var notifications = new List<AppNotificationItem>();

            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT ""Id"", ""UserId"", ""TaskId"", ""Type"", ""Title"", ""Message"", ""TargetUrl"", ""IsRead"", ""CreatedAt"", ""ReadAt""
                FROM ""AppNotifications""
                WHERE ""UserId"" = @userId
                  AND ""IsRead"" = FALSE
                ORDER BY ""CreatedAt"" DESC
                LIMIT @limit;";

            command.Parameters.AddWithValue("userId", userId);
            command.Parameters.AddWithValue("limit", limit);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                notifications.Add(MapNotification(reader));
            }

            return notifications;
        }

        public bool MarkAsRead(int notificationId, int userId)
        {
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE ""AppNotifications""
                SET ""IsRead"" = TRUE, ""ReadAt"" = NOW()
                WHERE ""Id"" = @id AND ""UserId"" = @userId;";

            command.Parameters.AddWithValue("id", notificationId);
            command.Parameters.AddWithValue("userId", userId);
            return command.ExecuteNonQuery() > 0;
        }

        public int MarkAllAsRead(int userId)
        {
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE ""AppNotifications""
                SET ""IsRead"" = TRUE, ""ReadAt"" = NOW()
                WHERE ""UserId"" = @userId
                  AND ""IsRead"" = FALSE;";

            command.Parameters.AddWithValue("userId", userId);
            return command.ExecuteNonQuery();
        }

        private static AppNotificationItem MapNotification(NpgsqlDataReader reader)
        {
            return new AppNotificationItem
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                TaskId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                Type = reader.GetString(3),
                Title = reader.GetString(4),
                Message = reader.GetString(5),
                TargetUrl = reader.IsDBNull(6) ? null : reader.GetString(6),
                IsRead = reader.GetBoolean(7),
                CreatedAt = reader.GetDateTime(8),
                ReadAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9)
            };
        }
    }
}
