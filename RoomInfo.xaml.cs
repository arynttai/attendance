using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace AC
{
	public partial class RoomInfo : ContentPage
	{
		private readonly string _role;
		private readonly string _uin;
		private readonly string _group;
		private readonly LessonService _lessonService;

		public RoomInfo(string role, string uin)
		{
			InitializeComponent();
			_role = role;
			_uin = Preferences.Get("UserUIN", uin);
			_group = null;
			_lessonService = new LessonService();
		}

		public RoomInfo(string role, string uin, string group)
		{
			InitializeComponent();
			_role = role;
			_uin = Preferences.Get("UserUIN", uin);
			_group = group;
			_lessonService = new LessonService();
		}

		protected override async void OnAppearing()
		{
			base.OnAppearing();
			await LoadLessonsAsync();
		}

		private async Task LoadLessonsAsync()
		{
			try
			{
				var lessons = await _lessonService.GetLessonsAsync();

				if (lessons == null || !lessons.Any())
				{
					Debug.WriteLine("[INFO] No lessons found.");
					return; // Если нет уроков, просто выходим
				}

				var filteredLessons = (_role == "teacher")
					? lessons.Where(lesson => lesson.TeacherUIN == _uin && DateTime.Now <= lesson.EndTime.AddHours(12)).ToList()
					: lessons.Where(lesson => lesson.Group == _group && DateTime.Now <= lesson.EndTime.AddHours(12)).ToList();

				if (!filteredLessons.Any())
				{
					Debug.WriteLine("[INFO] No filtered lessons found.");
					return; // Если нет подходящих уроков, просто выходим
				}

				Debug.WriteLine($"[INFO] Loaded {filteredLessons.Count} lessons.");

				// Convert lesson start and end times to 24-hour format for display
				foreach (var lesson in filteredLessons)
				{
					lesson.StartTimeFormatted = lesson.StartTime.ToString("HH:mm"); // 24-hour format
					lesson.EndTimeFormatted = lesson.EndTime.ToString("HH:mm");     // 24-hour format
				}

				// Устанавливаем ItemSource для ListView
				lessonsListView.ItemsSource = filteredLessons;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"[ERROR] Failed to load lessons: {ex.Message}");
				await DisplayAlert("Ошибка", "Не удалось загрузить уроки.", "OK");
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
				await Navigation.PushAsync(new NewLesson(_role, _uin));
			}
		}

		private async void GoBack(object sender, EventArgs e)
		{
			await Navigation.PopAsync();
		}

		private async void OnDesktopClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Desktop(_role, _uin));
		}

		private async void OnStatisticsClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Statistics(_role, _uin));
		}

		private async void OnProfileClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Profile(_role, _uin));
		}
	}

	// Updated Lesson class with formatted time properties
	
}
