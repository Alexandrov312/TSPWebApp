using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Npgsql;
using WebApp23621759.Database;
using WebApp23621759.Models.Entities;
using WebApp23621759.Models.Settings;

namespace WebApp23621759.Services
{
    public class OneTimeCodeService
    {
        public const string EmailVerificationPurpose = "EmailVerification";
        public const string EmailChangePurpose = "EmailChange";
        public const string PasswordResetPurpose = "PasswordReset";

        private readonly DatabaseService _databaseService;
        private readonly ReminderSettings _reminderSettings;

        public OneTimeCodeService(DatabaseService databaseService, IOptions<ReminderSettings> reminderOptions)
        {
            _databaseService = databaseService;
            _reminderSettings = reminderOptions.Value;
        }

        //Генерира нов 6-цифрен код и инвалидира старите активни кодове от същия тип.
        public string CreateCode(int userId, string email, string purpose)
        {
            DeleteExpiredCodesOnly();

            int expirationMinutes = purpose == PasswordResetPurpose
                ? _reminderSettings.PasswordResetCodeMinutes
                : _reminderSettings.EmailVerificationCodeMinutes;

            string code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            string codeHash = HashCode(code);

            using var connection = _databaseService.GetOpenConnection();
            using var invalidateCommand = connection.CreateCommand();
            invalidateCommand.CommandText = @"
                UPDATE ""OneTimeCodes""
                SET ""IsUsed"" = TRUE, ""UsedAt"" = NOW()
                WHERE ""UserId"" = @userId
                  AND ""Purpose"" = @purpose
                  AND ""IsUsed"" = FALSE;";
            invalidateCommand.Parameters.AddWithValue("userId", userId);
            invalidateCommand.Parameters.AddWithValue("purpose", purpose);
            invalidateCommand.ExecuteNonQuery();

            using var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT INTO ""OneTimeCodes""
                    (""UserId"", ""Email"", ""CodeHash"", ""Purpose"", ""CreatedAt"", ""ExpiresAt"", ""IsUsed"", ""UsedAt"")
                VALUES
                    (@userId, @email, @codeHash, @purpose, @createdAt, @expiresAt, FALSE, NULL);";

            DateTime createdAt = DateTime.UtcNow;
            insertCommand.Parameters.AddWithValue("userId", userId);
            insertCommand.Parameters.AddWithValue("email", email);
            insertCommand.Parameters.AddWithValue("codeHash", codeHash);
            insertCommand.Parameters.AddWithValue("purpose", purpose);
            insertCommand.Parameters.AddWithValue("createdAt", createdAt);
            insertCommand.Parameters.AddWithValue("expiresAt", createdAt.AddMinutes(expirationMinutes));
            insertCommand.ExecuteNonQuery();

            return code;
        }

        public bool ValidateCode(int userId, string purpose, string code)
        {
            return ValidateCodeAndGetEmail(userId, purpose, code) != null;
        }

        //Освен че валидира кода, методът връща и email-а, за който е издаден кодът.
        public string? ValidateCodeAndGetEmail(int userId, string purpose, string code)
        {
            DeleteExpiredCodes();

            using var connection = _databaseService.GetOpenConnection();
            using var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = @"
                SELECT ""Id"", ""Email"", ""CodeHash"", ""ExpiresAt"", ""IsUsed""
                FROM ""OneTimeCodes""
                WHERE ""UserId"" = @userId
                  AND ""Purpose"" = @purpose
                ORDER BY ""CreatedAt"" DESC
                LIMIT 1;";

            selectCommand.Parameters.AddWithValue("userId", userId);
            selectCommand.Parameters.AddWithValue("purpose", purpose);

            using var reader = selectCommand.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            int codeId = reader.GetInt32(0);
            string email = reader.GetString(1);
            string storedHash = reader.GetString(2);
            DateTime expiresAt = reader.GetDateTime(3);
            bool isUsed = reader.GetBoolean(4);

            reader.Close();

            if (isUsed || expiresAt < DateTime.UtcNow || storedHash != HashCode(code))
            {
                return null;
            }

            //Маркираме кода като използван веднага след успешна проверка, за да няма повторна употреба.
            using var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = @"
                UPDATE ""OneTimeCodes""
                SET ""IsUsed"" = TRUE, ""UsedAt"" = NOW()
                WHERE ""Id"" = @id;";
            updateCommand.Parameters.AddWithValue("id", codeId);
            updateCommand.ExecuteNonQuery();

            return email;
        }

