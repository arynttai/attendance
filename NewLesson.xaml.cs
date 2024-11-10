using System;
using System.Linq;
using System.Threading.Tasks;
using AC.AC.AC;
using Microsoft.Maui.Controls;
using AC.AC;
namespace AC
{
	public partial class NewLesson : ContentPage
	{
		private readonly LessonService _lessonService;
		private readonly UserService _userService;
		private readonly string _role;
		private readonly string _uin;
		private readonly string _token;
		private string _teacherFullName;

		public NewLesson(string role, string uin, string token)
		{
			InitializeComponent();
			_lessonService = new LessonService();
			_userService = new UserService();
			_role = role;
			_uin = uin;
			_token = token; // добавили токен
		}

		protected override async void OnAppearing()
		{
			base.OnAppearing();
			await LoadUserDataAsync();
		}

		private async Task LoadUserDataAsync()
		{
			var user = await _userService.GetUserByUINAsync(_uin, _token); // передаем токен
			_teacherFullName = $"{user.LastName} {user.FirstName} {user.Patronymic}";
			teacherLabel.Text = _teacherFullName;
		}

		private async void OnSaveClicked(object sender, EventArgs e)
		{
			var newLesson = new Lesson
			{
				LessonId = Guid.NewGuid().ToString(),
				TeacherUIN = _uin,
				Teacher = _teacherFullName,
				StartTime = DateTime.Today.Add(startTimePicker.Time),
				EndTime = DateTime.Today.Add(endTimePicker.Time),
				Room = roomEntry.Text,
				Group = groupPicker.SelectedItem?.ToString(),
				Description = descriptionEntry.Text
			};

			await _lessonService.AddLessonAsync(newLesson, _token); // передаем токен при создании урока
			await Navigation.PushAsync(new ScanWindowPrepoda(_role, _uin, _token, newLesson)); // передаем токен
		}
		private async void GoBack(object sender, EventArgs e)
		{
			await Navigation.PopAsync();
		}
	}

}
