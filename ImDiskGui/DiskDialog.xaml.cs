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
        public int SelectedAutoSaveIntervalMinutes { get; private set; }

        // Exact byte size detected from an existing image file (0 = not detected)
        private long _detectedImageSizeInBytes = 0;

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
                DefaultExt = ".img",
                OverwritePrompt = false  // Don't warn about overwriting existing files
            };

            if (saveDialog.ShowDialog() == true)
            {
                TxtImagePath.Text = saveDialog.FileName;

                // If user selected an existing image file, auto-detect its size
                try
                {
                    if (File.Exists(saveDialog.FileName))
                    {
                        var fi = new FileInfo(saveDialog.FileName);
                        if (fi.Length > 0)
                        {
                            // Store exact byte size for precise mounting
                            _detectedImageSizeInBytes = fi.Length;

                            // Display as MB (ceiling to avoid truncation)
                            long sizeMB = (fi.Length + (1024 * 1024 - 1)) / (1024 * 1024);
                            TxtSize.Text = sizeMB.ToString();

                            // Auto-check save on shutdown since they're loading from an existing image
                            ChkSaveOnShutdown.IsChecked = true;
                        }
                    }
                    else
                    {
                        _detectedImageSizeInBytes = 0;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error detecting image file size: " + ex.Message);
                }
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

            // If we detected an exact image file size, use that to avoid truncation issues;
            // otherwise compute from the user-entered MB value.
            if (_detectedImageSizeInBytes > 0)
            {
                SelectedSizeInBytes = _detectedImageSizeInBytes;
            }
            else
            {
                SelectedSizeInBytes = sizeMB * 1024 * 1024;
            }

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

            SelectedAutoSaveIntervalMinutes = 0;
            if (SaveOnShutdown && ComboAutoSave.SelectedItem is System.Windows.Controls.ComboBoxItem autoSaveItem && autoSaveItem.Tag != null)
            {
                SelectedAutoSaveIntervalMinutes = int.Parse(autoSaveItem.Tag.ToString());
            }

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
