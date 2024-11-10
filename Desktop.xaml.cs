using System.Diagnostics;

namespace AC
{
	public partial class Desktop : ContentPage
	{
		private readonly string _role;
		private readonly string _uin;
		private readonly string _token;
		private string role;
		private string uin;
 public Desktop()
        {
            InitializeComponent();
        }

        // Конструктор, принимающий role, uin и token
        public Desktop(string role, string uin, string token)
        {
            InitializeComponent();
            _role = role;
            _uin = uin;
            _token = token;

            Debug.WriteLine($"Desktop Initialized with Role: {_role}, UIN: {_uin}, Token: {_token}");
        }

		public Desktop(string role, string uin)
		{
			InitializeComponent();
			this.role = role;
			this.uin = uin;
		}


		// Pass token and uin to Profile page
		// Переход на страницу Profile
		private async void OnProfileClicked(object sender, EventArgs e)
		{
			var profilePage = new Profile(_role, _uin, _token);
			Debug.WriteLine($"Navigating to Profile with Role: {_role}, UIN: {_uin}, Token: {_token}");
			await Navigation.PushAsync(profilePage);
		}

		// Переход на страницу Statistics
		private async void OnStatisticsClicked(object sender, EventArgs e)
		{
			var statisticsPage = new Statistics(_role, _uin, _token);
			Debug.WriteLine($"Navigating to Statistics with Role: {_role}, UIN: {_uin}, Token: {_token}");
			await Navigation.PushAsync(statisticsPage);
		}

		// Переход на страницу Lesson или ScanWindow
		private async void OnLessonButtonClicked(object sender, EventArgs e)
		{
			if (_role == "teacher")
			{
				var roomInfoPage = new RoomInfo(_role, _uin, _token);
				Debug.WriteLine($"Navigating to RoomInfo with Role: {_role}, UIN: {_uin}, Token: {_token}");
				await Navigation.PushAsync(roomInfoPage);
			}
			else
			{
				var scanWindowPage = new ScanWindow(_role, _uin, _token);
				Debug.WriteLine($"Navigating to ScanWindow with Role: {_role}, UIN: {_uin}, Token: {_token}");
				await Navigation.PushAsync(scanWindowPage);
			}
		}

		private async void OnDesktopClicked(object sender, EventArgs e)
		{
			try
			{
				Debug.WriteLine($"Navigating to Desktop with Role: {_role}, UIN: {_uin}, Token: {_token}");
				await Navigation.PushAsync(new Desktop(_role, _uin, _token)); // Pass token and uin
			}
			catch (Exception ex)
			{
				await DisplayAlert("Ошибка", "Произошла ошибка при переходе на главный экран.", "OK");
			}
		}
	}

}
