namespace AC
{
	public partial class Desktop : ContentPage
	{
		private readonly string _role;
		private readonly string _uin;

		public Desktop() : this("defaultRole", "defaultUIN")
		{
		}

		public Desktop(string role, string uin)
		{
			InitializeComponent();
			_role = role;
			_uin = uin;
		}

		public Desktop(string role) : this(role, "defaultUIN")
		{
		}

		private async void OnLessonButtonClicked(object sender, EventArgs e)
		{
			if (_role == "teacher")
			{
				// Navigate to RoomInfo page for teacher role
				await Navigation.PushAsync(new RoomInfo(_role, _uin));
			}
			else
			{
				await Navigation.PushAsync(new ScanWindow(_role, _uin));
			}
		}

		private async void OnStatisticsClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Statistics(_role, _uin));
		}

		private async void OnDesktopClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Desktop(_role, _uin));
		}

		private async void OnProfileClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Profile(_role, _uin));
		}
	}
}
