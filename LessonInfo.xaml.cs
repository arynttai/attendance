using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Npgsql;
using Dapper;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using AC.AC;

namespace AC
{
	public partial class LessonInfo : ContentPage, INotifyPropertyChanged
	{
		private readonly string _role;
		private readonly string _uin;
		private readonly string _token;
		private Lesson _lesson;
		private List<Student> students;
		private List<Student> _originalStudents; // To store the original state
		private bool _isRefreshing = false;
		private bool _isPinCodeVisible;
		private bool _hasUnsavedChanges = false;

		public int StudentCount => students?.Count ?? 0;

		public event PropertyChangedEventHandler PropertyChanged;

		public bool IsPinCodeVisible
		{
			get => _isPinCodeVisible;
			set
			{
				_isPinCodeVisible = value;
				OnPropertyChanged(nameof(IsPinCodeVisible));
			}
		}

		public string LessonDate => _lesson?.StartTime.ToString("dd.MM.yyyy") ?? string.Empty;
		public string LessonStartTime => _lesson?.StartTime.ToString("HH:mm") ?? string.Empty;
		public string LessonEndTime => _lesson?.EndTime.ToString("HH:mm") ?? string.Empty;
		public string Group => _lesson?.Group ?? string.Empty;
		public string Room => _lesson?.Room ?? string.Empty;
		public string PinCode => _lesson?.PinCode ?? string.Empty;

		public string LessonTimeRange => $"{LessonStartTime} - {LessonEndTime}";

		public LessonInfo(string role, string uin, string lessonId)
		{
			InitializeComponent();
			_role = role;
			_uin = uin;

			this.BindingContext = this;
			this.Appearing += OnPageAppearing;

			Debug.WriteLine("[INFO] Initializing LessonInfo page.");

			LoadLessonDataAndStartAutoRefresh(lessonId).ConfigureAwait(false);
		}

		private async Task LoadLessonDataAndStartAutoRefresh(string lessonId)
		{
			await LoadLessonDataAsync(lessonId);
			StartAutoRefresh();
		}

		private async void OnPageAppearing(object sender, EventArgs e)
		{
			Debug.WriteLine("[INFO] Page appearing.");
			OnPropertyChanged(nameof(LessonDate));
			OnPropertyChanged(nameof(LessonStartTime));
			OnPropertyChanged(nameof(LessonEndTime));
			OnPropertyChanged(nameof(Group));
			OnPropertyChanged(nameof(Room));
			OnPropertyChanged(nameof(LessonTimeRange));
		}

		private SemaphoreSlim _refreshSemaphore = new SemaphoreSlim(1, 1);

		private async void StartAutoRefresh()
		{
			while (true)
			{
				if (!_isRefreshing)
				{
					await _refreshSemaphore.WaitAsync();
					try
					{
						_isRefreshing = true;
						await LoadStudentDataAsync();
					}
					finally
					{
						_isRefreshing = false;
						_refreshSemaphore.Release();
					}
				}
				await Task.Delay(15000);
			}
		}

		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private async Task LoadLessonDataAsync(string lessonId)
		{
			try
			{
				Debug.WriteLine("[INFO] Loading lesson by lessonId.");
				await LoadLessonByLessonId(lessonId);
				Debug.WriteLine("[INFO] Verifying PIN code.");
				await VerifyPinCodeAsync();
				Debug.WriteLine("[INFO] Initializing page.");
				await InitializePageAsync();
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"[ERROR] {ex.Message}");
				await DisplayAlert("Ошибка", "Произошла ошибка при загрузке данных.", "OK");
			}
		}

		public async Task<Lesson> GetLessonByIdAsync(string lessonId)
		{
			var lesson = await GetLessonFromDatabaseAsync(lessonId);

			if (lesson == null)
			{
				Debug.WriteLine($"[ERROR] Lesson with ID {lessonId} not found.");
			}
			else
			{
				Debug.WriteLine($"[DEBUG] Retrieved Lesson: ID: {lesson.LessonId}, PinCode: {lesson.PinCode}");
			}

			return lesson;
		}

