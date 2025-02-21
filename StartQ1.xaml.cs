#if ANDROID
using Android.Content;
using Android.Locations;
using Android.Net.Wifi;
using Android.Provider;
#endif

#if WINDOWS
using Windows.Devices.WiFi;
using Windows.Networking.Connectivity;
#endif

#if IOS
using CoreFoundation;
using SystemConfiguration;
#endif

#if MACCATALYST || MACOS
using SystemConfiguration;
using System.Diagnostics; // Для использования Process
using System.Text.RegularExpressions; // Для использования Regex
#endif

#if TIZEN
using Tizen.Network.WiFi;
#endif

namespace AC
{
    public partial class StartQ1 : ContentPage
    {
        private readonly UserService _userService;

        // Массив обязательных SSID сетей
        private readonly string[] RequiredSSIDs = { "Study", "WiFi-Guest", "Office" };

        public StartQ1()
        {
            InitializeComponent();
            _userService = new UserService();

#if ANDROID
            CheckLocationAndPermission();
#endif

#if WINDOWS
            CheckWiFiConnectionWindows();
#endif

#if IOS
            CheckNetworkConnectionIOS();
#endif

#if MACCATALYST || MACOS
            CheckWiFiConnectionMacOS();
#endif

#if TIZEN
            CheckWiFiConnectionTizen();
#endif
        }

#if ANDROID
        private void CheckWiFiConnection()
        {
            WifiHelper wifiHelper = new WifiHelper();
            string currentSSID = wifiHelper.GetWifiSSID();

            Console.WriteLine($"Detected SSID (Android): {currentSSID}");
            DisplayAlert("WiFi Info", $"Detected SSID: {currentSSID}", "OK");

            if (currentSSID == null || !RequiredSSIDs.Contains(currentSSID))
            {
                this.IsEnabled = false;
                DisplayAlert("WiFi Error", "You are not connected to a required WiFi network.", "OK");
            }
        }
        
        private void OpenLocationSettings()
        {
            Intent intent = new Intent(Settings.ActionLocationSourceSettings);
            Android.App.Application.Context.StartActivity(intent);
        }

        private bool IsLocationEnabled()
        {
            LocationManager locationManager = (LocationManager)Android.App.Application.Context.GetSystemService(Context.LocationService);
            return locationManager.IsProviderEnabled(LocationManager.GpsProvider) || locationManager.IsProviderEnabled(LocationManager.NetworkProvider);
        }

        private async void CheckLocationAndPermission()
        {
            if (!IsLocationEnabled())
            {
                await DisplayAlert("Location Required", "Please enable location services to detect the WiFi network.", "OK");
                OpenLocationSettings();
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

                return ssid?.Trim('"'); // Удаление кавычек, если они есть
            }
        }
#endif

#if WINDOWS
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

        private async void CheckWiFiConnectionWindows()
        {
            string currentSSID = await GetCurrentWifiSSIDWindows();

            Console.WriteLine($"Detected SSID (Windows): {currentSSID}");
            DisplayAlert("WiFi Info", $"Detected SSID: {currentSSID}", "OK");

            if (currentSSID == null || !RequiredSSIDs.Contains(currentSSID))
            {
                this.IsEnabled = false;
                DisplayAlert("WiFi Error", "You are not connected to a required WiFi network.", "OK");
            }
        }
#endif

#if IOS
        private bool IsConnectedToNetwork()
        {
            NetworkReachabilityFlags flags;
            var address = new System.Net.IPAddress(0);
            var reachability = new NetworkReachability(address);
            reachability.TryGetFlags(out flags);
            return (flags & NetworkReachabilityFlags.Reachable) != 0;
        }

        private void CheckNetworkConnectionIOS()
        {
            bool isConnected = IsConnectedToNetwork();
            Console.WriteLine($"Network Connectivity (iOS): {isConnected}");
            DisplayAlert("Network Info", $"Network Connectivity: {isConnected}", "OK");

            if (!isConnected)
            {
                this.IsEnabled = false;
                DisplayAlert("Network Error", "You are not connected to a required network.", "OK");
            }
        }
#endif

#if MACCATALYST || MACOS
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

        private void CheckWiFiConnectionMacOS()
        {
            string currentSSID = GetCurrentWifiSSIDMacOS();

            Console.WriteLine($"Detected SSID (macOS): {currentSSID}");
            DisplayAlert("WiFi Info", $"Detected SSID: {currentSSID}", "OK");

            if (currentSSID == null || !RequiredSSIDs.Contains(currentSSID))
            {
                this.IsEnabled = false;
                DisplayAlert("WiFi Error", "You are not connected to a required WiFi network.", "OK");
            }
        }
#endif

#if TIZEN
        private void CheckWiFiConnectionTizen()
        {
            string currentSSID = GetCurrentWifiSSIDTizen();

            Console.WriteLine($"Detected SSID (Tizen): {currentSSID}");
            DisplayAlert("WiFi Info", $"Detected SSID: {currentSSID}", "OK");

            if (currentSSID == null || !RequiredSSIDs.Contains(currentSSID))
            {
                this.IsEnabled = false;
                DisplayAlert("WiFi Error", "You are not connected to a required WiFi network.", "OK");
            }
        }
#endif

        private void ShowNoConnectionMessage()
        {
            MainUI.IsVisible = false;
            NoConnectionMessage.IsVisible = true;
        }

        private void ShowMainUI()
        {
            MainUI.IsVisible = true;
            NoConnectionMessage.IsVisible = false;
        }

        private async void OnStudentSelected(object sender, EventArgs e)
        {
            string role = "student";
            string lastName = LastNameEntry.Text;
            string firstName = FirstNameEntry.Text;
            string patronymic = PatronymicEntry.Text;

            await Navigation.PushAsync(new SignIn());
        }

        private async void OnTeacherSelected(object sender, EventArgs e)
        {
            string role = "teacher";
            string lastName = LastNameEntry.Text;
            string firstName = FirstNameEntry.Text;
            string patronymic = PatronymicEntry.Text;

            await Navigation.PushAsync(new SignIn());
        }

        private async void GoBack(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
