using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Dapper;
using Microsoft.Maui.Controls;
using Npgsql;
using ZXing;
using ZXing.Common;
using System.IO;
using AC.AC;
using QRCoder;
using QRCodeGenerator = QRCoder.QRCodeGenerator;

namespace AC
{
    public class RoomInfoViewModel
    {
        public string RoomNumber { get; set; }
    }

    public partial class RoomInfo : ContentPage
    {
        private CancellationTokenSource _cts;
        private readonly string _role;
        private readonly string _uin;
        private readonly Lesson _lesson;
        private readonly string _token;
        private readonly LessonService _lessonService;
        private RoomInfoViewModel _viewModel;
        private readonly string _roomid;
        private const string connectionString = "Host=10.250.0.64;Port=5432;Username=postgres;Password=postgres;Database=attendance;";
        public RoomInfo(string role, string uin, string roomid, string token)
        {
            InitializeComponent();
            _role = role;
            _uin = Preferences.Get("UserUIN", uin);
            _roomid = roomid;
            _token = Preferences.Get("auth_token", token);

            BindingContext = new RoomInfoViewModel
            {
                RoomNumber = roomid
            };

            NavigationPage.SetHasNavigationBar(this, false);
            _lessonService = new LessonService();
        }


        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _cts = new CancellationTokenSource();
            loadingIndicator.IsRunning = true;
            loadingIndicator.IsVisible = true;
            loadingLabel.IsVisible = true;
            lessonsListView.IsVisible = false;
            await LoadLessonsAsync(_cts.Token);
            loadingIndicator.IsRunning = false;
            loadingIndicator.IsVisible = false;
            loadingLabel.IsVisible = false;
            lessonsListView.IsVisible = true;
            
        }

        private async Task LoadLessonsAsync(CancellationToken token)
        {
            try
            {
                const string query = @"
SELECT 
            l.lessonid,
            l.pincode,
            l.teacher,
            l.starttime,
            l.endtime,
            l.room,
            l.group,
            s.subject_name AS Description -- Получаем название предмета
        FROM lessons l
        JOIN subjects_teachers st ON st.teacher_id = (
            SELECT id FROM users WHERE uin = @Uin AND role = 'teacher'
        )
        JOIN subjects s ON st.subject_id = s.subject_id
        WHERE l.room = @Room
          AND l.starttime <= NOW() + INTERVAL '5 hours' + INTERVAL '15 minutes'
          AND l.endtime >= NOW() + INTERVAL '5 hours'";

                using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync(token); // Передаём токен для отмены

                // Передаём параметры в запрос
                var lessons = (await connection.QueryAsync<Lesson>(
                    query,
                    new { Uin = _uin, Room = _roomid }
                )).ToList();

                // Проверяем токен: если операция была отменена, выбрасывается исключение
                token.ThrowIfCancellationRequested();

                if (!lessons.Any())
                {
                    Debug.WriteLine("[INFO] No current lessons found.");
                    DisplayNoLessonsMessage();
                    return;
                }

                Debug.WriteLine($"[INFO] Found {lessons.Count} ongoing lessons.");

                // Обновляем UI только если не отменено
                if (!token.IsCancellationRequested)
                {
                    lessonsListView.ItemsSource = lessons;
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("[INFO] LoadLessonsAsync was canceled.");
                // Здесь можно ничего не делать — операция прервана
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Failed to load lessons: {ex.Message}");

                // Отображаем ошибку только если не отменено
                if (!token.IsCancellationRequested)
                {
                    await DisplayAlert("Ошибка", "Не удалось загрузить уроки.", "OK");
                }
            }
            finally
            {
                // Убираем индикаторы загрузки только если не отменено
                if (!token.IsCancellationRequested)
                {
                    loadingIndicator.IsRunning = false;
                    loadingIndicator.IsVisible = false;
                    loadingLabel.IsVisible = false;
                    lessonsListView.IsVisible = true;
                }
            }
        }


        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if (_cts != null)
            {
                _cts.Cancel(); // Отменяем все операции
                _cts.Dispose(); // Освобождаем ресурсы
                _cts = null; // Обнуляем ссылку
            }
        }

        private async void OnLessonTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item is Lesson tappedLesson)
            {
                // Navigate to a new page with lesson details
                await Navigation.PushAsync(new LessonInfo(_role, _uin, _token, tappedLesson));
            }

    // Deselect the tapped item
    ((ListView)sender).SelectedItem = null;
        }


        private void DisplayNoLessonsMessage()
        {
            LessonsStackLayout.Children.Clear();

            if (_role == "teacher")
            {
                LessonsStackLayout.Children.Add(new Label
                {
                    Text = "В данной аудитории пока нет созданных уроков. Вы можете создать новый.",
                    FontSize = 15,
                    TextColor = Color.FromArgb("#AAADB2"),
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalTextAlignment = TextAlignment.Center
                });

                NewLessonButton.IsVisible = true;
            }
            else if (_role == "student")
            {
                LessonsStackLayout.Children.Add(new Label
                {
                    Text = "В данной аудитории пока нет доступных уроков.",
                    FontSize = 15,
                    TextColor = Color.FromArgb("#AAADB2"),
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalTextAlignment = TextAlignment.Center
                });

                NewLessonButton.IsVisible = false;
            }
        }

        private async void OnNewLessonButtonClicked(object sender, EventArgs e)
        {
            if (_role == "teacher")
            {
                await Navigation.PushAsync(new NewLesson(_role, _uin, _token, _roomid));
            }
        }


        private async void GoBack(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

    }
}
