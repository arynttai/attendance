using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Newtonsoft.Json.Linq;

namespace AC
{
    public partial class SignIn : ContentPage
    {
        private readonly UserService _userService;
        private const int maxAttempts = 5;
        private int failedAttempts = 0;
        private DateTime? lockoutEndTime = null;

        public SignIn()
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
            InitializeAttemptsData();
            _userService = new UserService();
        }

        private void InitializeAttemptsData()
        {
            failedAttempts = Preferences.Get("failedAttempts", 0);
            Debug.WriteLine($"Loaded failed attempts from Preferences: {failedAttempts}");

            string lockoutTimeStr = Preferences.Get("lockoutEndTime", null);
            if (!string.IsNullOrEmpty(lockoutTimeStr))
            {
                lockoutEndTime = DateTime.Parse(lockoutTimeStr, null, DateTimeStyles.RoundtripKind);
                Debug.WriteLine($"Loaded lockout end time from Preferences: {lockoutEndTime}");
            }
        }

        // Login Logic and Attempts Limiting

        private async void OnContinueClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(uinEntry.Text) || string.IsNullOrWhiteSpace(passwordEntry.Text))
            {
                await DisplayAlert("Error", "Please fill in all fields.", "OK");
                return;
            }

            var currentTime = await GetAstanaTimeAsync();
            if (lockoutEndTime.HasValue && currentTime < lockoutEndTime.Value)
            {
                var remainingTime = lockoutEndTime.Value - currentTime;
                await DisplayAlert("Blocked", $"Suspicious activity. Try again in {remainingTime.Minutes} minutes.", "OK");
                return;
            }

            await AttemptLogin(uinEntry.Text, passwordEntry.Text);
        }


        private async Task<DateTime> GetAstanaTimeAsync()
        {
            using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
            {
                const int retryCount = 1;
                const int delay = 3000;

                for (int attempt = 0; attempt < retryCount; attempt++)
                {
                    try
                    {
                        var response = await client.GetStringAsync("https://timeapi.io/api/Time/current/zone?timeZone=Asia/Almaty");
                        if (string.IsNullOrWhiteSpace(response)) throw new Exception("Received empty response from the server.");

                        var json = JObject.Parse(response);
                        var dateTimeStr = (string)json["dateTime"];
                        if (string.IsNullOrWhiteSpace(dateTimeStr)) throw new Exception("Datetime value from API is empty or unavailable.");

                        return DateTimeOffset.Parse(dateTimeStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).DateTime;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error retrieving time: {ex.Message}");
                        await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
                        await Task.Delay(delay);
                    }
                }

                throw new Exception("Retries exhausted. Unable to retrieve time.");
            }
        }

        private async Task AttemptLogin(string uin, string password)
        {
            try
            {
                // Вход через API 
                var loginResponse = await _userService.LoginAsync(uin, password);

                if (loginResponse != null && loginResponse.Success)
                {
                    ResetLoginAttempts();

                    // Сохраняем данные авторизации
                    Preferences.Set("auth_token", loginResponse.Token);
                    Preferences.Set("user_uin", uin);

                    // Получаем данные пользователя
                    var user = await _userService.GetUserByUINAsync(uin, loginResponse.Token);
                    if (user != null)
                    {
                        string role = user.Role; // Получаем роль из данных пользователя
                        Preferences.Set("user_role", role);

                        // Переход на Desktop
                        Application.Current.MainPage = new NavigationPage(new Desktop(role, uin, loginResponse.Token));
                    }
                    else
                    {
                        await DisplayAlert("Error", "Failed to retrieve user details.", "OK");
                    }
                }
                else
                {
                    await DisplayAlert("Error", "Invalid login or password.", "OK");
                    failedAttempts++;
                    Preferences.Set("failedAttempts", failedAttempts);

                    if (failedAttempts >= maxAttempts)
                    {
                        lockoutEndTime = DateTime.Now.AddMinutes(15);
                        Preferences.Set("lockoutEndTime", lockoutEndTime.Value.ToString("o"));
                        await DisplayAlert("Blocked", "Suspicious activity. Try again in 15 minutes.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Login error: {ex.Message}");
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        private void ResetLoginAttempts()
        {
            failedAttempts = 0;
            Preferences.Set("failedAttempts", failedAttempts);
            lockoutEndTime = null;
            Preferences.Remove("lockoutEndTime");
        }



        // Password Visibility Toggle
        private void OnPasswordButtonPressed(object sender, EventArgs e)
        {
            passwordEntry.IsPassword = false;
        }

        private void OnPasswordButtonReleased(object sender, EventArgs e)
        {
            passwordEntry.IsPassword = true;
        }

        private async void GoBack(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
