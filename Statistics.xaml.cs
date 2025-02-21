using Microsoft.Maui.Controls;
using Npgsql;
using SkiaSharp;
using Microcharts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AC
{
    public partial class Statistics : ContentPage
    {
        private readonly string _role;
        private readonly string _uin;
        private readonly string _token;
        private readonly string connectionString = "Host=10.250.0.64;Port=5432;Username=postgres;Password=postgres;Database=attendance;";

        public Statistics(string role, string uin, string token)
        {
            _role = role;
            _uin = uin;
            _token = token;
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);

            // Если преподаватель, скрываем диаграмму
            if (_role == "teacher")
            {
                ChartFrame.IsVisible = false;
            }

            // Значения по умолчанию для дат
            fromDatePicker.Date = DateTime.Now.AddDays(-30);
            toDatePicker.Date = DateTime.Now;

            // Загрузка данных
            LoadDataAsync(fromDatePicker.Date, toDatePicker.Date);
        }

        private async void OnDesktopClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new Desktop(_role, _uin, _token));
        }

        private async void OnProfileClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new Profile(_role, _uin, _token));
        }

        private async void OnApplyDateRangeClicked(object sender, EventArgs e)
        {
            var fromDate = fromDatePicker.Date;
            var toDate = toDatePicker.Date;
            await LoadDataAsync(fromDate, toDate);
        }

        private async Task LoadDataAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    if (_role == "teacher")
                    {
                        // Запрос только по конкретному преподавателю (teacheruin),
                        // без привязки к группе.
                        string teacherLessonsQuery = @"
                    SELECT 
                        l.lessonid,
                        l.starttime,
                        l.endtime,
                        u.last_name, 
                        u.first_name
                    FROM lessons l
                    JOIN users u ON u.uin = l.teacheruin
                    WHERE l.teacheruin = @TeacherUIN
                      AND l.starttime >= @FromDate 
                      AND l.starttime <= @ToDate
                    ORDER BY l.starttime;";

                        var lessons = new List<LessonInfoItem>();

                        using var cmd = new NpgsqlCommand(teacherLessonsQuery, connection);
                        cmd.Parameters.AddWithValue("@TeacherUIN", _uin);
                        cmd.Parameters.AddWithValue("@FromDate", fromDate.Date);
                        cmd.Parameters.AddWithValue("@ToDate", toDate.Date);

                        using var reader = await cmd.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            var startTime = reader.GetDateTime(1);
                            var endTime = reader.GetDateTime(2);
                            var lastName = reader.GetString(3);
                            var firstName = reader.GetString(4);

                            lessons.Add(new LessonInfoItem
                            {
                                TeacherName = $"{lastName} {firstName}",
                                LessonTime = $"{startTime:dd.MM.yyyy HH:mm} - {endTime:HH:mm}",
                                // Для преподавателя мы не показываем статусы —
                                // используем нейтральный цвет
                                AttendanceColor = Color.FromArgb("#9E9E9E")
                            });
                        }

                        lessonsCollectionView.ItemsSource = lessons;
                        // Диаграмму не отображаем, если преподаватель
                        ChartFrame.IsVisible = false;
                    }
                    else
                    {
                        // Запрос для студента: 
                        // Фильтруем занятия по дате + выводим конкретный статус 
                        // (green/yellow/red/etc.), который хранится в student_attendance.
                        string lessonsQuery = @"
                    SELECT 
                        l.lessonid,
                        l.starttime,
                        l.endtime,
                        u.last_name,
                        u.first_name,
                        l.teacheruin,
                        sa.status
                    FROM lessons l
                    JOIN users u ON u.uin = l.teacheruin
                    LEFT JOIN student_attendance sa 
                           ON sa.lessonid = l.lessonid 
                          AND sa.uin = @UIN
                    WHERE l.starttime >= @FromDate 
                      AND l.starttime <= @ToDate
                    ORDER BY l.starttime;";

                        var lessons = new List<LessonInfoItem>();

                        // Словарь для подсчёта каждого статуса (для диаграммы)
                        var statusDistributionData = new Dictionary<string, int>
                {
                    { "green", 0 },
                    { "yellow", 0 },
                    { "red", 0 },
                    { "unknown", 0 }
                };

                        using var lessonsCommand = new NpgsqlCommand(lessonsQuery, connection);
                        lessonsCommand.Parameters.AddWithValue("@FromDate", fromDate.Date);
                        lessonsCommand.Parameters.AddWithValue("@ToDate", toDate.Date);
                        lessonsCommand.Parameters.AddWithValue("@UIN", _uin);

                        using var reader = await lessonsCommand.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            var startTime = reader.GetDateTime(1);
                            var endTime = reader.GetDateTime(2);
                            var lastName = reader.GetString(3);
                            var firstName = reader.GetString(4);

                            // Если статус не установлен, считаем его "red"
                            // или иной статус по умолчанию
                            var attendanceStatus = reader.IsDBNull(6)
                                ? "red"
                                : reader.GetString(6);

                            lessons.Add(new LessonInfoItem
                            {
                                TeacherName = $"{lastName} {firstName}",
                                LessonTime = $"{startTime:dd.MM.yyyy HH:mm} - {endTime:HH:mm}",
                                AttendanceColor = DetermineColor(attendanceStatus)
                            });

                            if (statusDistributionData.ContainsKey(attendanceStatus))
                            {
                                statusDistributionData[attendanceStatus]++;
                            }
                            else
                            {
                                statusDistributionData["unknown"]++;
                            }
                        }

                        lessonsCollectionView.ItemsSource = lessons;

                        // Обновляем диаграмму
                        var statusList = statusDistributionData
                            .Select(kvp => new KeyValuePair<string, int>(kvp.Key, kvp.Value))
                            .ToList();

                        UpdateStatusDistributionChart(statusList);
                        ChartFrame.IsVisible = true;
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", $"Ошибка загрузки статистики: {ex.Message}", "OK");
            }
        }


        private Color DetermineColor(string status)
        {
            return status switch
            {
                "green" => Color.FromArgb("#4CAF50"),
                "yellow" => Color.FromArgb("#FFC107"),
                "red" => Color.FromArgb("#F44336"),
                _ => Color.FromArgb("#9E9E9E")
            };
        }

        private void UpdateStatusDistributionChart(List<KeyValuePair<string, int>> data)
        {
            var entries = data.Select(point => new Microcharts.ChartEntry(point.Value)
            {
                Label = point.Key switch
                {
                    "green" => $": {point.Value}",
                    "yellow" => $": {point.Value}",
                    "red" => $": {point.Value}",
                    _ => $": {point.Value}"
                },
                Color = point.Key switch
                {
                    "green" => SkiaSharp.SKColor.Parse("#4CAF50"),
                    "yellow" => SkiaSharp.SKColor.Parse("#FFC107"),
                    "red" => SkiaSharp.SKColor.Parse("#F44336"),
                    _ => SkiaSharp.SKColor.Parse("#9E9E9E")
                },
            }).ToList();

            performanceChart.Chart = new Microcharts.PieChart
            {
                Entries = entries,
                LabelTextSize = 30,
            };
        }

        private class LessonInfoItem
        {
            public string TeacherName { get; set; }
            public string LessonTime { get; set; }
            public Color AttendanceColor { get; set; }
        }
    }
}
