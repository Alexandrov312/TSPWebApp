using WebApp23621759.Models.Entities;
using WebApp23621759.Models.ViewModel.SubTasks;

namespace WebApp23621759.Helpers
{
    public class SubTaskHelper
    {
        //Проверява дали задаването на dependency ще създаде цикъл между подзадачите
        public static bool CreatesCycle(int subTaskId, int dependencyId, List<SubTaskItem> subTasks)
        {
            //Строи речник: за всяка подзадача пази от коя подзадача зависи
            //За текущата подзадача използва новия dependencyId, който се опитваме да зададем
            Dictionary<int, int?> dependencyBySubTaskId = subTasks.ToDictionary(
                subTask => subTask.Id,
                subTask => subTask.Id == subTaskId ? dependencyId : subTask.BlockedBySubTaskId);

            int? currentDependencyId = dependencyId;
            HashSet<int> visitedIds = new();

            //Обхожда веригата от зависимости нагоре
            while (currentDependencyId.HasValue)
            {
                //Ако стигнем до вече посетена подзадача, има цикъл
                if (!visitedIds.Add(currentDependencyId.Value))
                {
                    return true;
                }

                //Ако по веригата се върнем до самата текуща подзадача, също има цикъл
                if (currentDependencyId.Value == subTaskId)
                {
                    return true;
                }

                //Ако следващата dependency подзадача не съществува в списъка, веригата свършва
                if (!dependencyBySubTaskId.TryGetValue(currentDependencyId.Value, out int? nextDependencyId))
                {
                    return false;
                }

                currentDependencyId = nextDependencyId;
            }

            return false;
        }

        //Строи речник с дълбочината на всяка подзадача спрямо нейните зависимости
        public static Dictionary<int, int> BuildSubTaskDepthMap(List<SubTaskItem> subTasks)
        {
            Dictionary<int, SubTaskItem> subTasksById = subTasks.ToDictionary(subTask => subTask.Id);
            Dictionary<int, int> depthBySubTaskId = new();

            int GetDepth(SubTaskItem subTask, HashSet<int> chain)
            {
                //Ако дълбочината вече е сметната, използва кешираната стойност
                if (depthBySubTaskId.TryGetValue(subTask.Id, out int cachedDepth))
                {
                    return cachedDepth;
                }

                //Ако няма dependency, parent не съществува, или се засече цикъл,
                //приема тази подзадача за корен с дълбочина 0
                if (!subTask.BlockedBySubTaskId.HasValue
                    || !subTasksById.TryGetValue(subTask.BlockedBySubTaskId.Value, out SubTaskItem? parentSubTask)
                    || !chain.Add(subTask.Id))
                {
                    depthBySubTaskId[subTask.Id] = 0;
                    return 0;
                }

                //Дълбочината е дълбочината на родителя + 1
                int depth = GetDepth(parentSubTask, chain) + 1;

                //Маха текущата подзадача от текущата верига след връщане от рекурсията
                chain.Remove(subTask.Id);

                depthBySubTaskId[subTask.Id] = depth;
                return depth;
            }

            //Пресмята дълбочина за всяка подзадача
            foreach (SubTaskItem subTask in subTasks)
            {
                GetDepth(subTask, new HashSet<int>());
            }

            return depthBySubTaskId;
        }

        //Подрежда подзадачите така, че родителят да стои преди децата си
        public static List<SubTaskItem> OrderSubTasks(List<SubTaskItem> subTasks)
        {
            HashSet<int> existingSubTaskIds = subTasks.Select(subTask => subTask.Id).ToHashSet();

            //Root са подзадачите без dependency или с dependency към несъществуваща подзадача
            List<SubTaskItem> rootSubTasks = subTasks
                .Where(subTask => !subTask.BlockedBySubTaskId.HasValue || !existingSubTaskIds.Contains(subTask.BlockedBySubTaskId.Value))
                .OrderBy(subTask => subTask.Id)
                .ToList();

            //Групира децата по id на родителя, за да може лесно да се обхожда дървото
            Dictionary<int, List<SubTaskItem>> childrenByParentId = subTasks
                .Where(subTask => subTask.BlockedBySubTaskId.HasValue && existingSubTaskIds.Contains(subTask.BlockedBySubTaskId.Value))
                .GroupBy(subTask => subTask.BlockedBySubTaskId!.Value)
                .ToDictionary(group => group.Key, group => group.OrderBy(subTask => subTask.Id).ToList());

            List<SubTaskItem> orderedSubTasks = new();
            HashSet<int> visitedSubTaskIds = new();

            void AppendSubTask(SubTaskItem subTask)
            {
                //Предпазва от повторно добавяне и безкрайна рекурсия при невалидни данни
                if (!visitedSubTaskIds.Add(subTask.Id))
                {
                    return;
                }

                orderedSubTasks.Add(subTask);

                if (!childrenByParentId.TryGetValue(subTask.Id, out List<SubTaskItem>? childSubTasks))
                {
                    return;
                }

                //Добавя децата непосредствено след родителя
                foreach (SubTaskItem childSubTask in childSubTasks)
                {
                    AppendSubTask(childSubTask);
                }
            }

            //Първо подрежда всички root подзадачи и техните деца
            foreach (SubTaskItem rootSubTask in rootSubTasks)
            {
                AppendSubTask(rootSubTask);
            }

            //После добавя всички останали, които не са били обходени
            //Това покрива случаи с повредени или циклични зависимости
            foreach (SubTaskItem subTask in subTasks.OrderBy(subTask => subTask.Id))
            {
                if (visitedSubTaskIds.Add(subTask.Id))
                {
                    orderedSubTasks.Add(subTask);
                }
            }

            return orderedSubTasks;
        }

        //Строи списък с позволените dependency опции за конкретна подзадача
        public static List<SubTaskDependencyOptionViewModel> BuildDependencyOptions(SubTaskItem currentSubTask, List<SubTaskItem> subTasks)
        {
            return subTasks
                //Не позволява подзадача да зависи от себе си и пропуска dependency, които биха създали цикъл
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