namespace AC;

public partial class StartPage : ContentPage
{
	public StartPage()
	{
		InitializeComponent();
        this.Title = null;
    }
    private async void GoToFirstForm(object sender, EventArgs e)
    {
        // Переход к первой странице вопроса
        await Navigation.PushAsync(new StartQ1());
    }
}