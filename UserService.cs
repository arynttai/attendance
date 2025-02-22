using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui;
using System.Diagnostics;
using System.Text.Json.Serialization;
namespace AC
{
    public class UserService
    {
        private readonly HttpClient _httpClient;

        public UserService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://10.250.0.64:8989") // Замените на ваш фактический адрес API
            };
        }

        public async Task<LoginResponse> LoginAsync(string uin, string password)
        {
            var requestBody = new
            {
                uin = uin,
                password = password
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            try
            {
                Debug.WriteLine($"Отправка данных UIN: {uin}, Password: {password}");
                var response = await _httpClient.PostAsync("/api/login_by_uin", jsonContent);
                var responseBody = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"Ответ сервера: {responseBody}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Запрос не удался: {response.StatusCode}. Ответ сервера: {responseBody}");
                }

                return JsonSerializer.Deserialize<LoginResponse>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"Ошибка подключения: {ex.Message}");
                throw new Exception("Не удалось подключиться к серверу.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Общая ошибка: {ex.Message}");
                throw;
            }
        }

        public async Task<User> GetUserByUINAsync(string uin, string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("Токен не найден.");
            }

            // Устанавливаем заголовок Authorization
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                Console.WriteLine($"[GetUserByUINAsync] Отправляем запрос с токеном длиной: {token.Length}");
                var response = await _httpClient.GetAsync($"/api/user/{uin}");

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
            Console.WriteLine($"[UpdateUserAsync] Updating user with UIN: {user.uin} and Token: {token}");
            var response = await _httpClient.PatchAsync($"user/{user.uin}", jsonContent);

            return response.IsSuccessStatusCode;
        }
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public string Message { get; set; }
        public int UserId { get; set; }
        public string UserRole { get; set; }
    }

    

public class User
    {
        public string Role { get; set; }

        [JsonPropertyName("last_name")]
        public string LastName { get; set; }

        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("patronymic")]
        public string Patronymic { get; set; }

        public string uin { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; }

        [JsonPropertyName("id_card")]
        public string IdCard { get; set; }

        public string password { get; set; }

        [JsonPropertyName("group")]
        public string Group { get; set; }
    }


    public class LoginRequest
    {
        public string uin { get; set; }
        public string password { get; set; }
    }
}
