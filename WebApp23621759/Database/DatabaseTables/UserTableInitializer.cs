using Npgsql;
using WebApp23621759.Database.Helpers;
using WebApp23621759.Services;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

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
						""IsAdmin"" BOOLEAN NOT NULL
					);";
				createTableCommand.ExecuteNonQuery();

				InsertAdmin(connection);
			}
		}

		private static void InsertAdmin(NpgsqlConnection connection)
		{
			using NpgsqlCommand insertAdminCommand = connection.CreateCommand();
			insertAdminCommand.CommandText = @"
				INSERT INTO ""Users"" (""Username"", ""Email"", ""PasswordHash"", ""IsAdmin"")
				VALUES (@username, @email, @passwordHash, @isAdmin);";

			insertAdminCommand.Parameters.Add(new NpgsqlParameter("@username", "admin"));
			insertAdminCommand.Parameters.Add(new NpgsqlParameter("@email", "admin@site.com"));
			insertAdminCommand.Parameters.Add(new NpgsqlParameter("@passwordHash", PasswordService.Hash("admin123")));
			insertAdminCommand.Parameters.Add(new NpgsqlParameter("@isAdmin", true));

			insertAdminCommand.ExecuteNonQuery();
		}
	}
}
