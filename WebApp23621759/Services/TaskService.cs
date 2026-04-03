using Npgsql;
using WebApp23621759.Database;
using WebApp23621759.Enums;
using WebApp23621759.Models.Entities;

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

        public List<TaskItem> GetAllTasksByUserId(int userId)
        {
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
				SELECT ""Id"", ""Title"", ""Description"", ""DueDate"", ""CreatedAt"",
                        ""CompletedAt"", ""Status"", ""Priority"", ""UserId""
				FROM ""Tasks""
				WHERE ""UserId"" = @input;";
            command.Parameters.AddWithValue("@input", userId);

            using var reader = command.ExecuteReader();

            List<TaskItem> items = new List<TaskItem>();

            while (reader.Read())
            {
                items.Add(MapTask(reader));
            }

            return items;
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
