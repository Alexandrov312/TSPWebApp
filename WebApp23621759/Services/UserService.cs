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
				SELECT ""Id"", ""Username"", ""Email"", ""PasswordHash"", ""IsAdmin""
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
				SELECT ""Id"", ""Username"", ""Email"", ""PasswordHash"", ""IsAdmin""
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
				SELECT ""Id"", ""Username"", ""Email"", ""PasswordHash"", ""IsAdmin""
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

        public User CreateUser(string username, string email, string passwordHash, bool isAdmin)
        {
            using var connection = _databaseService.GetOpenConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"
				INSERT INTO ""Users"" (""Username"", ""Email"", ""PasswordHash"", ""IsAdmin"")
				VALUES (@username, @email, @passwordHash, @isAdmin)
				RETURNING ""Id"", ""Username"", ""Email"", ""PasswordHash"", ""IsAdmin"";";

            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@email", email);
            command.Parameters.AddWithValue("@passwordHash", passwordHash);
            command.Parameters.AddWithValue("@isAdmin", isAdmin);

            using var reader = command.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            return MapUser(reader);
        }

		//reader -> User
		private static User MapUser(NpgsqlDataReader reader)
		{
            return new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                Email = reader.GetString(2),
                PasswordHash = reader.GetString(3),
                IsAdmin = reader.GetBoolean(4)
            };
        }
    }
}
