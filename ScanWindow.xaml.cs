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
		private string _uin;
		private bool _isProcessing;
		private string lessonId;

		public ScanWindow(string role, string uin)
		{
			InitializeComponent();

			_role = role;
			_uin = Preferences.Get("UserUIN", uin);
			_isProcessing = false;

			barcodeReader.Options = new ZXing.Net.Maui.BarcodeReaderOptions
			{
				Formats = ZXing.Net.Maui.BarcodeFormat.QrCode,
				AutoRotate = true,
				Multiple = false
			};

			Console.WriteLine($"Initialized ScanWindow with UIN: {_uin} and Role: {_role}");
		}

		private string ExtractFieldFromQRData(string qrData, string fieldName)
		{
			try
			{
				Console.WriteLine($"[DEBUG] Raw QR Data: {qrData}");

				foreach (var line in qrData.Split('\n'))
				{
					if (line.StartsWith(fieldName))
					{
						string fieldValue = line.Split(':').LastOrDefault()?.Trim();
						Console.WriteLine($"[DEBUG] Extracted {fieldName}: {fieldValue}");
						return fieldValue;
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[ERROR] Exception occurred while extracting {fieldName}: {ex.Message}");
			}

			return null;
		}

		private async void barcodeReader_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
		{
			if (_isProcessing)
			{
				return;
			}

			_isProcessing = true;

			try
			{
				var first = e.Results?.FirstOrDefault();
				if (first is null)
				{
					return;
				}

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
					await Navigation.PushAsync(new LessonInfo(_role, _uin, lessonId));
				});
			}
			catch (Exception ex)
			{
				await MainThread.InvokeOnMainThreadAsync(async () =>
				{
					Console.WriteLine($"[ERROR] Exception occurred while processing QR data: {ex.Message}");
					await DisplayAlert("Ошибка", "Произошла ошибка при обработке QR-кода.", "OK");
				});
			}
			finally
			{
				_isProcessing = false;
			}
		}

		private async void OnScanWindowButtonClicked(object sender, EventArgs e)
		{
			try
			{
				await MainThread.InvokeOnMainThreadAsync(async () =>
				{
					await Navigation.PushAsync(new LessonInfo(_role, _uin, lessonId));
				});
			}
			catch (Exception ex)
			{
				await MainThread.InvokeOnMainThreadAsync(async () =>
				{
					await DisplayAlert("Ошибка", "Произошла ошибка при переходе на следующую страницу.", "OK");
				});
			}
		}

		private async void GoBack(object sender, EventArgs e)
		{
			try
			{
				await Navigation.PushAsync(new Desktop(_role, _uin));
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
				await Navigation.PushAsync(new Desktop(_role, _uin));
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
				await Navigation.PushAsync(new Profile(_role, _uin));
			}
			catch (Exception ex)
			{
				await DisplayAlert("Ошибка", "Произошла ошибка при переходе на страницу профиля.", "OK");
			}
		}
	}
}
