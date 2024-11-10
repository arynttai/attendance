using Microsoft.Maui.Controls;
using ZXing.Net.Maui;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace AC
{
	public partial class ScanWindow : ContentPage
	{
		private readonly string _role;
		private readonly string _token;
		private string _uin;
		private bool _isProcessing;
		private string lessonId;

		public ScanWindow(string role, string uin, string _token)
		{
			InitializeComponent();

			_role = role;
			_uin = Preferences.Get("UserUIN", uin);
			_isProcessing = false;

			barcodeReader.Options = new ZXing.Net.Maui.BarcodeReaderOptions
			{
				Formats = ZXing.Net.Maui.BarcodeFormat.QrCode,
				AutoRotate = true,
				Multiple = false,
				TryHarder = false
			};

			barcodeReader.AutoFocus();

			Device.StartTimer(TimeSpan.FromSeconds(2), () =>
			{
				barcodeReader.AutoFocus();
				return true;
			});
		}

		private string ExtractFieldFromQRData(string qrData, string fieldName)
		{
			try
			{
				foreach (var line in qrData.Split('\n'))
				{
					if (line.StartsWith(fieldName))
					{
						return line.Split(':').LastOrDefault()?.Trim();
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[ERROR] {ex.Message}");
			}

			return null;
		}

		private async void barcodeReader_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
		{
			if (_isProcessing)
				return;

			_isProcessing = true;

			try
			{
				var first = e.Results?.FirstOrDefault();
				if (first is null)
					return;

				string qrData = first.Value;
				lessonId = ExtractFieldFromQRData(qrData, "LessonId");

				if (string.IsNullOrEmpty(lessonId))
				{
					await MainThread.InvokeOnMainThreadAsync(async () =>
					{
						await DisplayAlert("Ошибка", "Неверный QR-код", "OK");
					});
					_isProcessing = false;
					return;
				}

				barcodeReader.BarcodesDetected -= barcodeReader_BarcodesDetected;

				await MainThread.InvokeOnMainThreadAsync(async () =>
				{
					try
					{
						await Navigation.PushAsync(new LessonInfo(_role, _uin, lessonId));
					}
					catch (Exception ex)
					{
						Console.WriteLine($"[ERROR] Failed to navigate to LessonInfo: {ex.Message}");
						await DisplayAlert("Ошибка", "Не удалось перейти на страницу урока.", "OK");
					}
				});

			}
			catch (Exception ex)
			{
				// Log the exception details
				Console.WriteLine($"[ERROR] Exception: {ex.GetType().Name} - {ex.Message}");
				Console.WriteLine($"[ERROR] Stack Trace: {ex.StackTrace}");

				await MainThread.InvokeOnMainThreadAsync(async () =>
				{
					await DisplayAlert("Ошибка", "Произошла ошибка при обработке QR-кода.", "OK");
				});
			}
			finally
			{
				_isProcessing = false;
			}
		}

		private async void OnCaptureButtonClicked(object sender, EventArgs e)
		{
			try
			{
				barcodeReader.IsDetecting = false;
				await Task.Delay(100);
				barcodeReader.IsDetecting = true;
			}
			catch (Exception ex)
			{
				await DisplayAlert("Ошибка", "Не удалось сфотографировать QR-код.", "OK");
				Console.WriteLine($"[ERROR] Exception in OnCaptureButtonClicked: {ex.Message}");
			}
		}

		private async void GoBack(object sender, EventArgs e)
		{
			try
			{
				await Navigation.PushAsync(new Desktop(_role, _uin , _token));
			}
			catch (Exception ex)
			{
				await DisplayAlert("Ошибка", "Произошла ошибка при возврате на главный экран.", "OK");
			}
		}

		private async void OnDesktopClicked(object sender, EventArgs e)
		{
			try
			{
				await Navigation.PushAsync(new Desktop(_role, _uin, _token));
			}
			catch (Exception ex)
			{
				await DisplayAlert("Ошибка", "Произошла ошибка при переходе на главный экран.", "OK");
			}
		}

		private async void OnProfileClicked(object sender, EventArgs e)
		{
			try
			{
				await Navigation.PushAsync(new Profile(_role, _uin , _token));
			}
			catch (Exception ex)
			{
				await DisplayAlert("Ошибка", "Произошла ошибка при переходе на страницу профиля.", "OK");
			}
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			try
			{
				// Restart camera feed if the SurfaceView was invalidated
				barcodeReader.IsDetecting = true;
				barcodeReader.AutoFocus();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[ERROR] Exception in OnAppearing: {ex.Message}");
			}
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();

			try
			{
				// Stop camera feed when the page is no longer active
				barcodeReader.IsDetecting = false;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[ERROR] Exception in OnDisappearing: {ex.Message}");
			}
		}

	}
}
