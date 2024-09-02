using System.Diagnostics;

namespace AC
{
	public partial class StartQ3 : ContentPage
	{
		private readonly string _role;
		private readonly string _lastName;
		private readonly string _firstName;
		private readonly string _patronymic;

		public StartQ3(string role, string lastName, string firstName, string patronymic)
		{
			InitializeComponent();
			_role = role;
			_lastName = lastName;
			_firstName = firstName;
			_patronymic = patronymic;
		}

		private async void GoBack(object sender, EventArgs e)
		{
			await Navigation.PopAsync();
		}

		private async void OnContinueClicked(object sender, EventArgs e)
		{
			string uin = uinEntry.Text;
			string password = passwordEntry.Text;

			if (string.IsNullOrWhiteSpace(uin) || string.IsNullOrWhiteSpace(password))
			{
				await DisplayAlert("Ошибка", "Пожалуйста, заполните все поля.", "OK");
				return;
			}

			try
			{
				var userService = new UserService();
				var existingUser = await userService.GetUserByUINAsync(uin);

				if (existingUser == null)
				{
					await DisplayAlert("Ошибка", "Пользователь не найден или произошла ошибка при доступе к базе данных.", "OK");
					return;
				}

				if (userService.VerifyPassword(password, existingUser.Password) && existingUser.Role == _role)
				{
					Preferences.Set("UserUIN", uin);  // Save UIN to Preferences
					await Navigation.PushAsync(new StartQ4(_role));
				}
				else
				{
					await DisplayAlert("Ошибка", "Неверный пароль или роль.", "OK");
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"[ERROR] {ex.Message}");
				await DisplayAlert("Ошибка", "Произошла ошибка при проверке данных. Пожалуйста, попробуйте еще раз.", "OK");
			}
		}
	}
}
