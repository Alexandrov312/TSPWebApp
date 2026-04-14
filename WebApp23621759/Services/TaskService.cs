using Npgsql;
using WebApp23621759.Database;
using WebApp23621759.Enums;
using WebApp23621759.Models.Entities;
using WebApp23621759.Models.ViewModel.Tasks;

namespace WebApp23621759.Services
{
    public class TaskService
    {
        private readonly DatabaseService _databaseService;

        public TaskService (DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public TaskItem CreateTask(string title, string description, DateTime dueDate, Priority priority,
            int userId)
        {
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO ""Tasks"" (""Title"", ""Description"", ""DueDate"", ""CreatedAt"", 
                            ""CompletedAt"", ""Status"", ""Priority"", ""UserId"")
                VALUES (@title, @description, @dueDate, @createdAt, @completedAt, @status, 
                            @priority, @userId)
                RETURNING ""Id"", ""Title"", ""Description"", ""DueDate"", 
                            ""CreatedAt"", ""CompletedAt"", ""Status"", ""Priority"", ""UserId"";";

            command.Parameters.AddWithValue("@title", title);
            command.Parameters.AddWithValue("@description", description);
            command.Parameters.AddWithValue("@dueDate", dueDate);
            command.Parameters.AddWithValue("@createdAt", DateTime.Now);
            command.Parameters.AddWithValue("@completedAt", DBNull.Value);
            command.Parameters.AddWithValue("@status", (int)Status.Pending);
            command.Parameters.AddWithValue("@priority", (int)priority);
            command.Parameters.AddWithValue("@userId", userId);

            using var reader = command.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            return MapTask(reader);
        }

        public List<TaskItem> GetAllTasksByUserId(int userId, string sortBy, string direction)
        {
            var tasks = new List<TaskItem>();

            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();

            string sortColumn = sortBy switch
            {
                "priority" => "\"Priority\"",
                "status" => "\"Status\"",
                "createdAt" => "\"CreatedAt\"",
                _ => "\"DueDate\""
            };

            command.CommandText = $@"
                SELECT ""Id"", ""Title"", ""Description"", ""DueDate"", ""CreatedAt"", ""CompletedAt"", ""Status"", ""Priority"", ""UserId""
                FROM ""Tasks""
                WHERE ""UserId"" = @userId
                ORDER BY {sortColumn} {direction};";

            command.Parameters.AddWithValue("@userId", userId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                tasks.Add(MapTask(reader));
            }

            return tasks;
        }

        public bool DeleteTask(int taskId)
        {
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM ""Tasks""
                WHERE ""Id"" = @input";
            command.Parameters.AddWithValue("@input", taskId);

            int rowsAffected = command.ExecuteNonQuery();
            return rowsAffected > 0;
        }

        public bool UpdateTask(EditTaskViewModel model, int userId)
        {
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                UPDATE ""Tasks""
                SET
                    ""Title"" = @title,
                    ""Description"" = @description,
                    ""DueDate"" = @dueDate,
                    ""Status"" = @status,
                    ""Priority"" = @priority,
                    ""CompletedAt"" = CASE
                        WHEN @status = @completedStatus AND ""CompletedAt"" IS NULL THEN NOW()
                        WHEN @status <> @completedStatus THEN NULL
                        ELSE ""CompletedAt""
                    END
                WHERE ""Id"" = @id AND ""UserId"" = @userId;";

            command.Parameters.AddWithValue("id", model.Id);
            command.Parameters.AddWithValue("userId", userId);
            command.Parameters.AddWithValue("title", model.Title);
            command.Parameters.AddWithValue("description", (object?)model.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("dueDate", model.DueDate);
            command.Parameters.AddWithValue("status", (int)model.Status);
            command.Parameters.AddWithValue("priority", (int)model.Priority);
            command.Parameters.AddWithValue("completedStatus", (int)Status.Completed);

            int affectedRows = command.ExecuteNonQuery();
            return affectedRows > 0;
        }

        public bool SetCompleted(int taskId, int userId)
        {
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                UPDATE ""Tasks""
                SET 
                    ""Status"" = @status,
                    ""CompletedAt"" = NOW()
                WHERE ""Id"" = @id AND ""UserId"" = @userId;";

            command.Parameters.AddWithValue("id", taskId);
            command.Parameters.AddWithValue("userId", userId);
            command.Parameters.AddWithValue("status", (int)Status.Completed);

            int affectedRows = command.ExecuteNonQuery();
            return affectedRows > 0;
        }

        public TaskItem GetById(int id, int userId)
        {
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT ""Id"", ""Title"", ""Description"", ""DueDate"",
                       ""CreatedAt"", ""CompletedAt"", ""Status"", ""Priority"", ""UserId""
                FROM ""Tasks""
                WHERE ""Id"" = @id AND ""UserId"" = @userId
                LIMIT 1;";

            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("userId", userId);

            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null;

            return MapTask(reader);
        }
        public List<TaskItem> GetTasksForMonth(int userId, int year, int month)
        {
            var tasks = new List<TaskItem>();

            DateTime startOfMonth = new DateTime(year, month, 1);
            DateTime startOfNextMonth = startOfMonth.AddMonths(1);

            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT ""Id"", ""Title"", ""Description"", ""DueDate"", ""CreatedAt"", ""CompletedAt"", ""Status"", ""Priority"", ""UserId""
                FROM ""Tasks""
                WHERE ""UserId"" = @userId
                  AND ""DueDate"" >= @startOfMonth
                  AND ""DueDate"" < @startOfNextMonth
                ORDER BY ""DueDate"" ASC;";

            command.Parameters.AddWithValue("userId", userId);
            command.Parameters.AddWithValue("startOfMonth", startOfMonth);
            command.Parameters.AddWithValue("startOfNextMonth", startOfNextMonth);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                tasks.Add(MapTask(reader));
            }

            return tasks;
        }

        public bool ChangeStatus(int taskId, int userId, Status newStatus)
        {
            if (newStatus == Status.Overdue)
            {
                return false;
            }

            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                UPDATE ""Tasks""
                SET
                    ""Status"" = @status,
                    ""CompletedAt"" = CASE
                        WHEN @status = @completedStatus AND ""CompletedAt"" IS NULL THEN NOW()
                        WHEN @status <> @completedStatus THEN NULL
                        ELSE ""CompletedAt""
                    END
                WHERE ""Id"" = @id AND ""UserId"" = @userId AND ""Status"" <> @overdueStatus;";

            command.Parameters.AddWithValue("id", taskId);
            command.Parameters.AddWithValue("userId", userId);
            command.Parameters.AddWithValue("status", (int)newStatus);
            command.Parameters.AddWithValue("completedStatus", (int)Status.Completed);
            command.Parameters.AddWithValue("overdueStatus", (int)Status.Overdue);

            int affectedRows = command.ExecuteNonQuery();
            return affectedRows > 0;
        }

        public TaskItem SyncStatusWithSubTasks(int taskId, int userId)
        {
            var task = GetById(taskId, userId);
            if (task == null)
            {
                return null;
            }

            var subTaskStatuses = GetSubTaskStatuses(taskId, userId);
            if (subTaskStatuses.Count == 0)
            {
                return task;
            }

            Status targetStatus;
            if (subTaskStatuses.All(status => status == Status.Completed))
            {
                targetStatus = Status.Completed;
            }
            else if (task.Status == Status.Completed || subTaskStatuses.Any(status => status == Status.InProgress || status == Status.Completed))
            {
                targetStatus = Status.InProgress;
            }
            else
            {
                targetStatus = Status.Pending;
            }

            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE ""Tasks""
                SET
                    ""Status"" = @status,
                    ""CompletedAt"" = CASE
                        WHEN @status = @completedStatus AND ""CompletedAt"" IS NULL THEN NOW()
                        WHEN @status <> @completedStatus THEN NULL
                        ELSE ""CompletedAt""
                    END
                WHERE ""Id"" = @id AND ""UserId"" = @userId;";

            command.Parameters.AddWithValue("id", taskId);
            command.Parameters.AddWithValue("userId", userId);
            command.Parameters.AddWithValue("status", (int)targetStatus);
            command.Parameters.AddWithValue("completedStatus", (int)Status.Completed);
            command.ExecuteNonQuery();

            return GetById(taskId, userId);
        }

        private List<Status> GetSubTaskStatuses(int taskId, int userId)
        {
            var statuses = new List<Status>();

            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT ""Status""
                FROM ""SubTasks""
                WHERE ""TaskId"" = @taskId AND ""UserId"" = @userId;";

            command.Parameters.AddWithValue("taskId", taskId);
            command.Parameters.AddWithValue("userId", userId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                statuses.Add((Status)reader.GetInt32(0));
            }

            return statuses;
        }

        private static TaskItem MapTask(NpgsqlDataReader reader)
        {
            return new TaskItem()
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.GetString(2),
                DueDate = reader.GetDateTime(3),
                CreatedAt = reader.GetDateTime(4),
                CompletedAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                Status = (Status)reader.GetInt32(6),
                Priority = (Priority)reader.GetInt32(7),
                UserId = reader.GetInt32(8)
            };
        }
    }
}
