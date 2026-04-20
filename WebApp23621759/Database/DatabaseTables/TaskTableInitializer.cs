using Npgsql;
using WebApp23621759.Database.Helpers;

namespace WebApp23621759.Database.DatabaseTables
{
    public class TaskTableInitializer
    {
        public static void EnsureTable (NpgsqlConnection connection)
        {
            if(!TableHelper.TableExists(connection, "Tasks"))
            {
                using var createTableCommand = connection.CreateCommand ();
                createTableCommand.CommandText = @"
					CREATE TABLE ""Tasks"" (
						""Id"" INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
						""Title"" TEXT NOT NULL,
						""Description"" TEXT NOT NULL,
						""DueDate"" TIMESTAMP NOT NULL,
						""CreatedAt"" TIMESTAMP NOT NULL,
						""CompletedAt"" TIMESTAMP,
						""Status"" INTEGER NOT NULL,
						""Priority"" INTEGER NOT NULL,
						""IsArchived"" BOOLEAN NOT NULL DEFAULT FALSE,
						""UserId"" INTEGER NOT NULL,
                        CONSTRAINT fk_tasks_users FOREIGN KEY (""UserId"") REFERENCES ""Users""(""Id"")
					);";
                createTableCommand.ExecuteNonQuery();
            }

            using var archiveColumnCommand = connection.CreateCommand();
            archiveColumnCommand.CommandText = @"
                ALTER TABLE ""Tasks""
                ADD COLUMN IF NOT EXISTS ""IsArchived"" BOOLEAN NOT NULL DEFAULT FALSE;";
            archiveColumnCommand.ExecuteNonQuery();
        }
    }
}
