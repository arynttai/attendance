using System;
using System.Threading.Tasks;
using Npgsql;
using Dapper;
using System.Text;
using System.Security.Cryptography;

namespace AC
{
	public class UserService
	{
		private readonly string _connectionString;

		public UserService()
		{
			_connectionString = "Host=localhost;Port=5433;Username=postgres;Password=1234;Database=my_database";
		}

		public async Task<User> GetUserByUINAsync(string uin)
		{
			using (var connection = new NpgsqlConnection(_connectionString))
			{
				await connection.OpenAsync();

				string query = @"
            SELECT role AS Role, last_name AS LastName, first_name AS FirstName, patronymic AS Patronymic, uin, email, phone_number AS PhoneNumber, id_card AS IdCard, Password
            FROM users
            WHERE CAST(uin AS TEXT) = @UIN";

				Console.WriteLine($"Executing query: {query}");
				Console.WriteLine($"With parameter: UIN = {uin}");

				var user = await connection.QueryFirstOrDefaultAsync<User>(query, new { UIN = uin });

				if (user == null)
				{
					Console.WriteLine("Пользователь не найден.");
					throw new Exception("Пользователь не найден.");
				}

				Console.WriteLine($"Пользователь найден: {user.LastName} {user.FirstName}");
				return user;
			}
		}


		public bool VerifyPassword(string enteredPassword, string storedPassword)
		{
			return enteredPassword == storedPassword;
		}

		public static string HashPassword(string password)
		{
			try
			{
				using (var sha256 = SHA256.Create())
				{
					var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
					return Convert.ToBase64String(hashedBytes);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Ошибка при хэшировании пароля: {ex.Message}");
				throw new Exception("Произошла ошибка при обработке пароля.");
			}
		}

		public async Task UpdateUserAsync(User updatedUser)
		{
			using (var connection = new NpgsqlConnection(_connectionString))
			{
				await connection.OpenAsync();

				string query = @"
                    UPDATE users
                    SET email = @Email, phone_number = @PhoneNumber
                    WHERE CAST(uin AS TEXT) = @UIN";

				var parameters = new
				{
					Email = updatedUser.Email,
					PhoneNumber = updatedUser.PhoneNumber,
					UIN = updatedUser.UIN
				};

				Console.WriteLine($"Executing update query: {query}");
				Console.WriteLine($"With parameters: Email = {updatedUser.Email}, PhoneNumber = {updatedUser.PhoneNumber}, UIN = {updatedUser.UIN}");

				int rowsAffected = await connection.ExecuteAsync(query, parameters);

				if (rowsAffected == 0)
				{
					Console.WriteLine("Пользователь не найден или обновление не было выполнено.");
					throw new Exception("Пользователь не найден или обновление не было выполнено.");
				}

				Console.WriteLine($"Обновлено строк: {rowsAffected}");
			}
		}
	}
}

public class User
{
	public string Role { get; set; }        // "teacher" or "student"
	public string LastName { get; set; }
	public string FirstName { get; set; }
	public string Patronymic { get; set; }
	public string UIN { get; set; }         // Unique Identification Number
	public string Email { get; set; }
	public string PhoneNumber { get; set; }
	public string IdCard { get; set; }
	public string Password { get; set; }
	public string Group { get; set; }       // For students, e.g., "2209". Null for teachers.
}

