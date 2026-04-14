using WebApp23621759.Enums;
using WebApp23621759.Models.Entities;
using WebApp23621759.Models.ViewModel.SubTasks;
using WebApp23621759.Models.ViewModel.Tasks;

namespace WebApp23621759.Helpers
{
    public static class TaskViewModelHelper
    {
        //Сглобява view model за главна задача заедно с подзадачите и progress информацията.
        public static MyTaskItemViewModel BuildTaskItemViewModel(TaskItem task, List<SubTaskItem> subTasks)
        {
            int totalSubTasks = subTasks.Count;
            int completedSubTasks = subTasks.Count(subTask => subTask.Status == Status.Completed);
            int inProgressSubTasks = subTasks.Count(subTask => subTask.Status == Status.InProgress);
            int completionPercentage = totalSubTasks == 0
                ? 0
                : (int)Math.Round((double)completedSubTasks * 100 / totalSubTasks);
            int projectedCompletionPercentage = totalSubTasks == 0
                ? 0
                : (int)Math.Round((double)(completedSubTasks + inProgressSubTasks) * 100 / totalSubTasks);
            Dictionary<int, int> depthBySubTaskId = SubTaskHelper.BuildSubTaskDepthMap(subTasks);
            List<SubTaskItem> orderedSubTasks = SubTaskHelper.OrderSubTasks(subTasks);

            return new MyTaskItemViewModel
            {
                Task = task,
                CompletionPercentage = completionPercentage,
                ProjectedCompletionPercentage = projectedCompletionPercentage,
                CompletedSubTaskCount = completedSubTasks,
                InProgressSubTaskCount = inProgressSubTasks,
                TotalSubTaskCount = totalSubTasks,
                SubTasks = orderedSubTasks.Select(subTask => new MyTaskSubTaskViewModel
                {
                    Id = subTask.Id,
                    TaskId = subTask.TaskId,
                    Depth = depthBySubTaskId.GetValueOrDefault(subTask.Id),
                    Title = subTask.Title,
                    Description = subTask.Description,
                    Status = subTask.Status,
                    CompletedAt = subTask.CompletedAt,
                    BlockedBySubTaskId = subTask.BlockedBySubTaskId,
                    BlockedByTitle = subTasks.FirstOrDefault(candidate => candidate.Id == subTask.BlockedBySubTaskId)?.Title,
                    DependencyOptions = SubTaskHelper.BuildDependencyOptions(subTask, subTasks)
                }).ToList()
            };
        }
    }
}
