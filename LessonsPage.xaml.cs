using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using AC.AC;
using AC.AC.AC;
namespace AC
{
	public partial class LessonsPage : ContentPage
	{
		private readonly string _role;
		private readonly string _uin;
		private readonly string _group;
		private readonly string _token;
		private readonly LessonService _lessonService;

		public LessonsPage(string role, string uin, string group = null)
		{
			InitializeComponent();
			_role = role;
			_uin = Preferences.Get("UserUIN", uin); // Retrieve UIN from Preferences or use provided UIN
			_group = group;

			_lessonService = new LessonService();

			Console.WriteLine($"Initialized LessonsPage with UIN: {_uin}, Role: {_role}, Group: {_group}");
		}

		protected override async void OnAppearing()
		{
			base.OnAppearing();
			await LoadLessonsAsync(); // Async lessons loading on page appearance
		}

		private async Task LoadLessonsAsync()
		{
			await DisplayAlert("Debug", $"UIN: {_uin}, Group: {_group}", "OK");

			try
			{
				Console.WriteLine($"Attempting to load lessons for UIN: {_uin}, Group: {_group}");

				var lessons = await _lessonService.GetLessonsAsync();

				if (lessons == null || !lessons.Any())
				{
					await DisplayAlert("Ошибка", "Уроки не найдены.", "OK");
					Console.WriteLine("No lessons found.");
					return;
				}

				if (_role == "teacher")
				{
					var filteredLessons = lessons.Where(lesson => lesson.TeacherUIN == _uin).ToList();
					lessonsListView.ItemsSource = filteredLessons;
				}
				else if (_role == "student")
				{
					var filteredLessons = lessons.Where(lesson => lesson.Group == _group).ToList();
					lessonsListView.ItemsSource = filteredLessons;
				}
				else
				{
					await DisplayAlert("Ошибка", "Роль пользователя не определена.", "OK");
					Console.WriteLine("User role is undefined.");
				}
			}
			catch (Exception ex)
			{
				await DisplayAlert("Ошибка", $"Произошла ошибка при загрузке данных: {ex.Message}", "OK");
				Console.WriteLine($"Error loading lessons: {ex.Message}");
			}
		}

		private async void OnLessonTapped(object sender, ItemTappedEventArgs e)
		{
			var selectedLesson = e.Item as Lesson;

			if (selectedLesson != null)
			{
				await DisplayAlert("Урок", $"Вы выбрали урок: {selectedLesson.Description}", "OK");
			}

			((ListView)sender).SelectedItem = null;
		}

		private async void GoBack(object sender, EventArgs e)
		{
			await Navigation.PopAsync();
		}

		private async void OnAddLessonClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new AddLessonPage(_role, _uin , _token)); // Assume you have a page for adding lessons
		}
	}

	internal class AddLessonPage : Page
	{
		private string role;
		private string uin;

		public AddLessonPage(string role, string uin , string token)
		{
			this.role = role;
			this.uin = uin;
		}
	}
}
