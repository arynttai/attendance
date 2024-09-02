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
		private readonly UserService _userService;

		public ChangeProfile(string role, string uin)
		{
			InitializeComponent();
			_role = role;
			_uin = uin;
			_userService = new UserService();
			LoadUserDataAsync();
		}

		private async Task LoadUserDataAsync()
		{
			var user = await _userService.GetUserByUINAsync(_uin);
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
				Role = _role // Role is usually not editable, hence taken from the original role.
			};

			await _userService.UpdateUserAsync(updatedUser);
			await Navigation.PopAsync(); // Go back to the previous page after saving
		}

		private async void OnCancelClicked(object sender, EventArgs e)
		{
			await Navigation.PopAsync(); // Go back without saving
		}

		private async void OnDesktopClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Desktop(_role));
		}

		private async void OnStatisticsClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Statistics(_role , _uin));
		}

		private async void OnProfileClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Profile(_role, _uin));
		}
	}
}
