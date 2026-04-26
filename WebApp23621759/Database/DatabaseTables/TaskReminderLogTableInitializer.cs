using Npgsql;
using WebApp23621759.Database.Helpers;

namespace WebApp23621759.Database.DatabaseTables
{
    public class TaskReminderLogTableInitializer
    {
        public static void EnsureTable(NpgsqlConnection connection)
        {
            if (TableHelper.TableExists(connection, "TaskReminderLogs"))
            {
                return;
            }

            using var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText = @"
                CREATE TABLE ""TaskReminderLogs"" (
                    ""Id"" INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                    ""UserId"" INTEGER NOT NULL,
                    ""TaskId"" INTEGER NOT NULL,
                    ""ReminderType"" TEXT NOT NULL,
                    ""ReferenceDueDate"" TIMESTAMP NOT NULL,
                    ""SentAt"" TIMESTAMP NOT NULL,
                    CONSTRAINT ""FK_TaskReminderLogs_Users""
                        FOREIGN KEY (""UserId"")
                        REFERENCES ""Users""(""Id"")
                        ON DELETE CASCADE,
                    CONSTRAINT ""FK_TaskReminderLogs_Tasks""
                        FOREIGN KEY (""TaskId"")
                        REFERENCES ""Tasks""(""Id"")
                        ON DELETE CASCADE
                );

                CREATE UNIQUE INDEX ""IX_TaskReminderLogs_Dedup""
                    ON ""TaskReminderLogs"" (""TaskId"", ""ReminderType"", ""ReferenceDueDate"");
            ";
            createTableCommand.ExecuteNonQuery();
        }
    }
}
