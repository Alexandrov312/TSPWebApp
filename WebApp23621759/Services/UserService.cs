using Npgsql;
using WebApp23621759.Database;
using WebApp23621759.Models.Entities;

namespace WebApp23621759.Services
{
	public class UserService
	{
		private readonly DatabaseService _databaseService;
		public UserService(DatabaseService databaseService)
		{
			_databaseService = databaseService;
		}

		public User GetByUsername(string username)
		{
			using var connection = _databaseService.GetOpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = @"
				SELECT ""Id"", ""Username"", ""Email"", ""PasswordHash"", ""IsEmailConfirmed""
				FROM ""Users""
				WHERE ""Username"" = @input
				LIMIT 1;";
			//command.Parameters.Add(new Npgsql.NpgsqlParameter("@input", username));
			command.Parameters.AddWithValue("@input", username);

			using var reader = command.ExecuteReader();
			if (!reader.Read())
			{
				return null;
            }

            return MapUser(reader);
        }

		public User GetByEmail(string email)
		{
			using var connection = _databaseService.GetOpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = @"
				SELECT ""Id"", ""Username"", ""Email"", ""PasswordHash"", ""IsEmailConfirmed""
				FROM ""Users""
				WHERE ""Email"" = @input
				LIMIT 1;";
			//command.Parameters.Add(new Npgsql.NpgsqlParameter("@input", email));
            command.Parameters.AddWithValue("@input", email);

            using var reader = command.ExecuteReader();
			if (!reader.Read())
			{
				return null;
            }

            return MapUser(reader);
        }
		public User GetByUsernameOrEmail(string usernameOrEmail)
		{
			using var connection = _databaseService.GetOpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = @"
				SELECT ""Id"", ""Username"", ""Email"", ""PasswordHash"", ""IsEmailConfirmed""
				FROM ""Users""
				WHERE ""Username"" = @input OR ""Email"" = @input
				LIMIT 1;";

			//command.Parameters.Add(new Npgsql.NpgsqlParameter("@input", usernameOrEmail));
            command.Parameters.AddWithValue("@input", usernameOrEmail);

            using var reader = command.ExecuteReader();

			if (!reader.Read())
			{
				return null;
            }

            return MapUser(reader);
        }

		public bool UsernameExists(string username)
		{
			return GetByUsername(username) != null;
		}

		public bool EmailExists(string email)
		{
			return GetByEmail(email) != null;
		}

        public User CreateUser(string username, string email, string passwordHash)
        {
            try
            {
                using var connection = _databaseService.GetOpenConnection();
                using var command = connection.CreateCommand();

                command.CommandText = @"
				    INSERT INTO ""Users"" (""Username"", ""Email"", ""PasswordHash"", ""IsEmailConfirmed"")
				    VALUES (@username, @email, @passwordHash, @isEmailConfirmed)
				    RETURNING ""Id"", ""Username"", ""Email"", ""PasswordHash"", ""IsEmailConfirmed"";";

                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@email", email);
                command.Parameters.AddWithValue("@passwordHash", passwordHash);
                command.Parameters.AddWithValue("@isEmailConfirmed", false);

                using var reader = command.ExecuteReader();

                if (!reader.Read())
                {
                    return null;
                }

                return MapUser(reader);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                return null;
            }
        }

        public bool DeleteUser(int userId)
        {
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM ""Users""
                WHERE ""Id"" = @userId;";

            command.Parameters.AddWithValue("userId", userId);
            return command.ExecuteNonQuery() > 0;
        }

        public User? GetById(int userId)
        {
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT ""Id"", ""Username"", ""Email"", ""PasswordHash"", ""IsEmailConfirmed""
                FROM ""Users""
                WHERE ""Id"" = @userId
                LIMIT 1;";

            command.Parameters.AddWithValue("userId", userId);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return MapUser(reader);
        }

        public List<User> GetAllConfirmedUsers()
        {
            var users = new List<User>();

            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT ""Id"", ""Username"", ""Email"", ""PasswordHash"", ""IsEmailConfirmed""
                FROM ""Users""
                WHERE ""IsEmailConfirmed"" = TRUE;";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                users.Add(MapUser(reader));
            }

            return users;
        }

        public bool ConfirmEmail(int userId)
        {
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE ""Users""
                SET ""IsEmailConfirmed"" = TRUE
                WHERE ""Id"" = @userId;";

            command.Parameters.AddWithValue("userId", userId);
            return command.ExecuteNonQuery() > 0;
        }

        public bool UpdatePassword(int userId, string passwordHash)
        {
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE ""Users""
                SET ""PasswordHash"" = @passwordHash
                WHERE ""Id"" = @userId;";

            command.Parameters.AddWithValue("userId", userId);
            command.Parameters.AddWithValue("passwordHash", passwordHash);
            return command.ExecuteNonQuery() > 0;
        }

        public bool UpdateUsername(int userId, string username)
        {
            try
            {
                using var connection = _databaseService.GetOpenConnection();
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE ""Users""
                    SET ""Username"" = @username
                    WHERE ""Id"" = @userId;";

                command.Parameters.AddWithValue("userId", userId);
                command.Parameters.AddWithValue("username", username);
                return command.ExecuteNonQuery() > 0;
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                return false;
            }
        }

        public bool UpdateEmail(int userId, string email)
        {
            try
            {
                using var connection = _databaseService.GetOpenConnection();
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE ""Users""
                    SET ""Email"" = @email
                    WHERE ""Id"" = @userId;";

                command.Parameters.AddWithValue("userId", userId);
                command.Parameters.AddWithValue("email", email);
                return command.ExecuteNonQuery() > 0;
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                return false;
            }
        }

        //Преобразуване на данни от една структура/формат в друга
        //reader -> User
        private static User MapUser(NpgsqlDataReader reader)
		{
            return new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                Email = reader.GetString(2),
                PasswordHash = reader.GetString(3),
                IsEmailConfirmed = reader.GetBoolean(4)
            };
        }
    }
}
