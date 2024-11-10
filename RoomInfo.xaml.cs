using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Dapper;
using Microsoft.Maui.Controls;
using Npgsql;
using ZXing; // Библиотека ZXing для генерации QR-кодов
using ZXing.Common;
using System.IO;
using AC.AC.AC;
using AC.AC;
using System.Drawing;
using QRCoder;
using QRCodeGenerator = QRCoder.QRCodeGenerator;
using ZXing;
using ZXing.Common;
namespace AC
{
	public partial class RoomInfo : ContentPage
	{
		private readonly string _role;
		private readonly string _uin;
		private readonly string _group;
		private readonly LessonService _lessonService;
		private readonly string _token;

		public RoomInfo(string role, string uin, string token)
		{
			InitializeComponent();
			_role = role;
			_uin = Preferences.Get("UserUIN", uin);
			_group = null;
			_token = token;

			_lessonService = new LessonService();
		}

		public RoomInfo(string role, string uin, string group, string token)
		{
			InitializeComponent();
			_role = role;
			_uin = Preferences.Get("UserUIN", uin);
			_group = group;
			_token = token;

			_lessonService = new LessonService();
		}

		protected override async void OnAppearing()
		{
			base.OnAppearing();

			loadingIndicator.IsRunning = true;
			loadingIndicator.IsVisible = true;
			loadingLabel.IsVisible = true;
			lessonsListView.IsVisible = false;

			await LoadLessonsAsync();

			loadingIndicator.IsRunning = false;
			loadingIndicator.IsVisible = false;
			loadingLabel.IsVisible = false;
			lessonsListView.IsVisible = true;
		}

		private async Task LoadLessonsAsync()
		{
			try
			{
				var lessons = await _lessonService.GetLessonsAsync();

				if (lessons == null || !lessons.Any())
				{
					Debug.WriteLine("[INFO] No lessons found.");
					return;
				}

				// Фильтрация уроков в зависимости от роли
				var filteredLessons = (_role == "teacher")
					? lessons.Where(lesson => lesson.TeacherUIN == _uin && DateTime.Now <= lesson.EndTime.AddHours(12)).ToList()
					: lessons.Where(lesson => lesson.Group == _group && DateTime.Now <= lesson.EndTime.AddHours(12)).ToList();

				if (!filteredLessons.Any())
				{
					Debug.WriteLine("[INFO] No filtered lessons found.");
					return;
				}

				// Получение списка всех lessonId для массового запроса PinCode
				var lessonIds = filteredLessons.Select(lesson => lesson.LessonId).ToList();
				var pinCodes = await GetPinCodesForLessonsAsync(lessonIds);

				foreach (var lesson in filteredLessons)
				{
					// Присвоение PinCode, если он найден
					if (pinCodes.TryGetValue(lesson.LessonId, out var pinCode))
					{
						lesson.PinCode = pinCode;
					}
					else
					{
						// Генерация PIN-кода, если он не найден в базе данных
						lesson.PinCode = _lessonService.GeneratePinCode();
						// При необходимости, сохраните сгенерированный PIN-код обратно в базу данных или JSON
						// Это зависит от вашей логики данных
					}

					// Генерация содержимого QR-кода на основе данных урока
					string qrCodeContent = GenerateQRData(lesson); // Используем метод GenerateQRData
					lesson.QRCodeImage = GenerateQRCodeImage(qrCodeContent);

					// Форматирование времени для отображения
					lesson.StartTimeFormatted = lesson.StartTime.ToString("HH:mm");
					lesson.EndTimeFormatted = lesson.EndTime.ToString("HH:mm");

					Debug.WriteLine($"[INFO] Lesson ID: {lesson.LessonId}, PinCode: {lesson.PinCode}");
				}

				// Установка обновленного списка уроков в ListView
				lessonsListView.ItemsSource = filteredLessons;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"[ERROR] Failed to load lessons: {ex.Message}");
				await DisplayAlert("Ошибка", "Не удалось загрузить уроки.", "OK");
			}
		}

		private string GenerateQRData(Lesson lesson)
		{
			return $"LessonId: {lesson.LessonId}\n" +
				   $"Teacher: {lesson.Teacher}\n" +
				   $"Room: {lesson.Room}\n" +
				   $"Group: {lesson.Group}\n" +
				   $"StartTime: {lesson.StartTime:HH:mm}\n" +
				   $"EndTime: {lesson.EndTime:HH:mm}\n" +
				   $"Description: {lesson.Description}";
		}

