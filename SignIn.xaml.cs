#if ANDROID
using Android.Content;
using Android.Locations;
using Android.Net.Wifi;
using Android.Provider;
#endif

#if WINDOWS
using Windows.Networking.Connectivity;
#endif

#if IOS
using CoreFoundation;
using SystemConfiguration;
#endif

#if MACCATALYST || MACOS
using System.Diagnostics;
using System.Text.RegularExpressions;
#endif

#if TIZEN
using Tizen.Network.WiFi;
#endif

using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Newtonsoft.Json.Linq;

namespace AC
{
    public partial class SignIn : ContentPage
    {
        private readonly UserService _userService;
        private string selectedRole;
        private const int maxAttempts = 5;
        private int failedAttempts = 0;
        private DateTime? lockoutEndTime = null;

        private readonly string[] RequiredSSIDs = { "Study", "WiFi-Guest", "Office", "AndroidAP" };

        public SignIn()
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
            InitializeAttemptsData();
            _userService = new UserService();

            PerformPlatformSpecificWiFiChecks();
        }

        private void InitializeAttemptsData()
        {
            failedAttempts = Preferences.Get("failedAttempts", 0);
            Debug.WriteLine($"Loaded failed attempts from Preferences: {failedAttempts}");

            string lockoutTimeStr = Preferences.Get("lockoutEndTime", null);
            if (!string.IsNullOrEmpty(lockoutTimeStr))
            {
                lockoutEndTime = DateTime.Parse(lockoutTimeStr, null, DateTimeStyles.RoundtripKind);
                Debug.WriteLine($"Loaded lockout end time from Preferences: {lockoutEndTime}");
            }
        }

        private void PerformPlatformSpecificWiFiChecks()
        {
#if ANDROID
            CheckLocationAndPermission();
#elif WINDOWS
            CheckWiFiConnectionWindows();
#elif IOS
            CheckNetworkConnectionIOS();
#elif MACCATALYST || MACOS
            CheckWiFiConnectionMacOS();
#elif TIZEN
            CheckWiFiConnectionTizen();
#endif
        }

