using Npgsql;
using WebApp23621759.Database.Helpers;

namespace WebApp23621759.Database.DatabaseTables
{
    public class SubTaskTableInitializer
    {
        public static void EnsureTable(NpgsqlConnection connection)
        {
            if(!TableHelper.TableExists(connection, "SubTasks"))
            {
                using var createTableCommand = connection.CreateCommand();
                createTableCommand.CommandText = @"
                        CREATE TABLE ""SubTasks"" (
                            ""Id"" SERIAL PRIMARY KEY,
                            ""Title"" VARCHAR(150) NOT NULL,
                            ""Description"" TEXT,
                            ""Status"" INT NOT NULL,
                            ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW(),
                            ""CompletedAt"" TIMESTAMP NULL,
                            ""TaskId"" INT NOT NULL,
                            ""UserId"" INT NOT NULL,
                            ""BlockedBySubTaskId"" INT NULL,
                    
                            CONSTRAINT ""FK_SubTasks_Tasks""
                                FOREIGN KEY (""TaskId"")
                                REFERENCES ""Tasks""(""Id"")
                                ON DELETE CASCADE,
                    
                            CONSTRAINT ""FK_SubTasks_Users""
                                FOREIGN KEY (""UserId"")
                                REFERENCES ""Users""(""Id"")
                                ON DELETE CASCADE,
                    
                            CONSTRAINT ""FK_SubTasks_BlockedBy""
                                FOREIGN KEY (""BlockedBySubTaskId"")
                                REFERENCES ""SubTasks""(""Id"")
                                ON DELETE SET NULL
                        );
                    ";
                createTableCommand.ExecuteNonQuery();
            }
        }
    }
}
