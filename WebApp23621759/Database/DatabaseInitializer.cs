using Npgsql;
using WebApp23621759.Database.DatabaseTables;
using WebApp23621759.Services;

namespace WebApp23621759.Database
{
	public class DatabaseInitializer
	{
		public static void Initialize(DatabaseService databaseService)
		{
			using var connection = databaseService.GetOpenConnection();

			UserTableInitializer.EnsureTable(connection);
			TaskTableInitializer.EnsureTable(connection);
			KanbanColumnTableInitializer.EnsureTable(connection);
			SubTaskTableInitializer.EnsureTable(connection);
		}
	}
}
