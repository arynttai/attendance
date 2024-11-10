using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Maui.Controls;

namespace AC
{
	public partial class Profile : ContentPage
	{
		private readonly string _role;
		private readonly string _uin;
		private readonly string _token;
		private readonly UserService _userService;

		public Profile(string role, string uin, string token)
		{
			InitializeComponent();
			_role = role;
			_uin = uin;
			_token = token;
			_userService = new UserService();

			// Проверка наличия токена
			if (string.IsNullOrEmpty(_token))
			{
				Console.WriteLine("Token is null or empty.");
			}
			else
			{
				Console.WriteLine($"Profile page initialized with UIN: {_uin}, Role: {_role}, Token Length: {_token.Length}");
			}
		}

		protected override async void OnAppearing()
		{
			base.OnAppearing();
			await LoadUserDataAsync();
		}

		private async Task LoadUserDataAsync()
		{
			try
			{
				// Debugging: Log the token value
				Console.WriteLine($"[LoadUserDataAsync] Token being used: {_token}");

				// Verify token validity
				if (!ValidateToken(_token))
				{
					Console.WriteLine("[LoadUserDataAsync] Invalid token.");
					await DisplayAlert("Error", "Invalid token. Please log in again.", "OK");
					return;
				}

				// Load user data
				var user = await _userService.GetUserByUINAsync(_uin, _token);
				if (user == null)
				{
					Console.WriteLine("[LoadUserDataAsync] User not found.");
					await DisplayAlert("Error", "User not found.", "OK");
					return;
				}

				Console.WriteLine($"[LoadUserDataAsync] User loaded: {user.FirstName} {user.LastName}");

				// Display user data
				firstNameLabel.Text = $"Имя: {user.FirstName}";
				lastNameLabel.Text = $"Фамилия: {user.LastName}";
				patronymicLabel.Text = $"Отчество: {user.Patronymic}";
				emailLabel.Text = $"Email: {user.Email}";
				phoneNumberLabel.Text = $"Номер телефона: {user.PhoneNumber}";
				roleLabel.Text = $"Роль: {user.Role}";
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[LoadUserDataAsync] Error loading user data: {ex.Message}");
				await DisplayAlert("Error", $"Failed to load user data: {ex.Message}", "OK");
			}
		}


		private bool ValidateToken(string token)
		{
			try
			{
				var key = Encoding.UTF8.GetBytes("fdsgiuasfogewnrIURibnwfeszidscfqweqfxs"); // Replace with your actual key
				var tokenHandler = new JwtSecurityTokenHandler();
				var validationParameters = new TokenValidationParameters
				{
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(key),
					ValidateIssuer = false,
					ValidateAudience = false,
					ValidateLifetime = true,
					ClockSkew = TimeSpan.Zero
				};

				tokenHandler.ValidateToken(token, validationParameters, out _);
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Token validation error: {ex.Message}");
				return false;
			}
		}

		private string GetTokenHeader(string token)
		{
			try
			{
				var parts = token.Split('.');
				if (parts.Length == 3)
				{
					var header = parts[0];
					var jsonBytes = Convert.FromBase64String(header + "=="); // Handle Base64 URL encoding
					return Encoding.UTF8.GetString(jsonBytes);
				}
				return "Invalid token format";
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error decoding token header: {ex.Message}");
				return "Error decoding token header";
			}
		}

		private async void OnEditProfileClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new ChangeProfile(_role, _uin, _token));
		}

		private async void OnLogOutClicked(object sender, EventArgs e)
		{
			Preferences.Clear();
			await Navigation.PushAsync(new StartPage());
		}

		private async void OnStatisticsClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Statistics(_role, _uin, _token));
		}

		private async void OnDesktopClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Desktop(_role, _uin, _token));
		}

		private async void OnProfileClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Profile(_role, _uin, _token));
		}
	}
}
