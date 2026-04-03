using Npgsql;

namespace WebApp23621759.Database.Helpers
{
	public class TableHelper
	{
		public static bool TableExists(NpgsqlConnection connection, string tableName)
		{
			using var command = connection.CreateCommand();
			command.CommandText = @"
				SELECT EXISTS (
					SELECT 1
					FROM information_schema.tables
					WHERE table_schema = 'public'
					AND table_name = @tableName
				);";
			command.Parameters.Add(new NpgsqlParameter("@tableName", tableName));
			//Изпълнява заявката и връща само една стойност (първата колона от първия ред)
			//! -> премахва предупреждението на компилатора, като "казва", че резултата няма да е null
			return (bool)command.ExecuteScalar()!;
		}
	}
}
