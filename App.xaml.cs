using AC;
using Microsoft.Maui.Controls;
using Microsoft.Maui;
using Microsoft.Maui.Storage;

namespace AC
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            string token = Preferences.Get("auth_token", null);
            string role = Preferences.Get("user_role", null);
            string uin = Preferences.Get("user_uin", null);

            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(role) && !string.IsNullOrEmpty(uin))
            {
                // Если токен есть, переходим сразу на Desktop
                MainPage = new NavigationPage(new Desktop(role, uin, token));
            }
            else
            {
                // Иначе показываем страницу входа
                MainPage = new NavigationPage(new StartPage());
            }
        }

    }
}
