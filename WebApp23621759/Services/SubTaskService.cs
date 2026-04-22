using Npgsql;
using WebApp23621759.Database;
using WebApp23621759.Enums;
using WebApp23621759.Helpers;
using WebApp23621759.Models.Entities;
using WebApp23621759.Models.ViewModel.SubTasks;

namespace WebApp23621759.Services
{
    public class SubTaskService
    {
        private readonly DatabaseService _databaseService;
        private readonly KanbanColumnService _kanbanColumnService;

        public SubTaskService(DatabaseService databaseService, KanbanColumnService kanbanColumnService)
        {
            _databaseService = databaseService;
            _kanbanColumnService = kanbanColumnService;
        }

        //Валидира целия списък с нови подзадачи преди да се запише каквото и да е в базата
        public (bool IsValid, string ErrorMessage) ValidateNewSubTasks(List<CreateSubTaskInputModel> subTasks)
        {
            if (subTasks == null || subTasks.Count == 0)
            {
                return (true, string.Empty);
            }

            for (int i = 0; i < subTasks.Count; i++)
            {
                int? blockedByIndex = subTasks[i].BlockedByIndex;
                if (!blockedByIndex.HasValue)
                {
                    continue;
                }

                if (blockedByIndex.Value < 0 || blockedByIndex.Value >= subTasks.Count)
                {
                    return (false, $"Subtask \"{subTasks[i].Title}\" has an invalid dependency.");
                }

                if (blockedByIndex.Value == i)
                {
                    return (false, $"Subtask \"{subTasks[i].Title}\" cannot depend on itself.");
                }

                if (CreatesNewSubTaskCycle(i, blockedByIndex.Value, subTasks))
                {
                    return (false, $"Subtask \"{subTasks[i].Title}\" creates a circular dependency.");
                }
            }

            return (true, string.Empty);
        }

        //Създава всички нови подзадачи и връзките между тях, след като списъкът е преминал валидация
        public List<SubTaskItem> CreateSubTasks(List<CreateSubTaskInputModel> subTasks, int taskId, int userId)
        {
            var validationResult = ValidateNewSubTasks(subTasks);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException(validationResult.ErrorMessage);
            }

            var createdSubTasks = new List<SubTaskItem>();

            foreach (var subTask in subTasks)
            {
                createdSubTasks.Add(CreateSubTask(
                    subTask.Title,
                    subTask.Description ?? "No description",
                    null,
                    taskId,
                    userId));
            }

            for (int i = 0; i < subTasks.Count; i++)
            {
                int? blockedByIndex = subTasks[i].BlockedByIndex;
                if (!blockedByIndex.HasValue)
                {
                    continue;
                }

                UpdateDependency(
                    createdSubTasks[i].Id,
                    createdSubTasks[blockedByIndex.Value].Id,
                    userId);
            }

            return createdSubTasks;
        }

