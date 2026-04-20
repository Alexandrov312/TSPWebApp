using Npgsql;
using WebApp23621759.Database.Helpers;

namespace WebApp23621759.Database.DatabaseTables
{
    public class KanbanColumnTableInitializer
    {
        public static void EnsureTable(NpgsqlConnection connection)
        {
            if (!TableHelper.TableExists(connection, "KanbanColumns"))
            {
                using var createTableCommand = connection.CreateCommand();
                createTableCommand.CommandText = @"
                    CREATE TABLE ""KanbanColumns"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""Name"" VARCHAR(80) NOT NULL,
                        ""DisplayOrder"" INT NOT NULL,
                        ""IsCompletedColumn"" BOOLEAN NOT NULL DEFAULT FALSE,
                        ""IsDefaultColumn"" BOOLEAN NOT NULL DEFAULT FALSE,
                        ""StatusValue"" INT NOT NULL,
                        ""TaskId"" INT NOT NULL,
                        ""UserId"" INT NOT NULL,

                        CONSTRAINT ""FK_KanbanColumns_Tasks""
                            FOREIGN KEY (""TaskId"")
                            REFERENCES ""Tasks""(""Id"")
                            ON DELETE CASCADE,

                        CONSTRAINT ""FK_KanbanColumns_Users""
                            FOREIGN KEY (""UserId"")
                            REFERENCES ""Users""(""Id"")
                            ON DELETE CASCADE
                    );";
                createTableCommand.ExecuteNonQuery();
            }
        }
    }
}
