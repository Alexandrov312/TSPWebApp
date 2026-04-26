using Npgsql;
using WebApp23621759.Database.Helpers;

namespace WebApp23621759.Database.DatabaseTables
{
	public class UserTableInitializer
	{
		public static void EnsureTable(NpgsqlConnection connection)
		{
			if (!TableHelper.TableExists(connection, "Users"))
			{
				using var createTableCommand = connection.CreateCommand();
				createTableCommand.CommandText = @"
					CREATE TABLE ""Users"" (
						""Id"" INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
						""Username"" TEXT NOT NULL UNIQUE,
						""Email"" TEXT NOT NULL UNIQUE,
						""PasswordHash"" TEXT NOT NULL,
						""IsEmailConfirmed"" BOOLEAN NOT NULL DEFAULT FALSE
					);";
				createTableCommand.ExecuteNonQuery();
                return;
			}
		}
	}
}
