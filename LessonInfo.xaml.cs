using Dapper;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using AC;

namespace AC
{
    public partial class LessonInfo : ContentPage
    {
        private const string connectionString = "Host=10.250.0.64;Port=5432;Username=postgres;Password=postgres;Database=attendance;";
        private List<Student> students;
        private bool _hasUnsavedChanges;
        private string _role;
        private string _uin;
        private string _token;
        private Lesson _lesson;  // ��������� ���� ��� �������� roomid

        public LessonInfo(string role, string uin, string token, Lesson lesson)
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
            _role = role;
            _uin = uin;
            _token = token;
            _lesson = lesson;  // ��������� ������ �����
            if (_role == "student")
            {
                saveButton.IsVisible = false;
            }

            // ����������� ������ ����� � BindingContext
            BindingContext = new
            {
                LessonDate = _lesson.StartTime.ToString("dd.MM.yyyy"),
                Room = _lesson.Room,
                PinCode = _lesson.PinCode,
                StartTime = _lesson.StartTime.ToString("HH:mm"),
                EndTime = _lesson.EndTime.ToString("HH:mm")
            };

            LoadData();
        }


        public class RoomInfoViewModel
        {
            public string RoomNumber { get; set; }
        }

        private async void LoadData()
        {
            try
            {
                students = await GetStudentsByLessonIdAsync(_lesson.LessonId);
                if (students == null || students.Count == 0)
                {
                    Debug.WriteLine("[INFO] No students found for this lesson.");
                }
                AssignAttendanceStatus();
                UpdateStudentList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Failed to load data for lesson {_lesson.LessonId}: {ex.Message}");
            }
        }


        // Adjusted the query according to the provided database structure
        private async Task<List<Student>> GetStudentsByLessonIdAsync(string lessonId)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = @"
                    SELECT u.uin AS UIN, 
                           CONCAT(u.last_name, ' ', u.first_name) AS Name, 
                           s.scantime AS ScanTime, 
                           s.status AS Status,
                           s.is_status_manually_overridden AS IsStatusManuallyOverridden
                    FROM student_attendance s
                    INNER JOIN users u ON u.uin = s.uin
                    WHERE s.lessonid = @LessonId AND (s.status = 'green' OR s.status = 'yellow') ORDER BY u.last_name ASC";

