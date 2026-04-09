using Npgsql;
using WebApp23621759.Database;
using WebApp23621759.Enums;
using WebApp23621759.Models.Entities;
using WebApp23621759.Models.ViewModel;
using WebApp23621759.Helpers;

namespace WebApp23621759.Services
{
    public class SubTaskService
    {
        private readonly DatabaseService _databaseService;

        public SubTaskService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public SubTaskItem CreateSubTask(string title, string description, int blockedBySubTaskId, int taskId, int userId)
        {
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO ""SubTasks""
                    (""Title"", ""Description"", ""Status"", ""CreatedAt"", ""CompletedAt"", ""BlockedBySubTaskId"", ""TaskId"", ""UserId"")
                VALUES
                    (@title, @description, @status, @createdAt, NULL, @blockedBySubTaskId, @taskId, @userId)
                RETURNING
                    ""Id"", ""Title"", ""Description"", ""Status"", ""CreatedAt"", ""CompletedAt"", ""BlockedBySubTaskId"", ""TaskId"", ""UserId"";";

            command.Parameters.AddWithValue("title", title);
            command.Parameters.AddWithValue("description", description ?? string.Empty);
            command.Parameters.AddWithValue("status", (int)Status.Pending);
            command.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("taskId", taskId);
            command.Parameters.AddWithValue("userId", userId);

            if (blockedBySubTaskId > 0)
            {
                command.Parameters.AddWithValue("blockedBySubTaskId", blockedBySubTaskId);
            }
            else
            {
                command.Parameters.AddWithValue("blockedBySubTaskId", DBNull.Value);
            }

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return MapSubTask(reader);
        }

        public void CreateSubTask(string title, string description, int taskId, int userId)
        {
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
        INSERT INTO ""SubTasks""
            (""Title"", ""Description"", ""Status"", ""CreatedAt"", ""CompletedAt"", ""TaskId"", ""UserId"", ""BlockedBySubTaskId"")
        VALUES
            (@title, @description, @status, @createdAt, NULL, @taskId, @userId, NULL);";

            command.Parameters.AddWithValue("title", title);
            command.Parameters.AddWithValue("description", description ?? string.Empty);
            command.Parameters.AddWithValue("status", (int)Status.Pending);
            command.Parameters.AddWithValue("createdAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("taskId", taskId);
            command.Parameters.AddWithValue("userId", userId);

            command.ExecuteNonQuery();
        }

        public List<SubTaskItem> GetAllSubTasks(int taskId)
        {
            var tasks = new List<SubTaskItem>();

            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    ""Id"", ""Title"", ""Description"", ""Status"", ""CreatedAt"", ""CompletedAt"", ""BlockedBySubTaskId"", ""TaskId"", ""UserId""
                FROM ""SubTasks""
                WHERE ""TaskId"" = @taskId
                ORDER BY ""Id"";";

            command.Parameters.AddWithValue("taskId", taskId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                tasks.Add(MapSubTask(reader));
            }

            return tasks;
        }

        public bool DeleteTask(int subTaskId)
        {
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM ""SubTasks""
                WHERE ""Id"" = @subTaskId;";

            command.Parameters.AddWithValue("subTaskId", subTaskId);

            return command.ExecuteNonQuery() > 0;
        }

        public bool UpdateTask(SubTaskViewModel model, int userId)
        {
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE ""SubTasks""
                SET
                    ""Title"" = @title,
                    ""Description"" = @description,
                    ""BlockedBySubTaskId"" = @blockedBySubTaskId
                WHERE ""Id"" = @id
                  AND ""UserId"" = @userId;";

            command.Parameters.AddWithValue("id", model.Id);
            command.Parameters.AddWithValue("title", model.Title);
            command.Parameters.AddWithValue("description", model.Description ?? string.Empty);
            command.Parameters.AddWithValue("userId", userId);

            if (model.BlockedBySubTaskId > 0)
            {
                command.Parameters.AddWithValue("blockedBySubTaskId", model.BlockedBySubTaskId);
            }
            else
            {
                command.Parameters.AddWithValue("blockedBySubTaskId", DBNull.Value);
            }

            return command.ExecuteNonQuery() > 0;
        }

        public SubTaskItem GetById(int subTaskId)
        {
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    ""Id"", ""Title"", ""Description"", ""Status"", ""CreatedAt"", ""CompletedAt"", ""BlockedBySubTaskId"", ""TaskId"", ""UserId""
                FROM ""SubTasks""
                WHERE ""Id"" = @subTaskId
                LIMIT 1;";

            command.Parameters.AddWithValue("subTaskId", subTaskId);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return MapSubTask(reader);
        }

        public bool ChangeStatus(int subTaskId, int userId)
        {
            using var connection = _databaseService.GetOpenConnection();

            SubTaskItem task;
            using (var getCommand = connection.CreateCommand())
            {
                getCommand.CommandText = @"
                    SELECT
                        ""Id"", ""Title"", ""Description"", ""Status"", ""CreatedAt"", ""CompletedAt"", ""BlockedBySubTaskId"", ""TaskId"", ""UserId""
                    FROM ""SubTasks""
                    WHERE ""Id"" = @subTaskId
                      AND ""UserId"" = @userId
                    LIMIT 1;";

                getCommand.Parameters.AddWithValue("subTaskId", subTaskId);
                getCommand.Parameters.AddWithValue("userId", userId);

                using var reader = getCommand.ExecuteReader();
                if (!reader.Read())
                {
                    return false;
                }

                task = MapSubTask(reader);
            }

            var nextStatus = StatusHelper.GetNextStatus(task.Status);

            if (nextStatus == Status.InProgress && task.BlockedBySubTaskId.HasValue)
            {
                var blocker = GetById(task.BlockedBySubTaskId.Value);
                if (blocker == null || blocker.Status != Status.Completed)
                {
                    return false;
                }
            }

            using var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = @"
                UPDATE ""SubTasks""
                SET
                    ""Status"" = @status,
                    ""CompletedAt"" = @completedAt
                WHERE ""Id"" = @subTaskId
                  AND ""UserId"" = @userId;";

            updateCommand.Parameters.AddWithValue("status", (int)nextStatus);
            updateCommand.Parameters.AddWithValue("subTaskId", subTaskId);
            updateCommand.Parameters.AddWithValue("userId", userId);

            if (nextStatus == Status.Completed)
            {
                updateCommand.Parameters.AddWithValue("completedAt", DateTime.UtcNow);
            }
            else
            {
                updateCommand.Parameters.AddWithValue("completedAt", DBNull.Value);
            }

            return updateCommand.ExecuteNonQuery() > 0;
        }

        private static SubTaskItem MapSubTask(NpgsqlDataReader reader)
        {
            return new SubTaskItem
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Status = (Status)reader.GetInt32(3),
                CreatedAt = reader.GetDateTime(4),
                CompletedAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                BlockedBySubTaskId = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                TaskId = reader.GetInt32(7),
                UserId = reader.GetInt32(8)
            };
        }
    }
}