		private async Task LoadLessonByLessonId(string lessonId)
		{
			_lesson = await GetLessonByIdAsync(lessonId);

			if (_lesson == null)
			{
				await DisplayAlert("Ошибка", "Не удалось найти урок в базе данных.", "OK");
				await Navigation.PopAsync();
				return;
			}

			Debug.WriteLine($"[INFO] Lesson loaded: {_lesson.LessonId}, PinCode: {_lesson.PinCode}");

			OnPropertyChanged(nameof(LessonDate));
			OnPropertyChanged(nameof(LessonStartTime));
			OnPropertyChanged(nameof(LessonEndTime));
			OnPropertyChanged(nameof(Group));
			OnPropertyChanged(nameof(Room));
			OnPropertyChanged(nameof(LessonTimeRange));
			OnPropertyChanged(nameof(PinCode));
		}

		private async Task VerifyPinCodeAsync()
		{
			string enteredPinCode = await DisplayPromptAsync("Введите PIN код", "Для доступа к уроку введите PIN код:");

			if (enteredPinCode == _lesson.PinCode)
			{
				IsPinCodeVisible = true;
			}
			else
			{
				await DisplayAlert("Ошибка", "Неправильный PIN код.", "OK");
				await Navigation.PopAsync();
			}
		}

		private async Task InitializePageAsync()
		{
			await LoadStudentDataAsync();
			if (_role == "student")
			{
				await UpdateStudentAttendanceAsync();
			}
		}

		private async Task<Lesson> GetLessonFromDatabaseAsync(string lessonId)
		{
			try
			{
				const string connectionString = "Host=dpg-csogsqggph6c73braemg-a.oregon-postgres.render.com;Port=5432;Username=delechka;Password=ZSQ5jHTFX2kfJy35JkfxobQ0qYh6ymGG;Database=attendance_9s8z;SslMode=Require;Trust Server Certificate=true";

				using (var connection = new NpgsqlConnection(connectionString))
				{
					await connection.OpenAsync();
					string query = @"SELECT lessonid, teacher, starttime AS StartTime, endtime AS EndTime, 
                             room, ""group"" AS Group, description, pincode AS PinCode, teacheruin AS TeacherUIN 
                             FROM lessons WHERE lessonid = @LessonId";
					return await connection.QueryFirstOrDefaultAsync<Lesson>(query, new { LessonId = lessonId });
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"[ERROR] Failed to get lesson: {ex.Message}");
				throw;
			}
		}

		public async Task LoadStudentDataAsync()
		{
			var attendedStudents = await GetStudentsByLessonIdAsync(_lesson.LessonId);

			students = new List<Student>();

			foreach (var student in attendedStudents)
			{
				student.Status = student.Status ?? "red";
				students.Add(student);
			}

			AssignAttendanceStatus();
			UpdateStudentList();
			OnPropertyChanged(nameof(StudentCount));

			// Create a deep copy for the original state
			_originalStudents = new List<Student>(students.Select(s => new Student
			{
				UIN = s.UIN,
				Name = s.Name,
				ScanTime = s.ScanTime,
				Status = s.Status,
				IsStatusManuallyOverridden = s.IsStatusManuallyOverridden,
				Comment = s.Comment
			}));

			_hasUnsavedChanges = false;
		}

		private async Task<List<Student>> GetStudentsByLessonIdAsync(string lessonId)
		{
			const string connectionString = "Host=dpg-csogsqggph6c73braemg-a.oregon-postgres.render.com;Port=5432;Username=delechka;Password=ZSQ5jHTFX2kfJy35JkfxobQ0qYh6ymGG;Database=attendance_9s8z;SslMode=Require;Trust Server Certificate=true";

			using (var connection = new NpgsqlConnection(connectionString))
			{
				await connection.OpenAsync();

				string query = @"
            SELECT u.uin AS UIN, 
                   CONCAT(u.last_name, ' ', u.first_name, ' ', u.patronymic) AS Name, 
                   s.scantime AS ScanTime, 
                   s.status AS Status,
                   s.is_status_manually_overridden AS IsStatusManuallyOverridden
            FROM student_attendance s
            INNER JOIN users u 
                ON u.uin = s.uin
            WHERE s.lessonid = @LessonId
            AND u.group = @Group";

				var students = await connection.QueryAsync<Student>(query, new { LessonId = lessonId, Group = _lesson.Group });
				return students.AsList();
			}
		}

