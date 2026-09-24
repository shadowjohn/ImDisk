using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
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

                var tagMatch = Regex.Match(json, "\\\"tag_name\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"");
                var urlMatch = Regex.Match(json, "\\\"html_url\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"");
                string tagName = tagMatch.Success ? tagMatch.Groups[1].Value : null;
                string releaseUrl = urlMatch.Success ? urlMatch.Groups[1].Value : null;
                if (string.IsNullOrWhiteSpace(tagName) ||
                    string.IsNullOrWhiteSpace(releaseUrl) ||
                    !Uri.TryCreate(releaseUrl, UriKind.Absolute, out var releaseUri) ||
                    releaseUri.Scheme != Uri.UriSchemeHttps ||
                    releaseUri.Host != "github.com" ||
                    !releaseUri.AbsolutePath.StartsWith("/shadowjohn/ImDisk/releases/", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Unexpected release response.");

                Version latest;
                Version current = Assembly.GetExecutingAssembly().GetName().Version;
                if (tagName.Equals("v1.01", StringComparison.OrdinalIgnoreCase))
                    latest = new Version(1, 0, 1, 0);
                else if (!Version.TryParse(tagName.TrimStart('v', 'V'), out latest))
                    throw new InvalidOperationException("Invalid release version.");

                if (latest > current)
                {
                    var choice = MessageBox.Show(
                        LanguageManager.Instance.Format("UpdateAvailable", tagName),
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

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
