using System;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
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
            NavigationPage.SetHasNavigationBar(this, false);
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

            // Проверка/валидация токена
            if (string.IsNullOrEmpty(_token) || !ValidateToken(_token))
            {
                await DisplayAlert("Error", "Session expired. Please log in again.", "OK");
                Preferences.Clear();
                Application.Current.MainPage = new NavigationPage(new StartPage());
                return;
            }

            await LoadUserDataAsync();

            // Скрываем элементы в зависимости от роли
            if (_role == "student")
            {
                logoutbtn.IsVisible = true; // Скрываем кнопку "Выйти" для студентов
                groupFrame.IsVisible = true; // Отображаем группу
            }
            else
            {
                logoutbtn.IsVisible = true; // Отображаем кнопку "Выйти" для преподавателей
                groupFrame.IsVisible = false; // Скрываем отображение группы
            }
        }


        private async Task LoadUserDataAsync()
        {
            try
            {
                var user = await _userService.GetUserByUINAsync(_uin, _token);
                if (user == null)
                {
                    await DisplayAlert("Error", "User not found.", "OK");
                    return;
                }

                // Заполняем поля ФИО*-
                fullNameLabel.Text = $"{user.LastName} {user.FirstName} {user.Patronymic}";

                // Заполняем должность/роль
                roleLabel.Text = user.Role;

                // Если поле Email пустое/NULL → показываем "Не указан"
                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    emailLabel.Text = "Не указан";
                }
                else
                {
                    emailLabel.Text = user.Email;
                }

                // Телефон
                if (string.IsNullOrWhiteSpace(user.PhoneNumber))
                {
                    phoneNumberLabel.Text = "Не указан";
                }
                else
                {
                    phoneNumberLabel.Text = user.PhoneNumber;
                }

                // Если это студент, показываем группу, иначе скрываем
                if (_role == "student")
                {
                    groupLabel.Text = user.Group; // Например, "2204"
                    groupLabel.IsVisible = true;
                }
                else
                {
                    groupLabel.Text = "";
                    groupLabel.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load user data: {ex.Message}", "OK");
            }
        }


        private bool ValidateToken(string token)
        {
            try
            {
                // Используем тот же reRET_KEY, что и на сервере
                var key = Encoding.UTF8.GetBytes("DBLFKJADSBCBALIasfGSGDSDHgdf6EF&ADL@E3213IL>SBBFL");
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false, // Настройте при необходимости
                    ValidateAudience = false, // Настройте при необходимости
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
            Preferences.Clear(); // Удаляем сохраненные данные
            Application.Current.MainPage = new NavigationPage(new SignIn()); // Переход на страницу входа
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