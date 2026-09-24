using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Navigation;

namespace ImDiskGui
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
            catch { }
        }

        private void BtnDriverMaintenance_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var win = new DriverMaintenanceWindow
                {
                    Owner = this
                };
                win.ShowDialog();
            }
            catch
            {
                MessageBox.Show(
                    LanguageManager.Instance["DriverMaintenanceFailed"],
                    LanguageManager.Instance["Error"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }

        private async void BtnCheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            btnCheckUpdates.IsEnabled = false;
            try
            {
                string json;
                using (var client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.UserAgent] = "ImDiskGui-fork-update-check";
                    client.Headers[HttpRequestHeader.Accept] = "application/vnd.github+json";
                    json = await client.DownloadStringTaskAsync(
                        new Uri("https://api.github.com/repos/shadowjohn/ImDisk/releases/latest"));
                }

                var release = new JavaScriptSerializer().Deserialize<ReleaseInfo>(json);
                if (release == null || string.IsNullOrWhiteSpace(release.tag_name) ||
                    string.IsNullOrWhiteSpace(release.html_url) ||
                    !Uri.TryCreate(release.html_url, UriKind.Absolute, out var releaseUri) ||
                    releaseUri.Scheme != Uri.UriSchemeHttps ||
                    releaseUri.Host != "github.com" ||
                    !releaseUri.AbsolutePath.StartsWith("/shadowjohn/ImDisk/releases/", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Unexpected release response.");

                Version latest;
                Version current = Assembly.GetExecutingAssembly().GetName().Version;
                if (release.tag_name.Equals("v1.01", StringComparison.OrdinalIgnoreCase))
                    latest = new Version(1, 0, 1, 0);
                else if (!Version.TryParse(release.tag_name.TrimStart('v', 'V'), out latest))
                    throw new InvalidOperationException("Invalid release version.");

                if (latest > current)
                {
                    var choice = MessageBox.Show(
                        LanguageManager.Instance.Format("UpdateAvailable", release.tag_name),
                        LanguageManager.Instance["UpdateTitle"],
                        MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (choice == MessageBoxResult.Yes)
                        Process.Start(new ProcessStartInfo(releaseUri.AbsoluteUri) { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show(LanguageManager.Instance["UpdateCurrent"],
                        LanguageManager.Instance["UpdateTitle"], MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception)
            {
                MessageBox.Show(LanguageManager.Instance["UpdateFailed"],
                    LanguageManager.Instance["UpdateTitle"], MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                btnCheckUpdates.IsEnabled = true;
            }
        }

        private sealed class ReleaseInfo
        {
            public string tag_name { get; set; }
            public string html_url { get; set; }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
