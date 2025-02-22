using Microsoft.Maui.Controls;
using ZXing.Net.Maui;
using Npgsql;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Reflection;

namespace AC
{
    public partial class ScanWindow : ContentPage
    {
        private readonly string _role;
        private readonly string _token;
        private string _uin;
        private bool _isProcessing;
        private string roomid;

        // Connection string for your PostgreSQL database
        private readonly string connectionString = "Host=10.250.0.64;Port=5432;Username=postgres;Password=postgres;Database=attendance;";

        public ScanWindow(string role, string uin, string _token)
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
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
                    if (line.StartsWith(fieldName, StringComparison.OrdinalIgnoreCase))
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

        // This method checks if the lesson exists in the database
        private async Task<bool> CheckRoomInDatabase(string roomId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT COUNT(*) FROM audiences WHERE audience_number = @room_id";
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("room_id", roomId);

                        var result = await command.ExecuteScalarAsync();
                        return Convert.ToInt32(result) > 0; // If lesson exists
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Database Error: {ex.Message}");
                await DisplayAlert("Ошибка", "Ошибка при подключении к базе данных.", "OK");
                return false;
            }
        }

        private async void barcodeReader_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
        {
            if (_isProcessing) return; // Prevent duplicate scans
            _isProcessing = true;

            try
            {
                var first = e.Results?.FirstOrDefault();
                if (first is null) return;

                string qrData = first.Value;
                Console.WriteLine($"[DEBUG] Scanned QR Data: {qrData}");

                // Extract lesson ID from the QR code
                roomid = ExtractFieldFromQRData(qrData, "roomid");
                if (string.IsNullOrEmpty(roomid))
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await DisplayAlert("Ошибка", "Неверный QR-код", "OK");
                    });
                    _isProcessing = false;
                    return;
                }

                // Check if the lesson exists in the database
                bool isValidRoom = await CheckRoomInDatabase(roomid);
                if (!isValidRoom)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await DisplayAlert("Ошибка", "Аудитория не найдена в базе данных.", "OK");
                    });
                    _isProcessing = false;
                    return;
                }

                // Disable the barcode reader temporarily to prevent duplicate processing
                barcodeReader.BarcodesDetected -= barcodeReader_BarcodesDetected;

                // Pass roomid to RoomInfo page
                var roomInfoPage = new RoomInfo(_role, _uin, roomid, _token);  // Pass the roomid here
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    try
                    {
                        await Navigation.PushAsync(roomInfoPage);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Failed to navigate to RoomInfo: {ex.Message}");
                        await DisplayAlert("Ошибка", "Не удалось перейти на страницу урока.", "OK");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception: {ex.Message}");
                await DisplayAlert("Ошибка", "Произошла ошибка при обработке QR-кода.", "OK");
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
                await Navigation.PushAsync(new Desktop(_role, _uin, _token));
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
                await Navigation.PushAsync(new Profile(_role, _uin, _token));
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
                barcodeReader.IsDetecting = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception in OnDisappearing: {ex.Message}");
            }
        }
    }
}
