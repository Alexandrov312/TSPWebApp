namespace WebApp23621759.Helpers
{
    public sealed record TaskSortRule(string Column, string Direction);

    public static class TaskSortHelper
    {
        public static readonly string[] MyTasksColumns = { "dueDate", "priority", "status", "createdAt" };
        public static readonly string[] ArchiveColumns = { "dueDate", "priority", "status", "completedAt" };

        //Парсва заявените sort правила и пази само позволените колони в подадения ред.
        public static List<TaskSortRule> ParseSortRules(string? sort, IEnumerable<string> allowedColumns)
        {
            var allowedColumnSet = new HashSet<string>(allowedColumns, StringComparer.OrdinalIgnoreCase);

            return (sort ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(rule => rule.Split(':', StringSplitOptions.TrimEntries))
                .Where(parts => parts.Length > 0 && allowedColumnSet.Contains(parts[0]))
                .Select(parts => new TaskSortRule(
                    parts[0],
                    parts.ElementAtOrDefault(1)?.Equals("DESC", StringComparison.OrdinalIgnoreCase) == true ? "DESC" : "ASC"))
                .GroupBy(rule => rule.Column, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
        }

        //Върти колоната в цикъл ASC -> DESC -> none, без да променя приоритета на останалите правила.
        public static string GetNextSort(string? sort, string column, IEnumerable<string> allowedColumns)
        {
            var rules = ParseSortRules(sort, allowedColumns);
            int index = rules.FindIndex(rule => rule.Column.Equals(column, StringComparison.OrdinalIgnoreCase));

            if (index < 0)
            {
                rules.Add(new TaskSortRule(column, "ASC"));
            }
            else if (rules[index].Direction == "ASC")
            {
                rules[index] = rules[index] with { Direction = "DESC" };
            }
            else
            {
                rules.RemoveAt(index);
            }

            return rules.Count == 0
                ? "none"
                : string.Join(",", rules.Select(rule => $"{rule.Column}:{rule.Direction}"));
        }

        public static string GetArrow(string? sort, string column, IEnumerable<string> allowedColumns)
        {
            var rule = ParseSortRules(sort, allowedColumns)
                .FirstOrDefault(rule => rule.Column.Equals(column, StringComparison.OrdinalIgnoreCase));

            if (rule == null)
            {
                return string.Empty;
            }

            return rule.Direction == "ASC" ? " ↑" : " ↓";
        }

        public static string GetSortPriority(string? sort, string column, IEnumerable<string> allowedColumns)
        {
            int index = ParseSortRules(sort, allowedColumns)
                .FindIndex(rule => rule.Column.Equals(column, StringComparison.OrdinalIgnoreCase));

            return index < 0 ? string.Empty : (index + 1).ToString();
        }
    }
}
