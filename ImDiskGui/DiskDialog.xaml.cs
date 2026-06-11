using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Win32;

namespace ImDiskGui
{
    public partial class DiskDialog : Window
    {

        public long SelectedSizeInBytes { get; private set; }
        public char SelectedDriveLetter { get; private set; }
        public string SelectedImagePath { get; private set; }
        public string SelectedFileSystem { get; private set; }
        public bool SaveOnShutdown { get; private set; }
        public bool IsRemovable { get; private set; }

        public DiskDialog()
        {
            InitializeComponent();
            LoadDriveLetters();
            LoadMemoryStatus();
        }

        private void LoadDriveLetters()
        {
            // Populate available drive letters (D to Z)
            var activeDrives = string.Concat(Directory.GetLogicalDrives());
            char freeLetter = 'R'; // Default fallback

            try
            {
                freeLetter = ImDiskNativeApi.ImDiskFindFreeDriveLetter();
            }
            catch { }

            for (char c = 'D'; c <= 'Z'; c++)
            {
                if (activeDrives.IndexOf(c.ToString(), StringComparison.OrdinalIgnoreCase) < 0)
                {
                    ComboDriveLetter.Items.Add(c.ToString() + ":");
                }
            }

            // Select the free letter if available
            string selectDriveStr = freeLetter.ToString() + ":";
            if (ComboDriveLetter.Items.Contains(selectDriveStr))
            {
                ComboDriveLetter.SelectedItem = selectDriveStr;
            }
            else if (ComboDriveLetter.Items.Count > 0)
            {
                ComboDriveLetter.SelectedIndex = 0;
            }
        }

        private void LoadMemoryStatus()
        {
            var memStatus = new ImDiskNativeApi.MEMORYSTATUSEX();
            if (ImDiskNativeApi.GlobalMemoryStatusEx(memStatus))
            {
                double totalGB = memStatus.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                double availMB = memStatus.ullAvailPhys / (1024.0 * 1024.0);
                TxtFreeSpace.Text = LanguageManager.Instance.Format("DlgFreeRam", availMB.ToString("N0"), totalGB.ToString("F1"));
            }
            else
            {
                TxtFreeSpace.Text = LanguageManager.Instance.Format("DlgFreeRam", "---", "---");
            }
        }

        private void BtnBrowseImage_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                Title = "Choose Backup Image Location",
                Filter = "Disk Image (*.img;*.bin)|*.img;*.bin|All files (*.*)|*.*",
                DefaultExt = ".img"
            };

            if (saveDialog.ShowDialog() == true)
            {
                TxtImagePath.Text = saveDialog.FileName;
            }
        }

        private void ChkSaveOnShutdown_Checked(object sender, RoutedEventArgs e)
        {
            // Auto check image path
            if (string.IsNullOrWhiteSpace(TxtImagePath.Text))
            {
                MessageBox.Show(
                    LanguageManager.Instance["MsgSaveOnShutdownInfo"],
                    LanguageManager.Instance["Info"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        private void ChkSaveOnShutdown_Unchecked(object sender, RoutedEventArgs e)
        {
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            // Validate Size
            if (!long.TryParse(TxtSize.Text, out long sizeMB) || sizeMB <= 0)
            {
                MessageBox.Show(
                    LanguageManager.Instance["MsgInvalidSize"],
                    LanguageManager.Instance["Error"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            SelectedSizeInBytes = sizeMB * 1024 * 1024;

            // Validate Drive Letter
            if (ComboDriveLetter.SelectedItem == null)
            {
                MessageBox.Show(
                    LanguageManager.Instance["MsgNoDriveLetter"],
                    LanguageManager.Instance["Error"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }
            SelectedDriveLetter = ComboDriveLetter.SelectedItem.ToString()[0];

            // Validate Image Path & Save on Shutdown combo
            SelectedImagePath = TxtImagePath.Text.Trim();
            SaveOnShutdown = ChkSaveOnShutdown.IsChecked == true;

            if (SaveOnShutdown && string.IsNullOrEmpty(SelectedImagePath))
            {
                MessageBox.Show(
                    LanguageManager.Instance["MsgNoBackupPathSave"],
                    LanguageManager.Instance["Error"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            SelectedFileSystem = ((System.Windows.Controls.ComboBoxItem)ComboFileSystem.SelectedItem).Content.ToString();
            IsRemovable = ChkRemovable.IsChecked == true;

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
