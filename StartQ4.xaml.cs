using System.Data;
using System.Diagnostics;

namespace AC
{
	public partial class StartQ4 : ContentPage
	{
		//private readonly string _role;
		private string _token;
		private string _uin;

		public StartQ4(/*string role, */ string token, string uin)
		{
			InitializeComponent();
			//_role = role;
			_token = token;
			_uin = uin;

			//Debug.WriteLine($"StartQ4 Initialized with Role: {_role}, Token: {_token}, UIN: {_uin}");
            Debug.WriteLine($"StartQ4 Initialized with Token: {_token}, UIN: {_uin}");

        }


        private async void OnStartClicked(object sender, EventArgs e)
		{
			try
			{
				Debug.WriteLine("Navigating to Desktop...");
				await Navigation.PushAsync(new Desktop(/*_role, */ _uin , _token));
				Debug.WriteLine("Navigation to Desktop succeeded.");
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"[ERROR] Navigation to Desktop failed: {ex.Message}");
				await DisplayAlert("Ошибка", "Произошла ошибка при переходе на Desktop.", "OK");
			}
		}

		private async void GoBack(object sender, EventArgs e)
		{
			await Navigation.PopAsync();
		}
	}
}
