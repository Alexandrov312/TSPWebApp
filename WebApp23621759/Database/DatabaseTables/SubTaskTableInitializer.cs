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
                            ""CompletedAt"" TIMESTAMP NULL,
                            ""KanbanColumnId"" INT NULL,
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

                            CONSTRAINT ""FK_SubTasks_KanbanColumns""
                                FOREIGN KEY (""KanbanColumnId"")
                                REFERENCES ""KanbanColumns""(""Id"")
                                ON DELETE SET NULL,
                    
                            CONSTRAINT ""FK_SubTasks_BlockedBy""
                                FOREIGN KEY (""BlockedBySubTaskId"")
                                REFERENCES ""SubTasks""(""Id"")
                                ON DELETE SET NULL
                        );
                    ";
                createTableCommand.ExecuteNonQuery();
            }

            using var dropCreatedAtColumnCommand = connection.CreateCommand();
            dropCreatedAtColumnCommand.CommandText = @"
                ALTER TABLE ""SubTasks""
                DROP COLUMN IF EXISTS ""CreatedAt"";";
            dropCreatedAtColumnCommand.ExecuteNonQuery();

            using var addKanbanColumnIdCommand = connection.CreateCommand();
            addKanbanColumnIdCommand.CommandText = @"
                ALTER TABLE ""SubTasks""
                ADD COLUMN IF NOT EXISTS ""KanbanColumnId"" INT NULL;";
            addKanbanColumnIdCommand.ExecuteNonQuery();

            using var addKanbanColumnForeignKeyCommand = connection.CreateCommand();
            addKanbanColumnForeignKeyCommand.CommandText = @"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_SubTasks_KanbanColumns'
                    ) THEN
                        ALTER TABLE ""SubTasks""
                        ADD CONSTRAINT ""FK_SubTasks_KanbanColumns""
                        FOREIGN KEY (""KanbanColumnId"")
                        REFERENCES ""KanbanColumns""(""Id"")
                        ON DELETE SET NULL;
                    END IF;
                END $$;";
            addKanbanColumnForeignKeyCommand.ExecuteNonQuery();
        }
    }
}