		private ImageSource GenerateQRCodeImage(string qrContent)
		{
			var writer = new BarcodeWriterPixelData
			{
				Format = BarcodeFormat.QR_CODE,
				Options = new EncodingOptions
				{
					Height = 200,
					Width = 200,
					Margin = 1
				}
			};
			var pixelData = writer.Write(qrContent);

			// Создание изображения из пиксельных данных
			using (var stream = new MemoryStream())
			{
				using (var bitmap = new System.Drawing.Bitmap(pixelData.Width, pixelData.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb))
				{
					var bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, pixelData.Width, pixelData.Height),
						System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
					try
					{
						// Копирование пиксельных данных в изображение
						System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
					}
					finally
					{
						bitmap.UnlockBits(bitmapData);
					}
					bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
				}
				return ImageSource.FromStream(() => new MemoryStream(stream.ToArray()));
			}
		}

		private async Task<Dictionary<string, string>> GetPinCodesForLessonsAsync(List<string> lessonIds)
		{
			try
			{
				const string connectionString = "Host=dpg-csogsqggph6c73braemg-a.oregon-postgres.render.com;Port=5432;Username=delechka;Password=ZSQ5jHTFX2kfJy35JkfxobQ0qYh6ymGG;Database=attendance_9s8z;SslMode=Require;Trust Server Certificate=true";

				using (var connection = new NpgsqlConnection(connectionString))
				{
					await connection.OpenAsync();

					// Формирование запроса для получения PinCode для всех уроков одновременно
					string query = @"SELECT lessonid, pincode FROM lessons WHERE lessonid = ANY(@LessonIds)";
					var result = await connection.QueryAsync<(string LessonId, string PinCode)>(query, new { LessonIds = lessonIds });

					// Преобразование результата в словарь
					return result.ToDictionary(r => r.LessonId, r => r.PinCode);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"[ERROR] Failed to get PinCodes for lessons: {ex.Message}");
				return new Dictionary<string, string>();
			}
		}

		// Метод для получения PinCode отдельного урока (не используется при массовом запросе)
		private async Task<string> GetPinCodeForLessonAsync(string lessonId)
		{
			try
			{
				const string connectionString = "Host=dpg-csogsqggph6c73braemg-a.oregon-postgres.render.com;Port=5432;Username=delechka;Password=ZSQ5jHTFX2kfJy35JkfxobQ0qYh6ymGG;Database=attendance_9s8z;SslMode=Require;Trust Server Certificate=true";

				using (var connection = new NpgsqlConnection(connectionString))
				{
					await connection.OpenAsync();
					string query = @"SELECT pincode FROM lessons WHERE lessonid = @LessonId";
					return await connection.QueryFirstOrDefaultAsync<string>(query, new { LessonId = lessonId });
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"[ERROR] Failed to get PinCode for lesson {lessonId}: {ex.Message}");
				return null;
			}
		}

		private async void OnLessonTapped(object sender, ItemTappedEventArgs e)
		{
			if (e.Item is Lesson lesson)
			{
				Debug.WriteLine($"[INFO] Lesson tapped: {lesson.LessonId}");
				Debug.WriteLine($"[INFO] Navigating with role: {_role}, uin: {_uin}, lessonId: {lesson.LessonId}");

				try
				{
					await Navigation.PushAsync(new LessonInfo(_role, _uin, lesson.LessonId));
					Debug.WriteLine("[INFO] Successfully navigated to LessonInfo.");
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"[ERROR] Navigation to LessonInfo failed: {ex.Message}");
					await DisplayAlert("Ошибка", "Не удалось открыть страницу урока.", "OK");
				}
			}

			((ListView)sender).SelectedItem = null;
		}

		private async void OnNewLessonButtonClicked(object sender, EventArgs e)
		{
			if (_role == "teacher")
			{
				await Navigation.PushAsync(new NewLesson(_role, _uin, _token));
			}
		}

		private async void GoBack(object sender, EventArgs e)
		{
			await Navigation.PopAsync();
		}

		private async void OnDesktopClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Desktop(_role, _uin, _token));
		}

		private async void OnStatisticsClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Statistics(_role, _uin, _token));
		}

		private async void OnProfileClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Profile(_role, _uin, _token));
		}
	}
}
