using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui;
using System.Diagnostics;

namespace AC
{
	public class UserService
	{
		private readonly HttpClient _httpClient;

		public UserService()
		{
			_httpClient = new HttpClient
			{
				BaseAddress = new Uri("https://appapa-cugq.onrender.com") // Replace with your actual API address
			};
		}

		public async Task<LoginResponse> LoginAsync(string uin, string password)
		{
			var requestBody = new
			{
				UIN = uin,
				Password = password
			};

			var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

			try
			{
				Debug.WriteLine($"Sending UIN: {uin}, Password: {password}");

				var response = await _httpClient.PostAsync("login", jsonContent);
				var responseBody = await response.Content.ReadAsStringAsync();

				Debug.WriteLine($"Server response: {responseBody}");

				if (!response.IsSuccessStatusCode)
				{
					throw new Exception($"Request failed: {response.StatusCode}. Server response: {responseBody}");
				}

				var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseBody, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				if (loginResponse == null || string.IsNullOrEmpty(loginResponse.Token))
				{
					throw new Exception("Некорректный ответ сервера: отсутствует токен.");
				}

				Debug.WriteLine($"Received JWT Token: {loginResponse.Token}");

				// Save token locally
				Preferences.Set("auth_token", loginResponse.Token);

				return loginResponse;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error processing the login request: {ex.Message}");
				throw new Exception($"Ошибка обработки запроса на вход: {ex.Message}");
			}
		}




		public async Task<User> GetUserByUINAsync(string uin, string token)
		{
			var savedToken = Preferences.Get("auth_token", string.Empty);

			Console.WriteLine($"[GetUserByUINAsync] Saved Token: {savedToken}");
			Console.WriteLine($"[GetUserByUINAsync] Received Token: {token}");

			if (string.IsNullOrEmpty(token))
			{
				Console.WriteLine("[GetUserByUINAsync] Токен отсутствует");
				throw new UnauthorizedAccessException("Токен не найден.");
			}

			if (!token.Contains(".") || token.Split('.').Length != 3)
			{
				Console.WriteLine("[GetUserByUINAsync] Некорректный формат токена");
				throw new Exception("Некорректный формат JWT-токена.");
			}

			// Устанавливаем токен в заголовок Authorization
			_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			try
			{
				Console.WriteLine($"[GetUserByUINAsync] Отправляем запрос с токеном длиной: {token.Length}");
				var response = await _httpClient.GetAsync($"https://appapa-cugq.onrender.com/api/User/{uin}");

				var responseBody = await response.Content.ReadAsStringAsync();
				Console.WriteLine($"[GetUserByUINAsync] Ответ API: {response.StatusCode}, Тело: {responseBody}");

				if (!response.IsSuccessStatusCode)
				{
					Console.WriteLine($"[GetUserByUINAsync] Ошибка при получении пользователя: {response.StatusCode}, Ответ: {responseBody}");
					throw new Exception($"Ошибка при получении пользователя: {response.StatusCode}");
				}

				var user = JsonSerializer.Deserialize<User>(responseBody, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				if (user == null)
				{
					throw new Exception("Пользователь не найден или ответ сервера пуст.");
				}

				return user;
			}
			catch (HttpRequestException ex)
			{
				Console.WriteLine($"[GetUserByUINAsync] Ошибка при выполнении HTTP-запроса: {ex.Message}");
				throw;
			}
			catch (UnauthorizedAccessException ex)
			{
				Console.WriteLine($"[GetUserByUINAsync] Ошибка аутентификации: {ex.Message}");
				throw;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[GetUserByUINAsync] Общая ошибка: {ex.Message}");
				throw;
			}
		}

		public async Task<bool> UpdateUserAsync(User user, string _token)
		{
			var token = Preferences.Get("auth_token", string.Empty);
			if (string.IsNullOrEmpty(token))
			{
				throw new UnauthorizedAccessException("Токен не найден.");
			}

			_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			var jsonContent = new StringContent(JsonSerializer.Serialize(user), Encoding.UTF8, "application/json");
			Console.WriteLine($"[UpdateUserAsync] Updating user with UIN: {user.UIN} and Token: {token}");
			var response = await _httpClient.PatchAsync($"user/{user.UIN}", jsonContent);

			return response.IsSuccessStatusCode;
		}


	}


	public class LoginResponse
	{
		public string Token { get; set; }
		public string Message { get; set; }
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

	public class LoginRequest
	{
		public string UIN { get; set; }
		public string Password { get; set; }
	}
}
