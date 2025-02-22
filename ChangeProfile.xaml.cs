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
            NavigationPage.SetHasNavigationBar(this, false);
            _role = role;
            _uin = uin;
            _token = token;
            _userService = new UserService();
            LoadUserDataAsync(); // Загрузка данных пользователя
        }

        private async Task LoadUserDataAsync()
        {
            try
            {
                // Получение данных пользователя
                var user = await _userService.GetUserByUINAsync(_uin, _token);

                // Автоматическое заполнение полей
                emailEntry.Text = user.Email; // Заполняем email
                phoneEntry.Text = user.PhoneNumber; // Заполняем номер телефона
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChangeProfile] Error loading user data: {ex.Message}");
                await DisplayAlert("Ошибка", "Не удалось загрузить данные пользователя.", "OK");
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                // Обновление данных пользователя
                var updatedUser = new User
                {
                    uin = _uin,
                    Email = emailEntry.Text, // Получение обновленного Email
                    PhoneNumber = phoneEntry.Text, // Получение обновленного номера телефона
                    Role = _role
                };

                // Сохранение изменений
                await _userService.UpdateUserAsync(updatedUser, _token);

                // Возврат на страницу профиля
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChangeProfile] Error saving user data: {ex.Message}");
                await DisplayAlert("Ошибка", "Не удалось сохранить изменения.", "OK");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            // Возврат без сохранения
            await Navigation.PopAsync();
        }

        private async void OnProfileClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new Profile(_role, _uin, _token));
        }

        private async void OnDesktopClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new Desktop(_role, _uin, _token));
        }

        private async void OnStatisticsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new Statistics(_role, _uin, _token));
        }
    }
}
