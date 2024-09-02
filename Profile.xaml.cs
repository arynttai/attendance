using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace AC
{
	public partial class Profile : ContentPage
	{
		private readonly string _role;
		private readonly string _uin;
		private readonly UserService _userService;

		public Profile(string role, string uin)
		{
			InitializeComponent();

			_role = role;
			_uin = Preferences.Get("UserUIN", uin); // Retrieve UIN from Preferences or use provided UIN

			_userService = new UserService();

			Console.WriteLine($"Initialized Profile with UIN: {_uin} and Role: {_role}");
		}

		protected override async void OnAppearing()
		{
			base.OnAppearing();
			await LoadUserDataAsync(); // Async user data loading on page appearance
		}

		private async void OnDesktopClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Desktop(_role, _uin)); // Navigate to Desktop page
		}

		private async void OnStatisticsClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Statistics(_role, _uin));
		}

		private async void OnProfileClicked(object sender, EventArgs e)
		{
			// Check if we're already on the Profile page
			if (!(Navigation.NavigationStack.LastOrDefault() is Profile))
			{
				await Navigation.PushAsync(new Profile(_role, _uin));
			}
		}

		private async Task LoadUserDataAsync()
		{
			await DisplayAlert("Debug", $"UIN: {_uin}", "OK");

			try
			{
				Console.WriteLine($"Attempting to load user data for UIN: {_uin}");

				var user = await _userService.GetUserByUINAsync(_uin);

				if (user != null)
				{
					firstNameLabel.Text = $"Фамилия: {user.LastName}";
					lastNameLabel.Text = $"Имя: {user.FirstName}";
					patronymicLabel.Text = $"Отчество: {user.Patronymic}";
					emailLabel.Text = $"Email: {user.Email}";
					phoneNumberLabel.Text = $"Номер телефона: {user.PhoneNumber}";
					roleLabel.Text = $"Должность: {user.Role}";
				}
				else
				{
					await DisplayAlert("Ошибка", "Пользователь не найден.", "OK");
					Console.WriteLine("User not found.");
				}
			}
			catch (Exception ex)
			{
				await DisplayAlert("Ошибка", $"Произошла ошибка при загрузке данных: {ex.Message}", "OK");
				Console.WriteLine($"Error loading user data: {ex.Message}");
			}
		}

		private async void OnEditProfileClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new ChangeProfile(_role, _uin));
		}

		private async void OnLogOutClicked(object sender, EventArgs e)
		{
			Preferences.Clear();
			await Navigation.PushAsync(new StartPage());
		}
	}
}
