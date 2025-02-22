using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using Dapper;

public class DatabaseService
{
	private readonly string _connectionString;

		public DatabaseService()
		{
        // Используем правильную строку подключения

        _connectionString = "Host=10.250.0.64;Port=5432;Username=postgres;Password=postgres;Database=attendance;";
    }

		public async Task<bool> ValidateStudentAsync(string uin, string password)
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
		try
		{
			using (var connection = new NpgsqlConnection(_connectionString))
			{
				await connection.OpenAsync();

				// Хэшируем введенный пароль для проверки
				string hashedPassword = HashPassword(password);

				string query = @"
					SELECT COUNT(1)
					FROM users u
					INNER JOIN roles r ON u.role_id = r.role_id
					WHERE u.uin = @UIN AND u.password_hash = @Password AND r.role_name = 'teacher'";

				var parameters = new { UIN = uin, Password = hashedPassword };
				return await connection.ExecuteScalarAsync<bool>(query, parameters);
			}
		}
		catch (Exception ex)
		{
			// Логируем ошибку для отладки
			Console.WriteLine($"Ошибка при проверке учетных данных преподавателя: {ex.Message}");
			return false;
		}
	}

	// Метод для хэширования пароля с использованием SHA-256
	private string HashPassword(string password)
	{
		using (var sha256 = SHA256.Create())
		{
			// Преобразуем строку пароля в байты, затем хэшируем и преобразуем в строку Base64
			var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
			return Convert.ToBase64String(hashedBytes);
		}
	}
}
