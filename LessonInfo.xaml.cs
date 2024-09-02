using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Npgsql;
using Dapper;
using System.ComponentModel;
using System.Diagnostics;

namespace AC
{
	public partial class LessonInfo : ContentPage, INotifyPropertyChanged
	{
		private readonly string _role;
		private readonly string _uin;
		private Lesson _lesson;
		private List<Student> students;
		private bool _isPinCodeVisible;

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

		// Properties for binding with 24-hour time format
		public string LessonDate => _lesson?.StartTime.ToString("dd.MM.yyyy") ?? string.Empty;
		public string LessonStartTime => _lesson?.StartTime.ToString("HH:mm") ?? string.Empty; // 24-hour format
		public string LessonEndTime => _lesson?.EndTime.ToString("HH:mm") ?? string.Empty;     // 24-hour format
		public string Group => _lesson?.Group ?? string.Empty;
		public string Room => _lesson?.Room ?? string.Empty;
		public string PinCode => _lesson?.PinCode ?? string.Empty; // Display the pin code

		public string LessonTimeRange => $"{LessonStartTime} - {LessonEndTime}";

		public LessonInfo(string role, string uin, string lessonId)
		{
			InitializeComponent();
			_role = role;
			_uin = uin;

			this.BindingContext = this;
			this.Appearing += OnPageAppearing;

			Debug.WriteLine("[INFO] Initializing LessonInfo page.");

			// Load the lesson and student data asynchronously
			LoadLessonDataAsync(lessonId).ConfigureAwait(false);
		}

		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

		private async Task LoadLessonByLessonId(string lessonId)
		{
			_lesson = await GetLessonFromDatabaseAsync(lessonId);

			if (_lesson == null)
			{
				await DisplayAlert("Ошибка", "Не удалось найти урок в базе данных.", "OK");
				await Navigation.PopAsync();
				return;
			}

			Debug.WriteLine($"[INFO] Lesson loaded: {_lesson}");

			// Update binding properties
			OnPropertyChanged(nameof(LessonDate));
			OnPropertyChanged(nameof(LessonStartTime));
			OnPropertyChanged(nameof(LessonEndTime));
			OnPropertyChanged(nameof(Group));
			OnPropertyChanged(nameof(Room));
			OnPropertyChanged(nameof(LessonTimeRange));
		}

		private async Task VerifyPinCodeAsync()
		{
			string enteredPinCode = await DisplayPromptAsync("Введите PIN код", "Для доступа к уроку введите PIN код:");

			if (enteredPinCode == _lesson.PinCode)
			{
				IsPinCodeVisible = true; // Show the PinCode after successful verification
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
				const string connectionString = "Host=localhost;Port=5433;Username=postgres;Password=1234;Database=my_database";
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
			students = await GetStudentsByLessonIdAsync(_lesson.LessonId);
			Debug.WriteLine($"[INFO] Students loaded: {students.Count}");
			AssignAttendanceStatus();
			UpdateStudentList();
			OnPropertyChanged(nameof(StudentCount));
		}


		private async Task<List<Student>> GetStudentsByLessonIdAsync(string lessonId)
		{
			const string connectionString = "Host=localhost;Port=5433;Username=postgres;Password=1234;Database=my_database";

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
					continue; // Use the manually overridden status if set
				}

				// The status from the database is used directly without recalculating
				if (!student.ScanTime.HasValue)
				{
					student.Status = "red"; // Mark as absent if no scan time is recorded
				}
				else if (student.Status == null)
				{
					// Fallback logic if the status was somehow not set
					student.Status = "yellow";
				}
			}
		}


