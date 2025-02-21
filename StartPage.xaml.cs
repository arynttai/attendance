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
        // �������� �� 5 ������
        await Task.Delay(5000);

        // ������� �� �������� SignIn
        await Navigation.PushAsync(new SignIn());
    }
}