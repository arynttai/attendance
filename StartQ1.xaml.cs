namespace AC
{
	public partial class StartQ1 : ContentPage
	{
		private readonly UserService _userService;
		private string role;

		public StartQ1()
		{
			InitializeComponent();
			_userService = new UserService();
		}

		private async void OnStudentSelected(object sender, EventArgs e)
		{
			role = "student";
			await Navigation.PushAsync(new StartQ2(role));
		}

		private async void OnTeacherSelected(object sender, EventArgs e)
		{
			role = "teacher";
			await Navigation.PushAsync(new StartQP2(role));
		}

		private async void GoBack(object sender, EventArgs e)
		{
			await Navigation.PopAsync();
		}
	}
}
