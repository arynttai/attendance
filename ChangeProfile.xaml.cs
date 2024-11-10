// ChangeProfile.xaml.cs
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace AC
{
	public partial class ChangeProfile : ContentPage
	{
		private readonly string _role;
		private readonly string _uin;
		private readonly string _token;
		private readonly UserService _userService;

		public ChangeProfile(string role, string uin, string token)
		{
			InitializeComponent();
			_role = role;
			_uin = uin;
			_token = token; // добавили токен
			_userService = new UserService();
			LoadUserDataAsync();
		}

		private async Task LoadUserDataAsync()
		{
			var user = await _userService.GetUserByUINAsync(_uin, _token); // передаем токен при запросе
			firstNameLabel.Text = user.FirstName;
			lastNameLabel.Text = user.LastName;
			patronymicLabel.Text = user.Patronymic;
			emailLabel.Text = user.Email;
			phoneNumberLabel.Text = user.PhoneNumber;
			roleLabel.Text = $"Должность: {user.Role}";
		}

		private async void OnSaveClicked(object sender, EventArgs e)
		{
			var updatedUser = new User
			{
				UIN = _uin,
				FirstName = firstNameLabel.Text,
				LastName = lastNameLabel.Text,
				Patronymic = patronymicLabel.Text,
				Email = emailLabel.Text,
				PhoneNumber = phoneNumberLabel.Text,
				Role = _role
			};

			await _userService.UpdateUserAsync(updatedUser, _token); // передаем токен при обновлении
			await Navigation.PopAsync(); // Go back to the previous page after saving
		}

		private async void OnProfileClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Profile(_role, _uin, _token)); // передаем токен
		}

		private async void OnDesktopClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Desktop(_role, _uin, _token)); // передаем токен
		}

		private async void OnStatisticsClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Statistics(_role, _uin, _token)); // передаем токен
		}
		private async void OnCancelClicked(object sender, EventArgs e)
		{
			await Navigation.PopAsync(); // Go back without saving
		}
	}

}
