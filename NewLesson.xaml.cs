using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AC.AC;
using Dapper;
using Microsoft.Maui.Controls;
using Npgsql;

namespace AC
{
    public partial class NewLesson : ContentPage
    {
        private readonly LessonService _lessonService;
        private readonly UserService _userService;
        private readonly string _role;
        private readonly string _uin;
        private readonly string _token;
        private readonly string _roomid;
        private string _teacherFullName;

        public NewLesson(string role, string uin, string token, string roomid)
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);

            _lessonService = new LessonService();
            _userService = new UserService();
            _role = role;
            _uin = uin;
            _token = Preferences.Get("auth_token", token);
            _roomid = roomid;  // Save the room ID here

            // Initialize event handlers
            startTimePicker.PropertyChanged += CheckFields;
            endTimePicker.PropertyChanged += CheckFields;
            groupPicker.SelectedIndexChanged += CheckFields;
        }
        const string connectionString = "Host=10.250.0.64;Port=5432;Username=postgres;Password=postgres;Database=attendance;";
        public async Task<List<string>> LoadGroupsFromDatabaseAsync()
        {
            var groups = new List<string>();
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            const string query = "SELECT group_name FROM groups";
            await using var command = new NpgsqlCommand(query, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                groups.Add(reader.GetString(0));
            }

            return groups;
        }

        public async Task UpdateGroupPickerAsync()
        {
            try
            {
                var groups = await LoadGroupsFromDatabaseAsync();
                if (!groups.Any())
                {
                    throw new Exception("Список групп пуст.");
                }

                groupPicker.ItemsSource = groups;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить список групп: {ex.Message}", "OK");
            }
        }


        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                await LoadUserDataAsync();
                CheckFields(null, null); // Check fields on load
                await UpdateGroupPickerAsync();
                await UpdateSubjectPickerAsync();

                // If you need to pre-populate roomEntry
                roomEntry.Text = _roomid; // Assuming _roomid was passed or is available here
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить данные пользователя: {ex.Message}", "OK");
            }
        }


        private async Task LoadUserDataAsync()
        {
            var user = await _userService.GetUserByUINAsync(_uin, _token);
            if (user == null)
            {
                throw new Exception("Не удалось получить данные пользователя.");
            }

            _teacherFullName = $"{user.LastName} {user.FirstName}";
            teacherLabel.Text = _teacherFullName;
        }

        private void CheckFields(object sender, EventArgs e)
        {
            if (roomEntry == null || groupPicker == null || startTimePicker == null || endTimePicker == null)
                return;

            saveButton.IsEnabled =
                !string.IsNullOrWhiteSpace(roomEntry.Text) &&
                groupPicker.SelectedItem != null &&
                startTimePicker.Time != TimeSpan.Zero &&
                endTimePicker.Time != TimeSpan.Zero;
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                if (subjectPicker.SelectedItem == null)
                {
                    await DisplayAlert("Ошибка", "Пожалуйста, выберите предмет.", "OK");
                    return;
                }

                var newLesson = new Lesson
                {
                    LessonId = Guid.NewGuid().ToString(),
                    Teacher = _teacherFullName,
                    TeacherUIN = _uin, // Это свойство должно корректно мапиться
                    StartTime = DateTime.Today.Add(startTimePicker.Time),
                    EndTime = DateTime.Today.Add(endTimePicker.Time),
                    Room = roomEntry.Text,
                    Group = groupPicker.SelectedItem?.ToString(),
                    Description = subjectPicker.SelectedItem?.ToString(),
                    PinCode = GeneratePinCode()
                };


                // Сохраняем урок
                await SaveLessonToDatabaseAsync(newLesson);

                // Инициализируем посещаемость
                await InitializeAttendanceAsync(newLesson.LessonId, newLesson.Group);

                await DisplayAlert("Успех", "Урок успешно добавлен.", "OK");
                await Navigation.PushAsync(new RoomInfo(_role, _uin, _roomid, _token));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось сохранить урок: {ex.Message}", "OK");
            }
        }

        private async Task InitializeAttendanceAsync(string lessonId, string groupName)
        {
            // Шаг 1. Находим все UIN студентов, у которых group = groupName
            const string getGroupUsersSql = @"
        SELECT uin
        FROM users
        WHERE ""group"" = @GroupName
    ";

            // Шаг 2. Для каждого студента делаем INSERT в student_attendance со status='red'
            // Используем ON CONFLICT (lessonid, uin) DO NOTHING или DO UPDATE,
            // но обычно достаточно DO NOTHING, чтобы не перезаписать, если вдруг запись уже есть.

            const string insertRedSql = @"
        INSERT INTO student_attendance (lessonid, uin, status, scantime, is_status_manually_overridden)
        VALUES (@LessonId, @UIN, 'red', NULL, false)
        ON CONFLICT (lessonid, uin) DO NOTHING
    ";

            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            // 1. Получаем список UIN
            var studentUins = await connection.QueryAsync<string>(getGroupUsersSql, new { GroupName = groupName });
            // 2. Для каждого делаем вставку 'red'
            foreach (var uin in studentUins)
            {
                await connection.ExecuteAsync(insertRedSql, new
                {
                    LessonId = lessonId,
                    UIN = uin
                });
            }
        }


        // Метод для сохранения урока в БД
        private async Task SaveLessonToDatabaseAsync(Lesson lesson)
        {
            const string insertQuery = @"
        INSERT INTO lessons (lessonid, teacher, ""teacheruin"", starttime, endtime, room, ""group"", description, pincode)
VALUES (@LessonId, @Teacher, @TeacherUIN, @StartTime, @EndTime, @Room, @Group, @Description, @PinCode);";

            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            Debug.WriteLine($"[DEBUG] LessonId={lesson.LessonId}, Room={lesson.Room}, Group={lesson.Group}, StartTime={lesson.StartTime}, EndTime={lesson.EndTime}");
            await connection.ExecuteAsync(insertQuery, lesson);
        }


        private string GeneratePinCode()
        {
            Random random = new Random();
            int pin = random.Next(1000, 10000);
            return pin.ToString("D4");
        }
        private async void GoBack(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        public async Task<List<string>> LoadSubjectsFromDatabaseAsync()
        {
            var subjects = new List<string>();
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            const string query = @"
    SELECT s.subject_name
    FROM subjects s
    JOIN subjects_teachers st ON s.subject_id = st.subject_id
    WHERE st.teacher_id = (
        SELECT id
        FROM users
        WHERE uin = @TeacherUIN
    )";

            await using var command = new NpgsqlCommand(query, connection);

            // Вот эта строка критически важна!
            command.Parameters.AddWithValue("@TeacherUIN", _uin);

            await using var reader = await command.ExecuteReaderAsync();


            while (await reader.ReadAsync())
            {
                subjects.Add(reader.GetString(0));
            }

            return subjects;
        }

        public async Task UpdateSubjectPickerAsync()
        {
            try
            {
                var subjects = await LoadSubjectsFromDatabaseAsync();
                if (!subjects.Any())
                {
                    throw new Exception("Список предметов пуст.");
                }

                subjectPicker.ItemsSource = subjects;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить список предметов: {ex.Message}", "OK");
            }
        }

    }
}
