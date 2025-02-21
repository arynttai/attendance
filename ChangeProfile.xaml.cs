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
            LoadUserDataAsync(); // �������� ������ ������������
        }

        private async Task LoadUserDataAsync()
        {
            try
            {
                // ��������� ������ ������������
                var user = await _userService.GetUserByUINAsync(_uin, _token);

                // �������������� ���������� �����
                emailEntry.Text = user.Email; // ��������� email
                phoneEntry.Text = user.PhoneNumber; // ��������� ����� ��������
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChangeProfile] Error loading user data: {ex.Message}");
                await DisplayAlert("������", "�� ������� ��������� ������ ������������.", "OK");
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                // ���������� ������ ������������
                var updatedUser = new User
                {
                    uin = _uin,
                    Email = emailEntry.Text, // ��������� ������������ Email
                    PhoneNumber = phoneEntry.Text, // ��������� ������������ ������ ��������
                    Role = _role
                };

                // ���������� ���������
                await _userService.UpdateUserAsync(updatedUser, _token);

                // ������� �� �������� �������
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChangeProfile] Error saving user data: {ex.Message}");
                await DisplayAlert("������", "�� ������� ��������� ���������.", "OK");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            // ������� ��� ����������
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
