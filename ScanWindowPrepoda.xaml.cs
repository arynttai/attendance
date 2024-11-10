using System;
using System.IO;
using Microsoft.Maui.Controls;
using QRCoder;
using Npgsql;
using ZXing;
using System.Drawing.Imaging;
using AC.AC;
namespace AC
{
	public partial class ScanWindowPrepoda : ContentPage
	{
		private readonly Lesson _lesson;
		private readonly string _pinCode;
		private readonly string _connectionString = "Host=dpg-csogsqggph6c73braemg-a.oregon-postgres.render.com;Port=5432;Username=delechka;Password=ZSQ5jHTFX2kfJy35JkfxobQ0qYh6ymGG;Database=attendance_9s8z;SslMode=Require;Trust Server Certificate=true";
		private string role;
		private string uin;
		private string lessonId;
		private string _token;

		public ScanWindowPrepoda(string role, string uin, string token, Lesson lesson)
		{
			InitializeComponent();
			_lesson = lesson;
			_pinCode = GenerateOrRetrievePinCode();
			this.role = role;
			_token = token;
			this.uin = uin;
			this.lessonId = _lesson.LessonId; // Инициализируем lessonId
		}

		public ScanWindowPrepoda(string role, string uin, string lessonId)
		{
			InitializeComponent();
			this.role = role;
			this.uin = uin;
			this.lessonId = lessonId;
		}

		private string GenerateOrRetrievePinCode()
		{
			if (string.IsNullOrEmpty(_lesson.PinCode))
			{
				_lesson.PinCode = GeneratePinCode();
				SaveLessonToDatabase();
			}

			return _lesson.PinCode;
		}

		private void SaveLessonToDatabase()
		{
			using (var connection = new NpgsqlConnection(_connectionString))
			{
				connection.Open();

				var command = new NpgsqlCommand(
					"INSERT INTO lessons (LessonId, Teacher, StartTime, EndTime, Room, \"group\", Description, PinCode, TeacherUIN) " +
					"VALUES (@LessonId, @Teacher, @StartTime, @EndTime, @Room, @Group, @Description, @PinCode, @TeacherUIN)", connection);

				command.Parameters.AddWithValue("@LessonId", Guid.Parse(_lesson.LessonId));
				command.Parameters.AddWithValue("@Teacher", _lesson.Teacher ?? string.Empty);
				command.Parameters.AddWithValue("@StartTime", _lesson.StartTime);
				command.Parameters.AddWithValue("@EndTime", _lesson.EndTime);
				command.Parameters.AddWithValue("@Room", _lesson.Room);
				command.Parameters.AddWithValue("@Group", (object)_lesson.Group ?? DBNull.Value);
				command.Parameters.AddWithValue("@Description", (object)_lesson.Description ?? DBNull.Value);
				command.Parameters.AddWithValue("@PinCode", _lesson.PinCode);
				command.Parameters.AddWithValue("@TeacherUIN", _lesson.TeacherUIN);

				command.ExecuteNonQuery();
			}
		}

		private void DisplayPinCode(string pinCode)
		{
			// Ensure that the UI update happens on the main thread
			MainThread.BeginInvokeOnMainThread(() =>
			{
				PinCodeLabel.Text = $"PIN-код: {pinCode}";
			});
		}

		private void OnGenerateQRCodeButtonClicked(object sender, EventArgs e)
		{
#if WINDOWS
			// Код для генерации QR-кода на Windows
			string data = GenerateQRData();

			using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
			{
				QRCodeData qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
				using (QRCode qrCode = new QRCode(qrCodeData))
				{
					using (var bitmap = qrCode.GetGraphic(20))
					{
						byte[] bitmapData;
						using (var stream = new MemoryStream())
						{
							bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
							bitmapData = stream.ToArray();
						}

						QRCodeImage.Source = ImageSource.FromStream(() => new MemoryStream(bitmapData));
					}
				}
			}

			// Отобразить PIN-код после генерации QR-кода
			DisplayPinCode(_pinCode);
			Console.WriteLine($"[DEBUG] Generated QR Data: {data}");

#elif ANDROID || IOS || MACCATALYST
    
    DisplayAlert("Используйте десктопную версию", "Для генерации QR-кода перейдите на десктопную версию приложения.", "OK");
#endif
		}


		private string GenerateQRData()
		{
			return $"LessonId: {_lesson.LessonId}\n" +
				   $"Teacher: {_lesson.Teacher}\n" +
				   $"Room: {_lesson.Room}\n" +
				   $"Group: {_lesson.Group}\n" +
				   $"StartTime: {_lesson.StartTime:HH:mm}\n" +
				   $"EndTime: {_lesson.EndTime:HH:mm}\n" +
				   $"Description: {_lesson.Description}";
		}

		private string GeneratePinCode()
		{
			Random random = new Random();
			int pin = random.Next(1000, 10000);
			return pin.ToString("D4");
		}

		private void GoBack(object sender, EventArgs e)
		{
			Navigation.PopAsync();
		}

		private void DisplayLessonStatistics()
		{
			using (var connection = new NpgsqlConnection(_connectionString))
			{
				connection.Open();

				var command = new NpgsqlCommand(
					"SELECT COUNT(*) FROM lesson_attendance WHERE LessonId = @LessonId", connection);

				command.Parameters.AddWithValue("@LessonId", _lesson?.LessonId ?? this.lessonId);

				int attendeesCount = Convert.ToInt32(command.ExecuteScalar());

				DisplayAlert("Statistics", $"Number of attendees: {attendeesCount}", "OK");
			}
		}
	}
}