        public SubTaskItem CreateSubTask(string title, string description, int? blockedBySubTaskId, int taskId, int userId)
        {
            var pendingColumn = _kanbanColumnService.GetDefaultColumnForStatus(taskId, userId, Status.Pending);

            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO ""SubTasks""
                    (""Title"", ""Description"", ""Status"", ""CompletedAt"", ""KanbanColumnId"", ""BlockedBySubTaskId"", ""TaskId"", ""UserId"")
                VALUES
                    (@title, @description, @status, NULL, @kanbanColumnId, @blockedBySubTaskId, @taskId, @userId)
                RETURNING
                    ""Id"", ""Title"", ""Description"", ""Status"", ""CompletedAt"", ""KanbanColumnId"", ""BlockedBySubTaskId"", ""TaskId"", ""UserId"";";

            command.Parameters.AddWithValue("title", title);
            command.Parameters.AddWithValue("description", description ?? string.Empty);
            command.Parameters.AddWithValue("status", (int)Status.Pending);
            command.Parameters.AddWithValue("kanbanColumnId", pendingColumn?.Id ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("taskId", taskId);
            command.Parameters.AddWithValue("userId", userId);
            command.Parameters.AddWithValue("blockedBySubTaskId", blockedBySubTaskId > 0 ? blockedBySubTaskId.Value : (object)DBNull.Value);

            using var reader = command.ExecuteReader();
            return reader.Read() ? MapSubTask(reader) : null;
        }

        public List<SubTaskItem> GetAllSubTasks(int taskId)
        {
            var tasks = new List<SubTaskItem>();

            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT ""Id"", ""Title"", ""Description"", ""Status"", ""CompletedAt"", ""KanbanColumnId"", ""BlockedBySubTaskId"", ""TaskId"", ""UserId""
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

            if (currentSubTask.Status == Status.Completed &&
                IsDependencyChanged(currentSubTask.BlockedBySubTaskId, model.BlockedBySubTaskId))
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
            command.Parameters.AddWithValue("blockedBySubTaskId", blockedBySubTaskId.HasValue ? blockedBySubTaskId.Value : (object)DBNull.Value);

            return command.ExecuteNonQuery() > 0;
        }

        public SubTaskItem GetById(int subTaskId)
        {
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT ""Id"", ""Title"", ""Description"", ""Status"", ""CompletedAt"", ""KanbanColumnId"", ""BlockedBySubTaskId"", ""TaskId"", ""UserId""
                FROM ""SubTasks""
                WHERE ""Id"" = @subTaskId
                LIMIT 1;";

            command.Parameters.AddWithValue("subTaskId", subTaskId);

            using var reader = command.ExecuteReader();
            return reader.Read() ? MapSubTask(reader) : null;
        }

        public bool ChangeStatus(int subTaskId, int userId)
        {
            var task = GetById(subTaskId);
            if (task == null || task.UserId != userId)
            {
                return false;
            }

            var nextStatus = StatusHelper.GetNextStatus(task.Status);
            return SetStatus(subTaskId, userId, nextStatus);
        }

        public bool SetStatus(int subTaskId, int userId, Status targetStatus)
        {
            if (targetStatus == Status.Overdue)
            {
                return false;
            }

            var task = GetById(subTaskId);
            if (task == null || task.UserId != userId)
            {
                return false;
            }

            var targetColumn = _kanbanColumnService.GetDefaultColumnForStatus(task.TaskId, userId, targetStatus);
            if (targetColumn == null)
            {
                return false;
            }

            return SetStatusAndColumn(subTaskId, userId, targetStatus, targetColumn.Id);
        }

        public bool SetColumn(int subTaskId, int userId, KanbanColumnItem targetColumn)
        {
            var targetStatus = targetColumn.StatusValue;
            return SetStatusAndColumn(subTaskId, userId, targetStatus, targetColumn.Id);
        }

        public bool UpdateDependency(int subTaskId, int? blockedBySubTaskId, int userId)
        {
            var subTask = GetById(subTaskId);
            if (subTask == null || subTask.UserId != userId)
            {
                return false;
            }

            if (subTask.Status == Status.Completed &&
                IsDependencyChanged(subTask.BlockedBySubTaskId, blockedBySubTaskId))
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
            command.Parameters.AddWithValue("blockedBySubTaskId", resolvedDependencyId.HasValue ? resolvedDependencyId.Value : (object)DBNull.Value);

            return command.ExecuteNonQuery() > 0;
        }

        public int SetAllCompletedForTask(int taskId, int userId)
        {
            var completedColumn = _kanbanColumnService.GetDefaultColumnForStatus(taskId, userId, Status.Completed);

            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE ""SubTasks""
                SET
                    ""Status"" = @status,
                    ""CompletedAt"" = NOW(),
                    ""KanbanColumnId"" = @kanbanColumnId
                WHERE ""TaskId"" = @taskId
                  AND ""UserId"" = @userId
                  AND ""Status"" <> @completedStatus;";

            command.Parameters.AddWithValue("status", (int)Status.Completed);
            command.Parameters.AddWithValue("completedStatus", (int)Status.Completed);
            command.Parameters.AddWithValue("kanbanColumnId", completedColumn?.Id ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("taskId", taskId);
            command.Parameters.AddWithValue("userId", userId);

            return command.ExecuteNonQuery();
        }

        public int SetAllPendingForTask(int taskId, int userId)
        {
            var pendingColumn = _kanbanColumnService.GetDefaultColumnForStatus(taskId, userId, Status.Pending);

            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE ""SubTasks""
                SET
                    ""Status"" = @status,
                    ""CompletedAt"" = NULL,
                    ""KanbanColumnId"" = @kanbanColumnId
                WHERE ""TaskId"" = @taskId
                  AND ""UserId"" = @userId
                  AND ""Status"" <> @pendingStatus;";

            command.Parameters.AddWithValue("status", (int)Status.Pending);
            command.Parameters.AddWithValue("pendingStatus", (int)Status.Pending);
            command.Parameters.AddWithValue("kanbanColumnId", pendingColumn?.Id ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("taskId", taskId);
            command.Parameters.AddWithValue("userId", userId);

            return command.ExecuteNonQuery();
        }

        private bool SetStatusAndColumn(int subTaskId, int userId, Status targetStatus, int targetColumnId)
        {
            var task = GetById(subTaskId);
            if (task == null || task.UserId != userId)
            {
                return false;
            }

            var allSubTasks = GetAllSubTasks(task.TaskId, userId);

            //При move към активна или завършена колона dependency-то трябва вече да е завършено
            if ((targetStatus == Status.InProgress || targetStatus == Status.Completed) && task.BlockedBySubTaskId.HasValue)
            {
                var blocker = allSubTasks.FirstOrDefault(subTask => subTask.Id == task.BlockedBySubTaskId.Value);
                if (blocker == null || blocker.Status != Status.Completed)
                {
                    return false;
                }
            }

            using var connection = _databaseService.GetOpenConnection();
            using var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = @"
                UPDATE ""SubTasks""
                SET
                    ""Status"" = @status,
                    ""CompletedAt"" = @completedAt,
                    ""KanbanColumnId"" = @kanbanColumnId
                WHERE ""Id"" = @subTaskId
                  AND ""UserId"" = @userId;";

            updateCommand.Parameters.AddWithValue("status", (int)targetStatus);
            updateCommand.Parameters.AddWithValue("completedAt", targetStatus == Status.Completed ? DateTime.UtcNow : (object)DBNull.Value);
            updateCommand.Parameters.AddWithValue("kanbanColumnId", targetColumnId);
            updateCommand.Parameters.AddWithValue("subTaskId", subTaskId);
            updateCommand.Parameters.AddWithValue("userId", userId);

            bool updated = updateCommand.ExecuteNonQuery() > 0;
            if (!updated)
            {
                return false;
            }

            if (targetStatus != Status.Completed)
            {
                var pendingColumn = _kanbanColumnService.GetDefaultColumnForStatus(task.TaskId, userId, Status.Pending);
                ResetDependentSubTasks(connection, task.Id, userId, allSubTasks, pendingColumn?.Id);
            }

            return true;
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
                KanbanColumnId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                BlockedBySubTaskId = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                TaskId = reader.GetInt32(7),
                UserId = reader.GetInt32(8)
            };
        }

        private int? ResolveBlockedBySubTaskId(int subTaskId, int taskId, int userId, int? blockedBySubTaskId)
        {
            //Проверка дали има зададена зависимост
            if (!blockedBySubTaskId.HasValue || blockedBySubTaskId.Value <= 0)
            {
                return null;
            }

            //Проверка дали самата зависимост не сочи към себе си
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

            //Проверка дали създава безкраен цикъл
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

        //Проверява дали нова зависимост между подзадачи в create екрана създава цикъл по индексите им
        private static bool IsDependencyChanged(int? currentDependencyId, int? requestedDependencyId)
        {
            int? normalizedRequestedId = requestedDependencyId.HasValue && requestedDependencyId.Value > 0
                ? requestedDependencyId.Value
                : null;

            return currentDependencyId != normalizedRequestedId;
        }

        private static bool CreatesNewSubTaskCycle(int currentSubTaskIndex, int candidateDependencyIndex, List<CreateSubTaskInputModel> subTasks)
        {
            var visited = new HashSet<int>();
            int? nextIndex = candidateDependencyIndex;

            while (nextIndex.HasValue)
            {
                if (!visited.Add(nextIndex.Value))
                {
                    break;
                }

                if (nextIndex.Value == currentSubTaskIndex)
                {
                    return true;
                }

                nextIndex = subTasks[nextIndex.Value].BlockedByIndex;
            }

            return false;
        }

        private static void ResetDependentSubTasks(NpgsqlConnection connection, int rootSubTaskId, int userId, List<SubTaskItem> allSubTasks, int? pendingColumnId)
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
                    ""CompletedAt"" = NULL,
                    ""KanbanColumnId"" = @kanbanColumnId
                WHERE ""Id"" = ANY(@subTaskIds)
                  AND ""UserId"" = @userId;";

            resetCommand.Parameters.AddWithValue("status", (int)Status.Pending);
            resetCommand.Parameters.AddWithValue("kanbanColumnId", pendingColumnId.HasValue ? pendingColumnId.Value : (object)DBNull.Value);
            resetCommand.Parameters.AddWithValue("subTaskIds", descendantIds.ToArray());
            resetCommand.Parameters.AddWithValue("userId", userId);
            resetCommand.ExecuteNonQuery();
        }
    }
}
