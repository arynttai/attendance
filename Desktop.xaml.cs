using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Maui.Controls;
using Npgsql;
using AC.AC;

namespace AC
{
    public partial class Desktop : ContentPage
    {

        private const string connectionString = "Host=10.250.0.64;Port=5432;Username=postgres;Password=postgres;Database=attendance;";
        private readonly string _role;
        private readonly string _uin;
        private readonly string _token;
        private readonly LessonService _lessonService;
        private string _group;
        public string RoleSpecificText { get; private set; }
        public Desktop()
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
            _lessonService = new LessonService();
        }

        // Конструктор, принимающий role, uin и token
        public Desktop(string role, string uin, string token)
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
            _role = role;
            _uin = uin;
            _token = token;
            _lessonService = new LessonService();
            RoleSpecificText = _role == "teacher"
            ? "Нажмите, чтобы отсканировать QR-код аудитории"
            : "Нажмите, чтобы перейти к событию";
            BindingContext = this;
            Debug.WriteLine($"Desktop Initialized with Role: {_role}, UIN: {_uin}, Token: {_token}");
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_role == "student")
            {
                _group = await GetStudentGroupAsync(_uin);
                Debug.WriteLine($"Loaded group for student: {_group}");
            }
        }

        private async Task<string> GetStudentGroupAsync(string uin)
        {
            const string connectionString = "Host=10.250.0.64;Port=5432;Username=postgres;Password=postgres;Database=attendance;";
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
                SELECT ""group""
                FROM users
                WHERE uin = @Uin";
                    return await connection.QueryFirstOrDefaultAsync<string>(query, new { Uin = uin });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Failed to get group for student: {ex.Message}");
                return null;
            }
        }

        private async Task<Lesson> GetCurrentLessonForGroupAsync(string groupId, DateTime currentTime)
        {
            const string connectionString = "Host=10.250.0.64;Port=5432;Username=postgres;Password=postgres;Database=attendance;";
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
                SELECT lessonid AS LessonId,
                       room AS Room,
                       pincode AS PinCode,
                       starttime AS StartTime,
                       endtime AS EndTime
                FROM lessons
                WHERE ""group"" = @GroupId
                  AND starttime - INTERVAL '15 minutes' <= @CurrentTime
                  AND endtime >= @CurrentTime";

                    var lesson = await connection.QueryFirstOrDefaultAsync<Lesson>(query, new { GroupId = groupId, CurrentTime = currentTime });

                    // Отладочный вывод
                    if (lesson == null)
                    {
                        Debug.WriteLine($"[DEBUG] No lesson found for Group: {groupId}, Current Time: {currentTime}");
                    }
                    else
                    {
                        Debug.WriteLine($"[DEBUG] Found Lesson: {lesson.LessonId}, Room: {lesson.Room}, PinCode: {lesson.PinCode}");
                    }

                    return lesson;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Failed to get current lesson for group: {ex.Message}");
                return null;
            }
        }


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
            try
            {
                if (_role == "teacher")
                {
                    // Проверяем, есть ли текущий урок у преподавателя
                    var currentTime = DateTime.UtcNow.AddHours(5); // UTC+5 условно
                    var currentLesson = await GetCurrentLessonForTeacherAsync(_uin, currentTime);

                    if (currentLesson != null)
                    {
                        // Если есть текущий урок, переходим на LessonInfo
                        Debug.WriteLine($"[DEBUG] Found current lesson for teacher: {currentLesson.LessonId}");
                        await Navigation.PushAsync(new LessonInfo(_role, _uin, _token, currentLesson));
                    }
                    else
                    {
                        // Если нет текущего урока, переходим на окно сканирования
                        var scanWindowPage = new ScanWindow(_role, _uin, _token);
                        Debug.WriteLine($"Navigating to ScanWindow with Role: {_role}, UIN: {_uin}, Token: {_token}");
                        await Navigation.PushAsync(scanWindowPage);
                    }
                }
                else if (_role == "student")
                {
                    // Логика для студента остаётся без изменений
                    string groupId = await GetStudentGroupAsync(_uin);
                    if (string.IsNullOrWhiteSpace(groupId))
                    {
                        await DisplayAlert("Ошибка", "Не удалось определить вашу группу.", "OK");
                        return;
                    }

                    var currentTime = DateTime.UtcNow.AddHours(5); // UTC+5 условно
                    var lesson = await GetCurrentLessonForGroupAsync(groupId, currentTime);

                    if (lesson == null)
                    {
                        await DisplayAlert("Нет уроков", "В данный момент у вас нет доступных уроков.", "OK");
                        return;
                    }

                    string enteredPin = await DisplayPromptAsync("Введите PIN-код", "Введите PIN-код для текущего урока:");
                    if (string.IsNullOrWhiteSpace(enteredPin))
                    {
                        await DisplayAlert("Ошибка", "PIN-код не может быть пустым.", "OK");
                        return;
                    }

                    if (enteredPin != lesson.PinCode)
                    {
                        await DisplayAlert("Ошибка", "Неверный PIN-код. Попробуйте еще раз.", "OK");
                        return;
                    }

                    var scanTime = DateTime.UtcNow.AddHours(5); // UTC+5 условно
                    var lessonStart = lesson.StartTime;
                    var diffMinutes = (scanTime - lessonStart).TotalMinutes;

                    var attendanceStatus = diffMinutes > 0 ? "yellow" : "green";

                    await MarkAttendanceAsync(lesson.LessonId, _uin, attendanceStatus, scanTime);
                    await Navigation.PushAsync(new LessonInfo(_role, _uin, _token, lesson));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] OnLessonButtonClicked failed: {ex.Message}");
                await DisplayAlert("Ошибка", "Произошла ошибка при обработке.", "OK");
            }
        }

        private async Task<Lesson> GetCurrentLessonForTeacherAsync(string teacherUin, DateTime currentTime)
        {
            const string connectionString = "Host=10.250.0.64;Port=5432;Username=postgres;Password=postgres;Database=attendance;";

            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"
                SELECT lessonid AS LessonId,
                       room AS Room,
                       pincode AS PinCode,
                       starttime AS StartTime,
                       endtime AS EndTime
                FROM lessons
                WHERE teacheruin = @TeacherUin
                  AND starttime <= @CurrentTime
                  AND endtime >= @CurrentTime";

                    var lesson = await connection.QueryFirstOrDefaultAsync<Lesson>(query, new
                    {
                        TeacherUin = teacherUin,
                        CurrentTime = currentTime
                    });

                    if (lesson != null)
                    {
                        Debug.WriteLine($"[DEBUG] Current lesson found: {lesson.LessonId}");
                    }

                    return lesson;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Failed to get current lesson for teacher: {ex.Message}");
                return null;
            }
        }


        private async Task MarkAttendanceAsync(string lessonId, string studentUin, string status, DateTime scanTime)
        {
            const string sql = @"
    INSERT INTO student_attendance (lessonid, uin, status, scantime, is_status_manually_overridden)
VALUES (@LessonId, @UIN, @Status, @ScanTime, false)
ON CONFLICT (lessonid, uin)
DO UPDATE SET 
    status = EXCLUDED.status,
    scantime = EXCLUDED.scantime
WHERE student_attendance.scantime IS NULL;
";



            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            await connection.ExecuteAsync(sql, new
            {
                LessonId = lessonId,
                UIN = studentUin,
                Status = status,     // "green"/"yellow"
                ScanTime = scanTime
            });
        }



        // Переход на главную страницу
        private async void OnDesktopClicked(object sender, EventArgs e)
        {
            try
            {
                Debug.WriteLine($"Navigating to Desktop with Role: {_role}, UIN: {_uin}, Token: {_token}");
                await Navigation.PushAsync(new Desktop(_role, _uin, _token));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", "Произошла ошибка при переходе на главный экран.", "OK");
            }
        }
    }
}
