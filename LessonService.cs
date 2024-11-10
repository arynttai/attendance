using Dapper;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace AC
{
	using System;
	using System.Text.Json.Serialization;
	using Microsoft.Maui.Controls;

	namespace AC
	{
		public class Lesson
		{
			// Конкатенированное имя преподавателя
			public string TeacherName => $"{LastName} {FirstName} {Patronymic}";

			public string LastName { get; set; }
			public string FirstName { get; set; }
			public string Patronymic { get; set; }
			public string LessonId { get; set; }
			public string Teacher { get; set; }

			// Свойства для 24-часового формата времени
			public DateTime StartTime { get; set; }
			public DateTime EndTime { get; set; }
			public string Room { get; set; }
			public string Group { get; set; }
			public string Description { get; set; }
			public string QRCode { get; set; }

			public string StartTimeFormatted { get; set; } // 24-часовой формат
			public string EndTimeFormatted { get; set; }   // 24-часовой формат

			// Обеспечивает корректную сериализацию/десериализацию PinCode
			[JsonInclude]
			public string PinCode { get; set; }

			public string TeacherUIN { get; set; }

			// Свойство для хранения изображения QR-кода
			[JsonIgnore] // Не сериализуется, так как генерируется на лету
			public ImageSource QRCodeImage { get; set; }
		}






		namespace AC
		{
			public class LessonService
			{
				private readonly string _filePath;
				private readonly string _token;
				private readonly HttpClient _httpClient;

				public LessonService()
				{
					_filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "lessons.json");

					_httpClient = new HttpClient
					{
						BaseAddress = new Uri("https://appapa-cugq.onrender.com/api/lesson") // Base URL for your API
					};
				}

				// Асинхронно получает все уроки
				public async Task<List<Lesson>> GetLessonsAsync()
				{
					if (!File.Exists(_filePath))
					{
						return new List<Lesson>();
					}

					try
					{
						var json = await File.ReadAllTextAsync(_filePath);
						var lessons = JsonSerializer.Deserialize<List<Lesson>>(json, new JsonSerializerOptions
						{
							PropertyNameCaseInsensitive = true
						}) ?? new List<Lesson>();

						foreach (var lesson in lessons)
						{
							Debug.WriteLine($"[DEBUG] Loaded Lesson ID: {lesson.LessonId}, PIN Code: {lesson.PinCode}");
						}

						return lessons;
					}
					catch (JsonException ex)
					{
						Debug.WriteLine($"[ERROR] Failed to deserialize JSON: {ex.Message}");
						return new List<Lesson>();
					}
				}

				// Асинхронно добавляет новый урок в JSON файл
				public async Task AddLessonAsync(Lesson lesson, string _token)
				{
					var lessons = await GetLessonsAsync();
					lessons.Add(lesson);

					try
					{
						var json = JsonSerializer.Serialize(lessons, new JsonSerializerOptions { WriteIndented = true });
						await File.WriteAllTextAsync(_filePath, json);
						Debug.WriteLine("[DEBUG] Lessons successfully saved.");
					}
					catch (Exception ex)
					{
						Debug.WriteLine($"[ERROR] Failed to save lessons: {ex.Message}");
					}
				}

				// Асинхронно создает новый урок
				public async Task<Lesson> CreateNewLessonAsync(string role, string uin, string teacher, string lastName, string firstName, string patronymic, DateTime startTime, DateTime endTime, string room, string group, string description)
				{
					if (role != "teacher")
					{
						throw new UnauthorizedAccessException("Only teachers can create lessons.");
					}

					var newLesson = new Lesson
					{
						LessonId = Guid.NewGuid().ToString(),
						Teacher = teacher,
						LastName = lastName,
						FirstName = firstName,
						Patronymic = patronymic,
						StartTime = startTime,
						EndTime = endTime,
						Room = room,
						Group = group,
						Description = description,
						QRCode = GenerateQRCode(teacher, room, group, startTime, endTime),
						PinCode = GeneratePinCode(),
						TeacherUIN = uin
					};

					Debug.WriteLine($"[DEBUG] Creating New Lesson: ID: {newLesson.LessonId}, PinCode: {newLesson.PinCode}");

					await AddLessonAsync(newLesson, _token);
					return newLesson;
				}

				// Генерирует данные для QR-кода в формате JSON с 24-часовым форматом времени
				public string GenerateQRCode(string teacher, string room, string group, DateTime startTime, DateTime endTime)
				{
					var qrData = new
					{
						Teacher = teacher,
						Room = room,
						Group = group,
						StartTime = startTime.ToString("yyyy-MM-dd HH:mm"), // 24-часовой формат
						EndTime = endTime.ToString("yyyy-MM-dd HH:mm")     // 24-часовой формат
					};

					return JsonSerializer.Serialize(qrData);
				}

				// Генерирует случайный 4-значный PIN-код
				public string GeneratePinCode()
				{
					var rand = new Random();
					var pinCode = rand.Next(1000, 9999).ToString();

					Debug.WriteLine($"[DEBUG] GeneratePinCode: {pinCode}");

					return pinCode;
				}

				// Получает урок из базы данных PostgreSQL по его ID
				public async Task<Lesson> GetLessonByIdAsync(string lessonId)
				{
					try
					{
						var response = await _httpClient.GetAsync($"{lessonId}");
						if (response.IsSuccessStatusCode)
						{
							var jsonResponse = await response.Content.ReadAsStringAsync();
							return JsonSerializer.Deserialize<Lesson>(jsonResponse, new JsonSerializerOptions
							{
								PropertyNameCaseInsensitive = true
							});
						}
						else
						{
							throw new Exception($"Failed to fetch lesson: {response.StatusCode}");
						}
					}
					catch (Exception ex)
					{
						Debug.WriteLine($"Error fetching lesson: {ex.Message}");
						throw;
					}
				}

				public async Task<IEnumerable<Lesson>> GetAllLessonsAsync()
				{
					try
					{
						var response = await _httpClient.GetAsync("");
						if (response.IsSuccessStatusCode)
						{
							var jsonResponse = await response.Content.ReadAsStringAsync();
							return JsonSerializer.Deserialize<IEnumerable<Lesson>>(jsonResponse, new JsonSerializerOptions
							{
								PropertyNameCaseInsensitive = true
							});
						}
						else
						{
							throw new Exception($"Failed to fetch all lessons: {response.StatusCode}");
						}
					}
					catch (Exception ex)
					{
						Debug.WriteLine($"Error fetching lessons: {ex.Message}");
						throw;
					}
				}
			}

		}
	}
}

