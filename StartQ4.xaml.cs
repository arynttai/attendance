namespace AC
{
	public partial class StartQ4 : ContentPage
	{
		private readonly string _role;

		public StartQ4(string role)
		{
			InitializeComponent();
			_role = role;
		}

		private async void OnStartClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Desktop(_role));
		}

		private async void GoBack(object sender, EventArgs e)
		{
			await Navigation.PopAsync();
		}
	}
}
