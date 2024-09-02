using System;
using System.Threading.Tasks;
using Npgsql;
using Dapper;

public class DatabaseService
{
	private readonly string _connectionString;

	public DatabaseService()
	{
		// Строка подключения к локальной базе данных PostgreSQL
		_connectionString = "Host=localhost;Port=5432;Username=postgres;Password=1234;Database=my_database";
	}

	// Метод для проверки учетных данных студента
	public async Task<bool> ValidateStudentAsync(long uin, string password)
	{
		using (var connection = new NpgsqlConnection(_connectionString))
		{
			string query = @"
                SELECT COUNT(1)
                FROM users u
                INNER JOIN roles r ON u.role_id = r.role_id
                WHERE u.uin = @UIN AND u.password_hash = @Password AND r.role_name = 'student'";

			var parameters = new { UIN = uin, Password = password };
			return await connection.ExecuteScalarAsync<bool>(query, parameters);
		}
	}

	// Метод для проверки учетных данных преподавателя
	public async Task<bool> ValidateTeacherAsync(long uin, string password)
	{
		using (var connection = new NpgsqlConnection(_connectionString))
		{
			string query = @"
                SELECT COUNT(1)
                FROM users u
                INNER JOIN roles r ON u.role_id = r.role_id
                WHERE u.uin = @UIN AND u.password_hash = @Password AND r.role_name = 'teacher'";

			var parameters = new { UIN = uin, Password = password };
			return await connection.ExecuteScalarAsync<bool>(query, parameters);
		}
	}
}
