using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AC
{
	public class Lesson
	{
		// Concatenated teacher name
		public string TeacherName => $"{LastName} {FirstName} {Patronymic}";

		public string LastName { get; set; }
		public string FirstName { get; set; }
		public string Patronymic { get; set; }
		public string LessonId { get; set; }
		public string Teacher { get; set; }

		// Properties for 24-hour time format
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
		public string Room { get; set; }
		public string Group { get; set; }
		public string Description { get; set; }
		public string QRCode { get; set; }

		public string StartTimeFormatted { get; set; } // 24-hour format
		public string EndTimeFormatted { get; set; }   // 24-hour format

		// Ensures correct serialization/deserialization of PinCode
		[JsonInclude]
		public string PinCode { get; set; }

		public string TeacherUIN { get; set; }
	}

	public class LessonService
	{
		private readonly string _filePath;

		public LessonService()
		{
			_filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "lessons.json");
		}

		// Retrieves all lessons asynchronously
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

		// Adds a new lesson to the list and saves it
		public async Task AddLessonAsync(Lesson lesson)
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

		// Creates a new lesson asynchronously
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

			await AddLessonAsync(newLesson);
			return newLesson;
		}

		// Generates QR code data as a JSON string with 24-hour time format
		private string GenerateQRCode(string teacher, string room, string group, DateTime startTime, DateTime endTime)
		{
			var qrData = new
			{
				Teacher = teacher,
				Room = room,
				Group = group,
				StartTime = startTime.ToString("yyyy-MM-dd HH:mm"), // 24-hour format
				EndTime = endTime.ToString("yyyy-MM-dd HH:mm")     // 24-hour format
			};

			return JsonSerializer.Serialize(qrData);
		}

		// Generates a random 4-digit PIN code
		private string GeneratePinCode()
		{
			var rand = new Random();
			var pinCode = rand.Next(1000, 9999).ToString();

			Debug.WriteLine($"[DEBUG] GeneratePinCode: {pinCode}");

			return pinCode;
		}

		// Retrieves a specific lesson by its ID
		public async Task<Lesson> GetLessonByIdAsync(string lessonId)
		{
			var lessons = await GetLessonsAsync();
			var lesson = lessons.Find(l => l.LessonId == lessonId);

			if (lesson == null)
			{
				Debug.WriteLine($"[ERROR] Lesson with ID {lessonId} not found.");
			}
			else
			{
				Debug.WriteLine($"[DEBUG] Retrieved Lesson: ID: {lesson.LessonId}, PinCode: {lesson.PinCode}");
			}

			return lesson;
		}
	}
}
