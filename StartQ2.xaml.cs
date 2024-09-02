namespace AC
{
	public partial class StartQ2 : ContentPage
	{
		private readonly string role;

		public StartQ2(string _role)
		{
			InitializeComponent();
			this.role = _role;
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

			// Передача данных на следующую страницу
			await Navigation.PushAsync(new StartQ3(role, lastName, firstName, patronymic));
		}
	}

}
