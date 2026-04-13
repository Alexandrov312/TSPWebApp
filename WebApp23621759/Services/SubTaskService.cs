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
                    (""Title"", ""Description"", ""Status"", ""CompletedAt"", ""BlockedBySubTaskId"", ""TaskId"", ""UserId"")
                VALUES
                    (@title, @description, @status, NULL, @blockedBySubTaskId, @taskId, @userId)
                RETURNING
                    ""Id"", ""Title"", ""Description"", ""Status"", ""CompletedAt"", ""BlockedBySubTaskId"", ""TaskId"", ""UserId"";";

            command.Parameters.AddWithValue("title", title);
            command.Parameters.AddWithValue("description", description ?? string.Empty);
            command.Parameters.AddWithValue("status", (int)Status.Pending);
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

        public SubTaskItem CreateSubTask(string title, string description, int taskId, int userId)
        {
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO ""SubTasks""
                    (""Title"", ""Description"", ""Status"", ""CompletedAt"", ""TaskId"", ""UserId"", ""BlockedBySubTaskId"")
                VALUES
                    (@title, @description, @status, NULL, @taskId, @userId, NULL)
                RETURNING
                    ""Id"", ""Title"", ""Description"", ""Status"", ""CompletedAt"", ""BlockedBySubTaskId"", ""TaskId"", ""UserId"";";

            command.Parameters.AddWithValue("title", title);
            command.Parameters.AddWithValue("description", description ?? string.Empty);
            command.Parameters.AddWithValue("status", (int)Status.Pending);
            command.Parameters.AddWithValue("taskId", taskId);
            command.Parameters.AddWithValue("userId", userId);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return MapSubTask(reader);
        }

        public List<SubTaskItem> GetAllSubTasks(int taskId)
        {
            var tasks = new List<SubTaskItem>();

            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    ""Id"", ""Title"", ""Description"", ""Status"", ""CompletedAt"", ""BlockedBySubTaskId"", ""TaskId"", ""UserId""
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

        public List<SubTaskItem> GetAllSubTasks(int taskId, int userId)
        {
            return GetAllSubTasks(taskId)
                .Where(subTask => subTask.UserId == userId)
                .ToList();
        }

        public bool DeleteTask(int subTaskId, int userId)
        {
            using var connection = _databaseService.GetOpenConnection();
            using (var cleanupCommand = connection.CreateCommand())
            {
                cleanupCommand.CommandText = @"
                    UPDATE ""SubTasks""
                    SET ""BlockedBySubTaskId"" = NULL
                    WHERE ""BlockedBySubTaskId"" = @subTaskId
                      AND ""UserId"" = @userId;";

                cleanupCommand.Parameters.AddWithValue("subTaskId", subTaskId);
                cleanupCommand.Parameters.AddWithValue("userId", userId);
                cleanupCommand.ExecuteNonQuery();
            }

            using var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM ""SubTasks""
                WHERE ""Id"" = @subTaskId
                  AND ""UserId"" = @userId;";

            command.Parameters.AddWithValue("subTaskId", subTaskId);
            command.Parameters.AddWithValue("userId", userId);

            return command.ExecuteNonQuery() > 0;
        }

        public bool UpdateTask(SubTaskViewModel model, int userId)
        {
            var currentSubTask = GetById(model.Id);
            if (currentSubTask == null || currentSubTask.UserId != userId)
            {
                return false;
            }

            int? blockedBySubTaskId = ResolveBlockedBySubTaskId(
                model.Id,
                currentSubTask.TaskId,
                userId,
                model.BlockedBySubTaskId);

            if (model.BlockedBySubTaskId.HasValue && model.BlockedBySubTaskId.Value > 0 && blockedBySubTaskId == null)
            {
                return false;
            }

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

            if (blockedBySubTaskId.HasValue)
            {
                command.Parameters.AddWithValue("blockedBySubTaskId", blockedBySubTaskId.Value);
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
                    ""Id"", ""Title"", ""Description"", ""Status"", ""CompletedAt"", ""BlockedBySubTaskId"", ""TaskId"", ""UserId""
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
                        ""Id"", ""Title"", ""Description"", ""Status"", ""CompletedAt"", ""BlockedBySubTaskId"", ""TaskId"", ""UserId""
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
            var allSubTasks = GetAllSubTasks(task.TaskId, userId);

            if (nextStatus == Status.InProgress && task.BlockedBySubTaskId.HasValue)
            {
                var blocker = allSubTasks.FirstOrDefault(subTask => subTask.Id == task.BlockedBySubTaskId.Value);
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

            bool updated = updateCommand.ExecuteNonQuery() > 0;
            if (!updated)
            {
                return false;
            }

            if (nextStatus == Status.Pending)
            {
                ResetDependentSubTasks(connection, task.Id, userId, allSubTasks);
            }

            return true;
        }

        public bool UpdateDependency(int subTaskId, int? blockedBySubTaskId, int userId)
        {
            var subTask = GetById(subTaskId);
            if (subTask == null || subTask.UserId != userId)
            {
                return false;
            }

            int? resolvedDependencyId = ResolveBlockedBySubTaskId(
                subTaskId,
                subTask.TaskId,
                userId,
                blockedBySubTaskId);

            if (blockedBySubTaskId.HasValue && blockedBySubTaskId.Value > 0 && resolvedDependencyId == null)
            {
                return false;
            }

            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE ""SubTasks""
                SET ""BlockedBySubTaskId"" = @blockedBySubTaskId
                WHERE ""Id"" = @subTaskId AND ""UserId"" = @userId;";

            command.Parameters.AddWithValue("subTaskId", subTaskId);
            command.Parameters.AddWithValue("userId", userId);

            if (resolvedDependencyId.HasValue)
            {
                command.Parameters.AddWithValue("blockedBySubTaskId", resolvedDependencyId.Value);
            }
            else
            {
                command.Parameters.AddWithValue("blockedBySubTaskId", DBNull.Value);
            }

            return command.ExecuteNonQuery() > 0;
        }

        public int SetAllCompletedForTask(int taskId, int userId)
        {
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE ""SubTasks""
                SET
                    ""Status"" = @status,
                    ""CompletedAt"" = NOW()
                WHERE ""TaskId"" = @taskId
                  AND ""UserId"" = @userId
                  AND ""Status"" <> @completedStatus;";

            command.Parameters.AddWithValue("status", (int)Status.Completed);
            command.Parameters.AddWithValue("completedStatus", (int)Status.Completed);
            command.Parameters.AddWithValue("taskId", taskId);
            command.Parameters.AddWithValue("userId", userId);

            return command.ExecuteNonQuery();
        }

        private static SubTaskItem MapSubTask(NpgsqlDataReader reader)
        {
            return new SubTaskItem
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Status = (Status)reader.GetInt32(3),
                CompletedAt = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                BlockedBySubTaskId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                TaskId = reader.GetInt32(6),
                UserId = reader.GetInt32(7)
            };
        }

        private int? ResolveBlockedBySubTaskId(int subTaskId, int taskId, int userId, int? blockedBySubTaskId)
        {
            if (!blockedBySubTaskId.HasValue || blockedBySubTaskId.Value <= 0)
            {
                return null;
            }

            if (blockedBySubTaskId.Value == subTaskId)
            {
                return null;
            }

            var subTasks = GetAllSubTasks(taskId, userId);
            var dependency = subTasks.FirstOrDefault(subTask => subTask.Id == blockedBySubTaskId.Value);
            if (dependency == null)
            {
                return null;
            }

            if (CreatesCycle(subTaskId, blockedBySubTaskId.Value, subTasks))
            {
                return null;
            }

            return blockedBySubTaskId.Value;
        }

        private static bool CreatesCycle(int currentSubTaskId, int candidateDependencyId, List<SubTaskItem> subTasks)
        {
            var subTaskMap = subTasks.ToDictionary(subTask => subTask.Id);
            var visited = new HashSet<int>();
            var nextId = candidateDependencyId;

            while (subTaskMap.TryGetValue(nextId, out var current))
            {
                if (!visited.Add(nextId))
                {
                    break;
                }

                if (current.Id == currentSubTaskId)
                {
                    return true;
                }

                if (!current.BlockedBySubTaskId.HasValue)
                {
                    return false;
                }

                nextId = current.BlockedBySubTaskId.Value;
            }

            return false;
        }

        private static void ResetDependentSubTasks(
            NpgsqlConnection connection,
            int rootSubTaskId,
            int userId,
            List<SubTaskItem> allSubTasks)
        {
            var dependentsByParentId = allSubTasks
                .Where(subTask => subTask.BlockedBySubTaskId.HasValue)
                .GroupBy(subTask => subTask.BlockedBySubTaskId!.Value)
                .ToDictionary(group => group.Key, group => group.Select(subTask => subTask.Id).ToList());

            var descendantIds = new List<int>();
            var stack = new Stack<int>();
            var visited = new HashSet<int>();
            stack.Push(rootSubTaskId);

            while (stack.Count > 0)
            {
                int currentId = stack.Pop();
                if (!dependentsByParentId.TryGetValue(currentId, out var dependentIds))
                {
                    continue;
                }

                foreach (int dependentId in dependentIds)
                {
                    if (!visited.Add(dependentId))
                    {
                        continue;
                    }

                    descendantIds.Add(dependentId);
                    stack.Push(dependentId);
                }
            }

            if (descendantIds.Count == 0)
            {
                return;
            }

            using var resetCommand = connection.CreateCommand();
            resetCommand.CommandText = @"
                UPDATE ""SubTasks""
                SET
                    ""Status"" = @status,
                    ""CompletedAt"" = @completedAt
                WHERE ""Id"" = ANY(@subTaskIds)
                  AND ""UserId"" = @userId;";

            resetCommand.Parameters.AddWithValue("status", (int)Status.Pending);
            resetCommand.Parameters.AddWithValue("completedAt", DBNull.Value);
            resetCommand.Parameters.AddWithValue("subTaskIds", descendantIds.ToArray());
            resetCommand.Parameters.AddWithValue("userId", userId);
            resetCommand.ExecuteNonQuery();
        }
    }
}
