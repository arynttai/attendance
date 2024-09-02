using AC;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace AC
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new NavigationPage(new StartPage());
        }

        //protected override void OnStart()
        //{
        //    base.OnStart();
        //    NavigateToInitialPage();
        //}
        //private void NavigateToInitialPage()
        //{
        //    bool isLoggedIn = Preferences.Get("IsLoggedIn", false);

        //    if (isLoggedIn)
        //    {
        //        // Если пользователь уже вошел, сразу переходим на главную страницу
        //        MainPage = new AppShell();
        //    }
        //    else
        //    {
        //        // Иначе показываем страницу входа
        //        MainPage = new NavigationPage(new StartPage());
        //    }
        //}
    }
}
