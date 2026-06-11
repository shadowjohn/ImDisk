using System.Diagnostics;
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

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