                return (await connection.QueryAsync<Student>(query, new { LessonId = lessonId })).AsList();
            }
        }

        private void AssignAttendanceStatus()
        {
            foreach (var student in students)
            {
                if (student.IsStatusManuallyOverridden) continue;

                student.Status = student.ScanTime.HasValue ? (student.Status ?? "yellow") : "red";
            }
        }

        private void UpdateStudentList()
        {
            StudentsStackLayout.Children.Clear();

            if (_role == "student")
            {
                // ������������� Frame
                var successFrame = new Frame
                {
                    BackgroundColor = Color.FromArgb("#E8F5E9"), // ������ ������ (����� �������� �� �����)
                    BorderColor = Color.FromArgb("#4CAF50"),     // ����� ����� ������ ��� �����
                    CornerRadius = 12,                          // ������ ����������
                    Margin = new Thickness(0, 30),              // ������� ������ � �����
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    Padding = new Thickness(10),
                    Content = new Label
                    {
                        Text = "�� ������� ����������!",
                        FontSize = 15,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#4CAF50"),
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center
                    }
                };

                StudentsStackLayout.Children.Add(successFrame);
                Debug.WriteLine("[INFO] Displaying success message for student.");
                return;
            }


            if (students == null || students.Count == 0)
            {
                StudentsStackLayout.Children.Add(new Label
                {
                    Text = "�� ���� ������� �� ������������� � �����.",
                    FontSize = 16,
                    TextColor = Color.FromArgb("#AAADB2"),
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                });
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
            // ������������ ��������� � ���� ��������� ���������
            var studentFrame = new Frame
            {
                CornerRadius = 10,
                BorderColor = Color.FromArgb("#666666"),
                BackgroundColor = Color.FromArgb("#FFFFFF"), // ������� ���
                Padding = new Thickness(10),
                Margin = new Thickness(0, 5)
            };

            // �������� ����� ��� ������������ ����� (3 �������)
            var grid = new Grid
            {
                ColumnDefinitions =
        {
            new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) }, // ���
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }, // ������ �������
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }  // �����
        },
                ColumnSpacing = 5,
                RowSpacing = 0
            };

            // ��� ��������
            var nameLabel = CreateLabel(student.Name, 13, "#201F1F");

            // ������ BoxView � ���� ��������
            var statusColor = student.Status switch
            {
                "red" => "#FF0000",   // �������
                "green" => "#00BF00",   // ������
                "yellow" => "#FFD700",   // Ƹ����
                _ => "#00BF00"    // �� ��������� ������
            };

            var statusCircle = new BoxView
            {
                WidthRequest = 15,
                HeightRequest = 15,
                CornerRadius = 7.5f,            // �������� Width/Height => ����
                Color = Color.FromArgb(statusColor),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            // ����� ������������
            var timeLabel = CreateLabel(student.ScanTime?.ToString("HH:mm") ?? "�", 13, "#7D7D7D");
            timeLabel.HorizontalOptions = LayoutOptions.End;

            // ��������� �������� � �����
            // 1) ��� (������� 0)
            Grid.SetRow(nameLabel, 0);
            Grid.SetColumn(nameLabel, 0);
            grid.Children.Add(nameLabel);

            // 2) ������� �������� (������� 1)
            Grid.SetRow(statusCircle, 0);
            Grid.SetColumn(statusCircle, 1);
            grid.Children.Add(statusCircle);

            // 3) ����� (������� 2)
            Grid.SetRow(timeLabel, 0);
            Grid.SetColumn(timeLabel, 2);
            grid.Children.Add(timeLabel);

            // ���������� ����� � Frame
            studentFrame.Content = grid;

            // ��������� ���������� �������, ���� ����� ������ ������ ��� ����
            studentFrame.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() => ChangeStudentStatus(student, statusCircle))
            });

            return studentFrame;
        }


        private Label CreateLabel(string text, int fontSize, string textColor)
        {
            return new Label
            {
                Text = text,
                FontSize = fontSize,
                TextColor = Color.FromArgb(textColor),
                VerticalOptions = LayoutOptions.Center,
                Padding = new Thickness(10, 0, 0, 0)
            };
        }

        private void ChangeStudentStatus(Student student, BoxView statusCircle)
        {
            if (_role != "teacher")
            {
                DisplayAlert("������ ��������", "������ ������������� ����� ������ ������ ��������.", "OK");
                return;
            }

            // ������ ������ �� ����� green?yellow?red?green
            var newStatus = student.Status switch
            {
                "green" => "yellow",
                "yellow" => "red",
                "red" => "green",
                _ => "green"
            };

            student.Status = newStatus;

            // ��������� ���� ������ ��������
            var newColor = newStatus switch
            {
                "green" => "#00BF00",  // ������
                "yellow" => "#FFD700",  // ����� (������)
                "red" => "#FF0000",  // �������
                _ => "#00BF00"
            };

            // ��������� � ������ BoxView
            statusCircle.Color = Color.FromArgb(newColor);

            // ���������, ��� ���� ������������ ���������
            _hasUnsavedChanges = true;
        }


        private Color GetColorFromStatus(string status)
        {
            return status switch
            {
                "green" => Color.FromArgb("#00FF00"),
                "yellow" => Color.FromArgb("#FFFF00"),
                "red" => Color.FromArgb("#FF0000"),
                _ => Color.FromArgb("#00FF00")
            };
        }

        private async Task SaveAttendanceAsync()
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();

                foreach (var student in students)
                {
                    string updateQuery = @"
                        UPDATE student_attendance
                        SET status = @Status, is_status_manually_overridden = @IsStatusManuallyOverridden
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

        private async Task SavePerformanceAsync(Student student)
        {
            if (_role != "teacher")
            {
                await DisplayAlert("������ ��������", "������ ������������� ����� ��������� ������������.", "OK");
                return;
            }

            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = @"
                    INSERT INTO student_attendance (lessonid, uin, status, scantime, is_status_manually_overridden)
                    VALUES (@LessonId, @UIN, @Status, @ScanTime, @IsStatusManuallyOverridden)
                    ON CONFLICT (lessonid, uin) DO UPDATE
                    SET status = EXCLUDED.status, scantime = EXCLUDED.scantime, is_status_manually_overridden = EXCLUDED.is_status_manually_overridden";

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

        private async void OnSaveBtn(object sender, EventArgs e)
        {
            // ��������� ������ ������������ � ������������
            await SaveAttendanceAsync();

            foreach (var student in students)
            {
                await SavePerformanceAsync(student);
            }

            await DisplayAlert("���������", "������ �� ������������ ���������.", "OK");

            // �������� ��������� � RoomInfo, ������� ���������� � PIN-����
            var isPinRequired = _role != "teacher"; // ��� �������������� PIN �� ���������
            await Navigation.PushAsync(new RoomInfo(_role, _uin, _lesson.Room, _token));
        }


        private async void GoBack(object sender, EventArgs e) => await Navigation.PopAsync();
        private async void OnDesktopClicked(object sender, EventArgs e) => await Navigation.PushAsync(new Desktop(_role, _uin, _token));
        private async void OnStatisticsClicked(object sender, EventArgs e) => await Navigation.PushAsync(new Statistics(_role, _uin, _token));
        private async void OnProfileClicked(object sender, EventArgs e) => await Navigation.PushAsync(new Profile(_role, _uin, _token));

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
