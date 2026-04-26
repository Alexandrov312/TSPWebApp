using Npgsql;
using WebApp23621759.Database.Helpers;

namespace WebApp23621759.Database.DatabaseTables
{
    public class AppNotificationTableInitializer
    {
        public static void EnsureTable(NpgsqlConnection connection)
        {
            if (TableHelper.TableExists(connection, "AppNotifications"))
            {
                return;
            }

            using var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText = @"
                CREATE TABLE ""AppNotifications"" (
                    ""Id"" INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                    ""UserId"" INTEGER NOT NULL,
                    ""TaskId"" INTEGER NULL,
                    ""Type"" TEXT NOT NULL,
                    ""Title"" TEXT NOT NULL,
                    ""Message"" TEXT NOT NULL,
                    ""TargetUrl"" TEXT NULL,
                    ""IsRead"" BOOLEAN NOT NULL DEFAULT FALSE,
                    ""CreatedAt"" TIMESTAMP NOT NULL,
                    ""ReadAt"" TIMESTAMP NULL,
                    CONSTRAINT ""FK_AppNotifications_Users""
                        FOREIGN KEY (""UserId"")
                        REFERENCES ""Users""(""Id"")
                        ON DELETE CASCADE,
                    CONSTRAINT ""FK_AppNotifications_Tasks""
                        FOREIGN KEY (""TaskId"")
                        REFERENCES ""Tasks""(""Id"")
                        ON DELETE CASCADE
                );

                CREATE INDEX ""IX_AppNotifications_UserCreatedAt""
                    ON ""AppNotifications"" (""UserId"", ""CreatedAt"" DESC);
            ";
            createTableCommand.ExecuteNonQuery();
        }
    }
}
