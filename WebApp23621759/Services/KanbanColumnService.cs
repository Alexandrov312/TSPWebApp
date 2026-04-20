using Npgsql;
using WebApp23621759.Database;
using WebApp23621759.Enums;
using WebApp23621759.Models.Entities;

namespace WebApp23621759.Services
{
    public class KanbanColumnService
    {
        private readonly DatabaseService _databaseService;

        public KanbanColumnService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public List<KanbanColumnItem> GetColumnsForTask(int taskId, int userId)
        {
            EnsureDefaultColumnsForTask(taskId, userId);

            var columns = new List<KanbanColumnItem>();
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT ""Id"", ""Name"", ""DisplayOrder"", ""IsCompletedColumn"", ""IsDefaultColumn"", ""StatusValue"", ""TaskId"", ""UserId""
                FROM ""KanbanColumns""
                WHERE ""TaskId"" = @taskId AND ""UserId"" = @userId
                ORDER BY ""DisplayOrder"", ""Id"";";

            command.Parameters.AddWithValue("taskId", taskId);
            command.Parameters.AddWithValue("userId", userId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                columns.Add(MapColumn(reader));
            }

            return columns;
        }

        public KanbanColumnItem? GetById(int columnId, int userId)
        {
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT ""Id"", ""Name"", ""DisplayOrder"", ""IsCompletedColumn"", ""IsDefaultColumn"", ""StatusValue"", ""TaskId"", ""UserId""
                FROM ""KanbanColumns""
                WHERE ""Id"" = @columnId AND ""UserId"" = @userId
                LIMIT 1;";

            command.Parameters.AddWithValue("columnId", columnId);
            command.Parameters.AddWithValue("userId", userId);

            using var reader = command.ExecuteReader();
            return reader.Read() ? MapColumn(reader) : null;
        }

        public KanbanColumnItem? GetDefaultColumnForStatus(int taskId, int userId, Status status)
        {
            EnsureDefaultColumnsForTask(taskId, userId);

            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT ""Id"", ""Name"", ""DisplayOrder"", ""IsCompletedColumn"", ""IsDefaultColumn"", ""StatusValue"", ""TaskId"", ""UserId""
                FROM ""KanbanColumns""
                WHERE ""TaskId"" = @taskId
                  AND ""UserId"" = @userId
                  AND ""IsDefaultColumn"" = TRUE
                  AND ""StatusValue"" = @status
                LIMIT 1;";

            command.Parameters.AddWithValue("taskId", taskId);
            command.Parameters.AddWithValue("userId", userId);
            command.Parameters.AddWithValue("status", (int)status);

            using var reader = command.ExecuteReader();
            return reader.Read() ? MapColumn(reader) : null;
        }

        public void EnsureDefaultColumnsForTask(int taskId, int userId)
        {
            InsertDefaultColumn(taskId, userId, "Pending", 0, Status.Pending, false);
            InsertDefaultColumn(taskId, userId, "In Progress", 1, Status.InProgress, false);
            InsertDefaultColumn(taskId, userId, "Completed", 2, Status.Completed, true);
        }

        private void InsertDefaultColumn(int taskId, int userId, string name, int displayOrder, Status status, bool isCompletedColumn)
        {
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO ""KanbanColumns""
                    (""Name"", ""DisplayOrder"", ""IsCompletedColumn"", ""IsDefaultColumn"", ""StatusValue"", ""TaskId"", ""UserId"")
                SELECT @name, @displayOrder, @isCompletedColumn, TRUE, @statusValue, @taskId, @userId
                WHERE EXISTS (
                    SELECT 1 FROM ""Tasks""
                    WHERE ""Id"" = @taskId AND ""UserId"" = @userId
                )
                AND NOT EXISTS (
                    SELECT 1 FROM ""KanbanColumns""
                    WHERE ""TaskId"" = @taskId
                      AND ""UserId"" = @userId
                      AND ""IsDefaultColumn"" = TRUE
                      AND ""StatusValue"" = @statusValue
                );";

            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("displayOrder", displayOrder);
            command.Parameters.AddWithValue("isCompletedColumn", isCompletedColumn);
            command.Parameters.AddWithValue("statusValue", (int)status);
            command.Parameters.AddWithValue("taskId", taskId);
            command.Parameters.AddWithValue("userId", userId);
            command.ExecuteNonQuery();
        }

        private static KanbanColumnItem MapColumn(NpgsqlDataReader reader)
        {
            return new KanbanColumnItem
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                DisplayOrder = reader.GetInt32(2),
                IsCompletedColumn = reader.GetBoolean(3),
                IsDefaultColumn = reader.GetBoolean(4),
                StatusValue = (Status)reader.GetInt32(5),
                TaskId = reader.GetInt32(6),
                UserId = reader.GetInt32(7)
            };
        }
    }
}
