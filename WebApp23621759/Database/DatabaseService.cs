using Npgsql;

namespace WebApp23621759.Database
{
	public class DatabaseService
	{
		private readonly string _connectionString;
		public DatabaseService(IConfiguration configuration)
		{
			_connectionString = configuration.GetConnectionString("DefaultConnection");
		}
		public NpgsqlConnection GetOpenConnection()
		{
			var connection = new NpgsqlConnection(_connectionString);
			connection.Open();
			return connection;
		}
	}
}