        // Platform-Specific WiFi and Location Checks

#if ANDROID
        private async void CheckLocationAndPermission()
        {
            if (!IsLocationEnabled())
            {
                await DisplayAlert("Location Required", "Please enable location services to detect the WiFi network.", "OK");
                OpenLocationSettings();
                return;
            }

            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status == PermissionStatus.Granted && IsLocationEnabled())
            {
                CheckWiFiConnection();
            }
            else
            {
                await DisplayAlert("Permission Denied", "Location permission is required to access WiFi information.", "OK");
            }
        }

        private bool IsLocationEnabled()
        {
            LocationManager locationManager = (LocationManager)Android.App.Application.Context.GetSystemService(Context.LocationService);
            return locationManager.IsProviderEnabled(LocationManager.GpsProvider) || locationManager.IsProviderEnabled(LocationManager.NetworkProvider);
        }

        private void OpenLocationSettings()
        {
            Intent intent = new Intent(Settings.ActionLocationSourceSettings);
            intent.AddFlags(ActivityFlags.NewTask);
            Android.App.Application.Context.StartActivity(intent);
        }

        private void CheckWiFiConnection()
        {
            WifiHelper wifiHelper = new WifiHelper();
            string currentSSID = wifiHelper.GetWifiSSID();

            Console.WriteLine($"Detected SSID (Android): {currentSSID}");

            if (currentSSID == null || !RequiredSSIDs.Contains(currentSSID))
            {
                this.IsEnabled = false;
                DisplayAlert("WiFi Error", "You are not connected to a required WiFi network.", "OK");
            }
        }

        public class WifiHelper
        {
            public string GetWifiSSID()
            {
                string ssid = null;
                try
                {
                    WifiManager wifiManager = (WifiManager)Android.App.Application.Context.GetSystemService(Context.WifiService);
                    if (wifiManager.IsWifiEnabled)
                    {
                        ssid = wifiManager.ConnectionInfo.SSID;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error retrieving WiFi SSID: " + ex.Message);
                }

                return ssid?.Trim('"'); // Remove quotes if they exist
            }
        }
#endif

#if WINDOWS
        private async void CheckWiFiConnectionWindows()
        {
            string currentSSID = await GetCurrentWifiSSIDWindows();

            Console.WriteLine($"Detected SSID (Windows): {currentSSID}");

            if (currentSSID == null || !RequiredSSIDs.Contains(currentSSID))
            {
                this.IsEnabled = false;
                await DisplayAlert("WiFi Error", "You are not connected to a required WiFi network.", "OK");
            }
        }

        private async Task<string> GetCurrentWifiSSIDWindows()
        {
            try
            {
                var profiles = NetworkInformation.GetConnectionProfiles();

                foreach (var profile in profiles)
                {
                    if (profile.IsWlanConnectionProfile && profile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess)
                    {
                        var ssid = profile.WlanConnectionProfileDetails.GetConnectedSsid();
                        return ssid;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error retrieving WiFi SSID on Windows: " + ex.Message);
            }

            return null;
        }
#endif

#if IOS
        private void CheckNetworkConnectionIOS()
        {
            bool isConnected = IsConnectedToNetwork();
            Console.WriteLine($"Network Connectivity (iOS): {isConnected}");

            if (!isConnected)
            {
                this.IsEnabled = false;
                DisplayAlert("Network Error", "You are not connected to a required network.", "OK");
            }
        }

        private bool IsConnectedToNetwork()
        {
            NetworkReachabilityFlags flags;
            var address = new System.Net.IPAddress(0);
            var reachability = new NetworkReachability(address);
            reachability.TryGetFlags(out flags);
            return (flags & NetworkReachabilityFlags.Reachable) != 0;
        }
#endif

#if MACCATALYST || MACOS
        private void CheckWiFiConnectionMacOS()
        {
            string currentSSID = GetCurrentWifiSSIDMacOS();

            Console.WriteLine($"Detected SSID (macOS): {currentSSID}");

            if (currentSSID == null || !RequiredSSIDs.Contains(currentSSID))
            {
                this.IsEnabled = false;
                DisplayAlert("WiFi Error", "You are not connected to a required WiFi network.", "OK");
            }
        }

        private string GetCurrentWifiSSIDMacOS()
        {
            string ssid = null;
            try
            {
                var task = new Process();
                task.StartInfo.FileName = "/System/Library/PrivateFrameworks/Apple80211.framework/Versions/Current/Resources/airport";
                task.StartInfo.Arguments = "-I";
                task.StartInfo.RedirectStandardOutput = true;
                task.StartInfo.UseShellExecute = false;
                task.StartInfo.CreateNoWindow = true;
                task.Start();

                string output = task.StandardOutput.ReadToEnd();
                task.WaitForExit();

                var match = Regex.Match(output, @"\s*SSID: (.+)");
                if (match.Success)
                {
                    ssid = match.Groups[1].Value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error retrieving WiFi SSID on macOS: " + ex.Message);
            }

            return ssid;
        }
#endif

#if TIZEN
        private void CheckWiFiConnectionTizen()
        {
            string currentSSID = GetCurrentWifiSSIDTizen();

            Console.WriteLine($"Detected SSID (Tizen): {currentSSID}");

            if (currentSSID == null || !RequiredSSIDs.Contains(currentSSID))
            {
                this.IsEnabled = false;
                DisplayAlert("WiFi Error", "You are not connected to a required WiFi network.", "OK");
            }
        }

        private string GetCurrentWifiSSIDTizen()
        {
            string ssid = null;
            try
            {
                WiFiManager manager = WiFiManager.Instance;
                if (manager.ConnectionState == WiFiConnectionState.Connected)
                {
                    ssid = manager.GetConnectedAP()?.Essid;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error retrieving WiFi SSID on Tizen: " + ex.Message);
            }

            return ssid;
        }
#endif

        // Login Logic and Attempts Limiting

        private async void OnContinueClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(uinEntry.Text) || string.IsNullOrWhiteSpace(passwordEntry.Text) || string.IsNullOrEmpty(selectedRole))
            {
                await DisplayAlert("Error", "Please fill in all fields and select a role.", "OK");
                return;
            }

            var currentTime = await GetAstanaTimeAsync();
            if (lockoutEndTime.HasValue && currentTime < lockoutEndTime.Value)
            {
                var remainingTime = lockoutEndTime.Value - currentTime;
                await DisplayAlert("Blocked", $"Suspicious activity. Try again in {remainingTime.Minutes} minutes.", "OK");
                return;
            }

            await AttemptLogin(uinEntry.Text, passwordEntry.Text);
        }

        private async Task<DateTime> GetAstanaTimeAsync()
        {
            using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
            {
                const int retryCount = 1;
                const int delay = 3000;

                for (int attempt = 0; attempt < retryCount; attempt++)
                {
                    try
                    {
                        var response = await client.GetStringAsync("https://timeapi.io/api/Time/current/zone?timeZone=Asia/Almaty");
                        if (string.IsNullOrWhiteSpace(response)) throw new Exception("Received empty response from the server.");

                        var json = JObject.Parse(response);
                        var dateTimeStr = (string)json["dateTime"];
                        if (string.IsNullOrWhiteSpace(dateTimeStr)) throw new Exception("Datetime value from API is empty or unavailable.");

                        return DateTimeOffset.Parse(dateTimeStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).DateTime;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error retrieving time: {ex.Message}");
                        await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
                        await Task.Delay(delay);
                    }
                }

                throw new Exception("Retries exhausted. Unable to retrieve time.");
            }
        }

        private async Task AttemptLogin(string uin, string password)
        {
            try
            {
                var loginResponse = await _userService.LoginAsync(uin, password);

                if (loginResponse != null && loginResponse.Message == "Login successful.")
                {
                    ResetLoginAttempts();
                    Preferences.Set("UserUIN", uin);
                    Preferences.Set("auth_token", loginResponse.Token);
                    await Navigation.PushAsync(new Desktop(selectedRole, loginResponse.Token, uin));
                }
                else
                {
                    await DisplayAlert("Error", "Invalid login or password.", "OK");
                    failedAttempts++;
                    Preferences.Set("failedAttempts", failedAttempts);

                    if (failedAttempts >= maxAttempts)
                    {
                        lockoutEndTime = DateTime.Now.AddMinutes(15);
                        Preferences.Set("lockoutEndTime", lockoutEndTime.Value.ToString("o"));
                        await DisplayAlert("Blocked", "Suspicious activity. Try again in 15 minutes.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Login error: {ex.Message}");
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        private void ResetLoginAttempts()
        {
            failedAttempts = 0;
            Preferences.Set("failedAttempts", failedAttempts);
            lockoutEndTime = null;
            Preferences.Remove("lockoutEndTime");
        }

        // Role Selection Methods

        private void OnTeacherSelected(object sender, EventArgs e)
        {
            selectedRole = "teacher";
            teacherButton.BackgroundColor = Color.FromArgb("#7CB1FF");
            teacherButton.TextColor = Colors.White;
            studentButton.BackgroundColor = Colors.White;
            studentButton.TextColor = Color.FromArgb("#828282");
        }

        private void OnStudentSelected(object sender, EventArgs e)
        {
            selectedRole = "student";
            studentButton.BackgroundColor = Color.FromArgb("#7CB1FF");
            studentButton.TextColor = Colors.White;
            teacherButton.BackgroundColor = Colors.White;
            teacherButton.TextColor = Color.FromArgb("#828282");
        }

        // Password Visibility Toggle

        private void OnPasswordButtonPressed(object sender, EventArgs e)
        {
            // Show password
            passwordEntry.IsPassword = false;
        }

        private void OnPasswordButtonReleased(object sender, EventArgs e)
        {
            // Hide password
            passwordEntry.IsPassword = true;
        }

        private async void GoBack(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
