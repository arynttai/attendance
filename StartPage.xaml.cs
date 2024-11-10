namespace AC;

public partial class StartPage : ContentPage
{
    public StartPage()
    {
        InitializeComponent();
        this.Title = null;
        NavigateToSignInWithDelay();
        NavigationPage.SetHasNavigationBar(this, false);
    }
    private async void NavigateToSignInWithDelay()
    {
        // Задержка на 5 секунд
        await Task.Delay(5000);

        // Переход на страницу SignIn
        await Navigation.PushAsync(new SignIn());
    }
}