using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace AC
{
	public partial class NewLesson : ContentPage
	{
		private readonly LessonService _lessonService;
		private readonly UserService _userService;
		private readonly string _role;
		private readonly string _uin;
		private string _teacherFullName;

		public NewLesson(string role, string uin)
		{
			InitializeComponent();
			_lessonService = new LessonService();
			_userService = new UserService();
			_role = role;
			_uin = Preferences.Get("UserUIN", uin); // Keep this consistent
		}

		protected override async void OnAppearing()
		{
			base.OnAppearing();
			await LoadUserDataAsync(); // Async user data loading on page appearance
		}

		private async Task LoadUserDataAsync()
		{
			try
			{
				var user = await _userService.GetUserByUINAsync(_uin);

				if (user != null)
				{
					// Concatenate the teacher's full name and store it
					_teacherFullName = $"{user.LastName} {user.FirstName} {user.Patronymic}";
					teacherLabel.Text = _teacherFullName;
				}
				else
				{
					await DisplayAlert("Ошибка", "Пользователь не найден.", "OK");
				}
			}
			catch (Exception ex)
			{
				await DisplayAlert("Ошибка", $"Произошла ошибка при загрузке данных: {ex.Message}", "OK");
			}
		}

		private async void OnSaveClicked(object sender, EventArgs e)
		{
			var newLesson = new Lesson
			{
				LessonId = Guid.NewGuid().ToString(),
				TeacherUIN = _uin, // Устанавливаем UIN текущего учителя
				Teacher = _teacherFullName, // Assign the teacher's full name
				StartTime = DateTime.Today.Add(startTimePicker.Time),
				EndTime = DateTime.Today.Add(endTimePicker.Time),
				Room = roomEntry.Text,
				Group = groupPicker.SelectedItem?.ToString(), // Группа, если она выбрана
				Description = descriptionEntry.Text
			};

			await _lessonService.AddLessonAsync(newLesson);

			// Navigate to ScanWindowPrepoda and pass the lesson data for QR code generation
			await Navigation.PushAsync(new ScanWindowPrepoda(_role, _uin, newLesson));
		}

		private async void GoBack(object sender, EventArgs e)
		{
			await Navigation.PopAsync();
		}
	}
}
