using WebApp23621759.Models.Entities;
using WebApp23621759.Models.ViewModel.SubTasks;

namespace WebApp23621759.Helpers
{
    public class SubTaskHelper
    {
        public static bool CreatesCycle(int subTaskId, int dependencyId, List<SubTaskItem> subTasks)
        {
            Dictionary<int, int?> dependencyBySubTaskId = subTasks.ToDictionary(
                subTask => subTask.Id,
                subTask => subTask.Id == subTaskId ? dependencyId : subTask.BlockedBySubTaskId);

            int? currentDependencyId = dependencyId;
            HashSet<int> visitedIds = new();

            while (currentDependencyId.HasValue)
            {
                if (!visitedIds.Add(currentDependencyId.Value))
                {
                    return true;
                }

                if (currentDependencyId.Value == subTaskId)
                {
                    return true;
                }

                if (!dependencyBySubTaskId.TryGetValue(currentDependencyId.Value, out int? nextDependencyId))
                {
                    return false;
                }

                currentDependencyId = nextDependencyId;
            }

            return false;
        }

        public static Dictionary<int, int> BuildSubTaskDepthMap(List<SubTaskItem> subTasks)
        {
            Dictionary<int, SubTaskItem> subTasksById = subTasks.ToDictionary(subTask => subTask.Id);
            Dictionary<int, int> depthBySubTaskId = new();

            int GetDepth(SubTaskItem subTask, HashSet<int> chain)
            {
                if (depthBySubTaskId.TryGetValue(subTask.Id, out int cachedDepth))
                {
                    return cachedDepth;
                }

                if (!subTask.BlockedBySubTaskId.HasValue
                    || !subTasksById.TryGetValue(subTask.BlockedBySubTaskId.Value, out SubTaskItem? parentSubTask)
                    || !chain.Add(subTask.Id))
                {
                    depthBySubTaskId[subTask.Id] = 0;
                    return 0;
                }

                int depth = GetDepth(parentSubTask, chain) + 1;
                chain.Remove(subTask.Id);
                depthBySubTaskId[subTask.Id] = depth;
                return depth;
            }

            foreach (SubTaskItem subTask in subTasks)
            {
                GetDepth(subTask, new HashSet<int>());
            }

            return depthBySubTaskId;
        }

        public static List<SubTaskItem> OrderSubTasks(List<SubTaskItem> subTasks)
        {
            HashSet<int> existingSubTaskIds = subTasks.Select(subTask => subTask.Id).ToHashSet();
            List<SubTaskItem> rootSubTasks = subTasks
                .Where(subTask => !subTask.BlockedBySubTaskId.HasValue || !existingSubTaskIds.Contains(subTask.BlockedBySubTaskId.Value))
                .OrderBy(subTask => subTask.Id)
                .ToList();
            Dictionary<int, List<SubTaskItem>> childrenByParentId = subTasks
                .Where(subTask => subTask.BlockedBySubTaskId.HasValue && existingSubTaskIds.Contains(subTask.BlockedBySubTaskId.Value))
                .GroupBy(subTask => subTask.BlockedBySubTaskId!.Value)
                .ToDictionary(group => group.Key, group => group.OrderBy(subTask => subTask.Id).ToList());

            List<SubTaskItem> orderedSubTasks = new();
            HashSet<int> visitedSubTaskIds = new();

            void AppendSubTask(SubTaskItem subTask)
            {
                if (!visitedSubTaskIds.Add(subTask.Id))
                {
                    return;
                }

                orderedSubTasks.Add(subTask);

                if (!childrenByParentId.TryGetValue(subTask.Id, out List<SubTaskItem>? childSubTasks))
                {
                    return;
                }

                foreach (SubTaskItem childSubTask in childSubTasks)
                {
                    AppendSubTask(childSubTask);
                }
            }

            foreach (SubTaskItem rootSubTask in rootSubTasks)
            {
                AppendSubTask(rootSubTask);
            }

            foreach (SubTaskItem subTask in subTasks.OrderBy(subTask => subTask.Id))
            {
                if (visitedSubTaskIds.Add(subTask.Id))
                {
                    orderedSubTasks.Add(subTask);
                }
            }

            return orderedSubTasks;
        }

        public static List<SubTaskDependencyOptionViewModel> BuildDependencyOptions(SubTaskItem currentSubTask, List<SubTaskItem> subTasks)
        {
            return subTasks
                .Where(subTask => subTask.Id != currentSubTask.Id && !SubTaskHelper.CreatesCycle(currentSubTask.Id, subTask.Id, subTasks))
                .Select(subTask => new SubTaskDependencyOptionViewModel
                {
                    Id = subTask.Id,
                    Title = subTask.Title
                })
                .ToList();
        }
    }
}

