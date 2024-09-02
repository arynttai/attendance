namespace AC;

public partial class StartQP2 : ContentPage
{
	private readonly string role;

	public StartQP2(string role)
	{
		InitializeComponent();
		this.role = role;
	}

	private async void GoBack(object sender, EventArgs e)
	{
		await Navigation.PopAsync();
	}

	private async void OnNextClicked(object sender, EventArgs e)
	{
		string lastName = LastNameEntry.Text;
		string firstName = FirstNameEntry.Text;
		string patronymic = PatronymicEntry.Text;

		// Переход к следующей странице
		await Navigation.PushAsync(new StartQ3(role, lastName, firstName, patronymic));
	}
}