		private async Task SaveAttendanceAsync()
		{
			if (_role != "teacher")
			{
				await DisplayAlert("Доступ запрещен", "Только преподаватель может сохранять данные об успеваемости.", "OK");
				return;
			}

			const string connectionString = "Host=localhost;Port=5433;Username=postgres;Password=1234;Database=my_database";

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
			new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) },
			new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }
		}
			};

			var nameLabel = new Label
			{
				Text = student.Name,
				TextColor = Color.FromArgb("#FFFFFF"),  // Light color for better contrast
				FontSize = 16,
				VerticalOptions = LayoutOptions.Center,
				Padding = new Thickness(10, 0, 0, 0)
			};

			var uinLabel = new Label
			{
				Text = student.UIN,
				FontSize = 13,
				VerticalOptions = LayoutOptions.Center,
				TextColor = Color.FromArgb("#FFFFFF")  // Light color for better contrast
			};

			var scanTimeLabel = new Label
			{
				Text = student.ScanTime?.ToString("g") ?? "N/A",
				FontSize = 13,
				VerticalOptions = LayoutOptions.Center,
				TextColor = Color.FromArgb("#FFFFFF")  // Light color for better contrast
			};

			if (_role == "teacher")
			{
				nameLabel.GestureRecognizers.Add(new TapGestureRecognizer
				{
					Command = new Command(() =>
					{
						ChangeStudentStatus(student, nameLabel);
					})
				});

				var commentButton = new ImageButton
				{
					Source = "comment.png",
					WidthRequest = 30,
					HeightRequest = 30,
					VerticalOptions = LayoutOptions.Center,
					HorizontalOptions = LayoutOptions.End,
					Margin = 5,
					Command = new Command(() => ShowCommentDialog(student))
				};

				grid.Children.Add(commentButton);
				Grid.SetColumn(commentButton, 1);
			}

			grid.Children.Add(nameLabel);
			Grid.SetColumn(nameLabel, 0);
			grid.Children.Add(uinLabel);
			Grid.SetColumn(uinLabel, 1);
			grid.Children.Add(scanTimeLabel);
			Grid.SetColumn(scanTimeLabel, 2);

			return new Frame
			{
				Content = grid,
				BorderColor = GetColorFromStatus(student.Status),
				CornerRadius = 10,
				Padding = new Thickness(10),
				Margin = new Thickness(0, 5),
				BackgroundColor = Color.FromArgb("#23034a")  // Dark purple color
			};
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
			const string connectionString = "Host=localhost;Port=5433;Username=postgres;Password=1234;Database=my_database";

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

		private void ChangeStudentStatus(Student student, Label nameLabel)
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
			else
			{
				student.Status = "green";
			}

			student.IsStatusManuallyOverridden = true;

			nameLabel.TextColor = GetColorFromStatus(student.Status);

			// Save status change and reload student list
			SaveAttendanceAsync().ContinueWith(_ => LoadStudentDataAsync());
		}

		private async Task UpdateStudentAttendanceAsync()
		{
			const string connectionString = "Host=localhost;Port=5433;Username=postgres;Password=1234;Database=my_database";

			using (var connection = new NpgsqlConnection(connectionString))
			{
				await connection.OpenAsync();

				Debug.WriteLine($"[DEBUG] Attempting to update attendance for UIN: {_uin}");

				string checkQuery = @"
        SELECT COUNT(*)
        FROM student_attendance
        WHERE uin = @UIN AND lessonid = @LessonId";

				int attendanceCount = await connection.ExecuteScalarAsync<int>(checkQuery, new { UIN = _uin, LessonId = _lesson.LessonId });

				if (attendanceCount == 0)
				{
					string insertQuery = @"
            INSERT INTO student_attendance (uin, lessonid, scantime, status)
            VALUES (@UIN, @LessonId, @ScanTime, @Status)";

					await connection.ExecuteAsync(insertQuery, new
					{
						UIN = _uin,
						LessonId = _lesson.LessonId,
						ScanTime = DateTime.Now,
						Status = "green"
					});

					Debug.WriteLine($"[INFO] Student {_uin} marked as present.");

					// Reload student list after updating attendance
					await LoadStudentDataAsync();
				}
			}
		}


		private Color GetColorFromStatus(string status)
		{
			return status switch
			{
				"green" => Color.FromArgb("#00ff00"),
				"yellow" => Color.FromArgb("#ffff00"),
				"red" => Color.FromArgb("#ff0000"),
				_ => Color.FromArgb("#000000")
			};
		}

		

		private async void OnSaveBtn(object sender, EventArgs e)
		{
			await SaveAttendanceAsync();
			await DisplayAlert("Сохранено", "Данные об успеваемости сохранены.", "OK");
		}

		private async Task SavePerformanceAsync(Student student)
		{
			if (_role != "teacher")
			{
				await DisplayAlert("Доступ запрещен", "Только преподаватель может сохранять успеваемость.", "OK");
				return;
			}

			const string connectionString = "Host=localhost;Port=5433;Username=postgres;Password=1234;Database=my_database";

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
					ScanTime = student.ScanTime,
					IsStatusManuallyOverridden = student.IsStatusManuallyOverridden
				});
			}

			Debug.WriteLine($"[INFO] Performance saved for student {student.Name}: {student.Status}");
		}

		private async void GoBack(object sender, EventArgs e)
		{
			await Navigation.PopAsync();
		}

		private async void OnDesktopClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Desktop(_role, _uin));
		}

		private async void OnStatisticsClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Statistics(_role, _uin));
		}

		private async void OnProfileClicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new Profile(_role, _uin));
		}

		public class Student
		{
			public string UIN { get; set; }                      // Unique Identification Number
			public string Name { get; set; }                     // Full name of the student
			public DateTime? ScanTime { get; set; }              // The time when the student was scanned (24-hour format)
			public string Status { get; set; }                   // Attendance status (green, yellow, red)
			public bool IsStatusManuallyOverridden { get; set; } // Whether the status was manually overridden
			public string Comment { get; set; }                  // Comment about the student
		}
	}
}