		private void AssignAttendanceStatus()
		{
			foreach (var student in students)
			{
				if (student.IsStatusManuallyOverridden)
				{
					continue;
				}

				if (!student.ScanTime.HasValue)
				{
					student.Status = "red";
				}
				else if (student.Status == null)
				{
					student.Status = "yellow";
				}
			}
		}

		private void UpdateStudentList()
		{
			StudentsStackLayout.Children.Clear();

			if (students == null || students.Count == 0)
			{
				var noStudentsLabel = new Label
				{
					Text = "Ни один студент не присоединился к уроку.",
					FontSize = 16,
					TextColor = Color.FromArgb("#e3ccff"),
					HorizontalOptions = LayoutOptions.Center,
					VerticalOptions = LayoutOptions.Center
				};

				StudentsStackLayout.Children.Add(noStudentsLabel);
			}
			else
			{
				foreach (var student in students)
				{
					StudentsStackLayout.Children.Add(CreateStudentFrame(student));
				}
			}

			Debug.WriteLine("[INFO] Student list updated.");
		}

		private Frame CreateStudentFrame(Student student)
		{
			var grid = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) },
					new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
					new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
					new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }
				},
				Padding = new Thickness(10),
				BackgroundColor = Color.FromArgb("#333")
			};

			var nameLabel = new Label
			{
				Text = student.Name,
				TextColor = Color.FromArgb("#FFFFFF"),
				FontSize = 16,
				VerticalOptions = LayoutOptions.Center,
				Padding = new Thickness(10, 0, 0, 0)
			};

			var uinLabel = new Label
			{
				Text = student.UIN,
				FontSize = 13,
				VerticalOptions = LayoutOptions.Center,
				TextColor = Color.FromArgb("#CCCCCC"),
				Padding = new Thickness(10, 0, 0, 0)
			};

			var statusLabel = new Label
			{
				Text = student.Status == "red" ? "Отсутствует" : "Присутствует",
				TextColor = student.Status == "red" ? Color.FromArgb("#FF0000") : Color.FromArgb("#00FF00"),
				FontSize = 16,
				VerticalOptions = LayoutOptions.Center,
				HorizontalOptions = LayoutOptions.End,
				Padding = new Thickness(0, 0, 10, 0)
			};

			var timeLabel = new Label
			{
				Text = student.ScanTime.HasValue ? student.ScanTime.Value.ToString("HH:mm") : "Не отмечен",
				FontSize = 14,
				VerticalOptions = LayoutOptions.Center,
				HorizontalOptions = LayoutOptions.End,
				TextColor = Color.FromArgb("#AAAAAA"),
				Padding = new Thickness(0, 0, 10, 0)
			};

			grid.Children.Add(nameLabel);
			Grid.SetColumn(nameLabel, 0);

			grid.Children.Add(uinLabel);
			Grid.SetColumn(uinLabel, 1);

			grid.Children.Add(statusLabel);
			Grid.SetColumn(statusLabel, 2);

			grid.Children.Add(timeLabel);
			Grid.SetColumn(timeLabel, 3);

			var studentFrame = new Frame
			{
				Content = grid,
				CornerRadius = 10,
				BorderColor = Color.FromArgb("#666666"),
				BackgroundColor = Color.FromArgb("#444444"),
				Padding = new Thickness(0),
				Margin = new Thickness(0, 5)
			};

			var tapGestureRecognizer = new TapGestureRecognizer();
			tapGestureRecognizer.Tapped += (s, e) =>
			{
				ChangeStudentStatus(student, statusLabel);
			};
			studentFrame.GestureRecognizers.Add(tapGestureRecognizer);

			return studentFrame;
		}

		private void ChangeStudentStatus(Student student, Label statusLabel)
		{
			if (_role != "teacher")
			{
				DisplayAlert("Доступ запрещен", "Только преподаватель может менять статус студента.", "OK");
				return;
			}

			if (student.Status == "green")
			{
				student.Status = "yellow";
			}
			else if (student.Status == "yellow")
			{
				student.Status = "red";
			}
			else if (student.Status == "red")
			{
				student.Status = "green";
			}
			else
			{
				student.Status = "green";
			}

			statusLabel.Text = student.Status switch
			{
				"green" => "Присутствует",
				"yellow" => "Предупреждение",
				"red" => "Отсутствует",
				_ => "Присутствует"
			};

			statusLabel.TextColor = GetColorFromStatus(student.Status);

			_hasUnsavedChanges = true;
		}

		private Color GetColorFromStatus(string status)
		{
			return status switch
			{
				"green" => Color.FromArgb("#00FF00"),
				"yellow" => Color.FromArgb("#FFFF00"), // Yellow for "Предупреждение"
				"red" => Color.FromArgb("#FF0000"),    // Red for "Отсутствует"
				"" => Color.FromArgb("#00FF00"),
			};
		}

		private async Task SaveAttendanceAsync()
		{
			const string connectionString = "Host=dpg-csogsqggph6c73braemg-a.oregon-postgres.render.com;Port=5432;Username=delechka;Password=ZSQ5jHTFX2kfJy35JkfxobQ0qYh6ymGG;Database=attendance_9s8z;SslMode=Require;Trust Server Certificate=true";

			using (var connection = new NpgsqlConnection(connectionString))
			{
				await connection.OpenAsync();

				foreach (var student in students)
				{
					string updateQuery = @"
                UPDATE student_attendance
                SET status = @Status, 
                    is_status_manually_overridden = @IsStatusManuallyOverridden
                WHERE uin = @UIN AND lessonid = @LessonId";

					await connection.ExecuteAsync(updateQuery, new
					{
						Status = student.Status,
						IsStatusManuallyOverridden = student.IsStatusManuallyOverridden,
						UIN = student.UIN,
						LessonId = _lesson.LessonId
					});
				}
			}
		}

		private async void ShowCommentDialog(Student student)
		{
			if (_role != "teacher")
			{
				await DisplayAlert("Доступ запрещен", "Только преподаватель может добавлять комментарии.", "OK");
				return;
			}

			string comment = await DisplayPromptAsync("Комментарий", "Введите комментарий для студента:", "OK", "Отмена", "Комментарий");

			if (!string.IsNullOrWhiteSpace(comment))
			{
				student.Comment = comment;
				await SaveCommentAsync(student);
			}
		}

		private async Task SaveCommentAsync(Student student)
		{
			const string connectionString = "Host=dpg-csogsqggph6c73braemg-a.oregon-postgres.render.com;Port=5432;Username=delechka;Password=ZSQ5jHTFX2kfJy35JkfxobQ0qYh6ymGG;Database=attendance_9s8z;SslMode=Require;Trust Server Certificate=true";


			using (var connection = new NpgsqlConnection(connectionString))
			{
				await connection.OpenAsync();

				string updateQuery = @"
                UPDATE student_attendance
                SET comment = @Comment
                WHERE uin = @UIN AND lessonid = @LessonId";

				await connection.ExecuteAsync(updateQuery, new
				{
					Comment = student.Comment,
					UIN = student.UIN,
					LessonId = _lesson.LessonId
				});
			}
		}



		private async Task UpdateStudentAttendanceAsync()
		{
			const string connectionString = "Host=dpg-csogsqggph6c73braemg-a.oregon-postgres.render.com;Port=5432;Username=delechka;Password=ZSQ5jHTFX2kfJy35JkfxobQ0qYh6ymGG;Database=attendance_9s8z;SslMode=Require;Trust Server Certificate=true";

			using (var connection = new NpgsqlConnection(connectionString))
			{
				await connection.OpenAsync();

				Debug.WriteLine($"[DEBUG] Attempting to update attendance for UIN: {_uin}");

				// Get UTC time from the system
				DateTime utcNow = DateTime.UtcNow;
				// Convert UTC time to Astana time (Asia/Almaty)
				TimeZoneInfo astanaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Almaty");
				DateTime astanaTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, astanaTimeZone);

				Debug.WriteLine($"[INFO] Current time in Astana: {astanaTime}");

				string checkQuery = @"
SELECT COUNT(*)
FROM student_attendance
WHERE uin = @UIN AND lessonid = @LessonId";

				int attendanceCount = await connection.ExecuteScalarAsync<int>(checkQuery, new { UIN = _uin, LessonId = _lesson.LessonId });

				if (attendanceCount == 0)
				{
					string insertQuery = @"
INSERT INTO student_attendance (lessonid, uin, scantime, status)
VALUES (@LessonId, @UIN, @ScanTime, @Status)";

					// Insert record with Astana time
					await connection.ExecuteAsync(insertQuery, new
					{
						LessonId = _lesson.LessonId,
						UIN = _uin,
						ScanTime = astanaTime, // Use the correctly converted Astana time
						Status = "green"
					});

					Debug.WriteLine($"[INFO] Student {_uin} marked as present with new record.");
				}
				else
				{
					string updateQuery = @"
UPDATE student_attendance
SET scantime = @ScanTime, status = @Status
WHERE uin = @UIN AND lessonid = @LessonId";

					// Update record with Astana time
					await connection.ExecuteAsync(updateQuery, new
					{
						ScanTime = astanaTime, // Use the correctly converted Astana time
						Status = "green",
						UIN = _uin,
						LessonId = _lesson.LessonId
					});

					Debug.WriteLine($"[INFO] Student {_uin} attendance record updated.");
				}

				// Update student list locally
				var student = students.FirstOrDefault(s => s.UIN == _uin);
				if (student != null)
				{
					student.Status = "green"; // Mark the student's status as green
					student.ScanTime = astanaTime; // Set the scan time to the converted Astana time
				}

				// Refresh the student list
				UpdateStudentList();
			}
		}




		private async void OnSaveBtn(object sender, EventArgs e)
		{
			await SaveAttendanceAsync();

			foreach (var student in students)
			{
				await SavePerformanceAsync(student);
			}

			await DisplayAlert("Сохранено", "Данные об успеваемости сохранены.", "OK");
		}

		private async Task SavePerformanceAsync(Student student)
		{
			if (_role != "teacher")
			{
				await DisplayAlert("Доступ запрещен", "Только преподаватель может сохранять успеваемость.", "OK");
				return;
			}

			const string connectionString = "Host=dpg-csogsqggph6c73braemg-a.oregon-postgres.render.com;Port=5432;Username=delechka;Password=ZSQ5jHTFX2kfJy35JkfxobQ0qYh6ymGG;Database=attendance_9s8z;SslMode=Require;Trust Server Certificate=true";

			using (var connection = new NpgsqlConnection(connectionString))
			{
				await connection.OpenAsync();

				string query = @"
            INSERT INTO student_attendance (lessonid, uin, status, scantime, is_status_manually_overridden)
            VALUES (@LessonId, @UIN, @Status, @ScanTime, @IsStatusManuallyOverridden)
            ON CONFLICT (lessonid, uin) DO UPDATE
            SET status = EXCLUDED.status, scantime = EXCLUDED.scantime, 
                is_status_manually_overridden = EXCLUDED.is_status_manually_overridden";

				await connection.ExecuteAsync(query, new
				{
					LessonId = _lesson.LessonId,
					UIN = student.UIN,
					Status = student.Status,
					ScanTime = student.ScanTime ?? DateTime.Now,
					IsStatusManuallyOverridden = student.IsStatusManuallyOverridden
				});

				Debug.WriteLine($"[INFO] Performance saved for student {student.Name}: {student.Status}");
			}
		}

		private async void GoBack(object sender, EventArgs e)
		{
			await Navigation.PopAsync();
		}

		private async void OnDesktopClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Desktop(_role, _uin , _token));
		}

		private async void OnStatisticsClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Statistics(_role, _uin , _token));
		}

		private async void OnProfileClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Profile(_role, _uin , _token));
		}

		public class Student
		{
			public string UIN { get; set; }
			public string Name { get; set; }
			public DateTime? ScanTime { get; set; }
			public string Status { get; set; }
			public bool IsStatusManuallyOverridden { get; set; }
			public string Comment { get; set; }
		}
	}
}
