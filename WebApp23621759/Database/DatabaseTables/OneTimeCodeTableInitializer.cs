using Npgsql;
using WebApp23621759.Database.Helpers;

namespace WebApp23621759.Database.DatabaseTables
{
    public class OneTimeCodeTableInitializer
    {
        public static void EnsureTable(NpgsqlConnection connection)
        {
            if (TableHelper.TableExists(connection, "OneTimeCodes"))
            {
                return;
            }

            using var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText = @"
                CREATE TABLE ""OneTimeCodes"" (
                    ""Id"" INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                    ""UserId"" INTEGER NOT NULL,
                    ""Email"" TEXT NOT NULL,
                    ""CodeHash"" TEXT NOT NULL,
                    ""Purpose"" TEXT NOT NULL,
                    ""CreatedAt"" TIMESTAMP NOT NULL,
                    ""ExpiresAt"" TIMESTAMP NOT NULL,
                    ""IsUsed"" BOOLEAN NOT NULL DEFAULT FALSE,
                    ""UsedAt"" TIMESTAMP NULL,
                    CONSTRAINT ""FK_OneTimeCodes_Users""
                        FOREIGN KEY (""UserId"")
                        REFERENCES ""Users""(""Id"")
                        ON DELETE CASCADE
                );

                CREATE INDEX ""IX_OneTimeCodes_UserPurpose""
                    ON ""OneTimeCodes"" (""UserId"", ""Purpose"");
            ";
            createTableCommand.ExecuteNonQuery();
        }
    }
}