        public OneTimeCodeItem? GetLatestActiveCode(int userId, string purpose)
        {
            DeleteExpiredCodes();

            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT ""Id"", ""UserId"", ""Email"", ""CodeHash"", ""Purpose"", ""CreatedAt"", ""ExpiresAt"", ""IsUsed"", ""UsedAt""
                FROM ""OneTimeCodes""
                WHERE ""UserId"" = @userId
                  AND ""Purpose"" = @purpose
                  AND ""IsUsed"" = FALSE
                ORDER BY ""CreatedAt"" DESC
                LIMIT 1;";

            command.Parameters.AddWithValue("userId", userId);
            command.Parameters.AddWithValue("purpose", purpose);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return MapCode(reader);
        }

        //Трие изтеклите временни кодове
        public int DeleteExpiredCodes()
        {
            using var connection = _databaseService.GetOpenConnection();
            using var deleteCodesCommand = connection.CreateCommand();
            DateTime now = DateTime.UtcNow;
            deleteCodesCommand.CommandText = @"
                DELETE FROM ""OneTimeCodes""
                WHERE ""ExpiresAt"" <= @now;";
            deleteCodesCommand.Parameters.AddWithValue("now", now);
            int deletedCodes = deleteCodesCommand.ExecuteNonQuery();

            //Ако акаунтът не е потвърден и вече няма активен код за верификация,
            //премахваме и самия акаунт, за да не остават висящи регистрации.
            using var deleteUsersCommand = connection.CreateCommand();
            deleteUsersCommand.CommandText = @"
                DELETE FROM ""Users"" u
                WHERE u.""IsEmailConfirmed"" = FALSE
                  AND NOT EXISTS (
                      SELECT 1
                      FROM ""OneTimeCodes"" c
                      WHERE c.""UserId"" = u.""Id""
                        AND c.""Purpose"" = @emailVerificationPurpose
                        AND c.""IsUsed"" = FALSE
                        AND c.""ExpiresAt"" > @now
                  );";
            deleteUsersCommand.Parameters.AddWithValue("emailVerificationPurpose", EmailVerificationPurpose);
            deleteUsersCommand.Parameters.AddWithValue("now", now);
            int deletedUsers = deleteUsersCommand.ExecuteNonQuery();

            return deletedCodes + deletedUsers;
        }

        //При създаване на нов код чистим само изтеклите кодове,
        //за да не изтрием потребителя точно преди повторно изпращане.
        private int DeleteExpiredCodesOnly()
        {
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM ""OneTimeCodes""
                WHERE ""ExpiresAt"" <= @now;";

            command.Parameters.AddWithValue("now", DateTime.UtcNow);
            return command.ExecuteNonQuery();
        }

        private static string HashCode(string code)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
            return Convert.ToHexString(bytes);
        }

        private static OneTimeCodeItem MapCode(NpgsqlDataReader reader)
        {
            return new OneTimeCodeItem
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                Email = reader.GetString(2),
                CodeHash = reader.GetString(3),
                Purpose = reader.GetString(4),
                CreatedAt = reader.GetDateTime(5),
                ExpiresAt = reader.GetDateTime(6),
                IsUsed = reader.GetBoolean(7),
                UsedAt = reader.IsDBNull(8) ? null : reader.GetDateTime(8)
            };
        }
    }
}
