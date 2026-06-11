using System;
using System.Windows;

namespace ImDiskGui
{
    public partial class ResizeDialog : Window
    {
        private readonly long _currentSizeInBytes;
        public long NewSizeInBytes { get; private set; }

        public ResizeDialog(char driveLetter, long currentSizeInBytes)
        {
            InitializeComponent();
            _currentSizeInBytes = currentSizeInBytes;

            long currentSizeMB = currentSizeInBytes / (1024 * 1024);
            string promptPattern = LanguageManager.Instance["ResizePrompt"];
            TxtPrompt.Text = string.Format(promptPattern, driveLetter, currentSizeMB);
            TxtNewSize.Text = (currentSizeMB + 128).ToString(); // Suggest +128MB
            TxtNewSize.Focus();
            TxtNewSize.SelectAll();
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            if (!long.TryParse(TxtNewSize.Text, out long newSizeMB) || newSizeMB <= 0)
            {
                MessageBox.Show(
                    LanguageManager.Instance["MsgInvalidSize"],
                    LanguageManager.Instance["Error"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            long newSizeInBytes = newSizeMB * 1024 * 1024;
            if (newSizeInBytes <= _currentSizeInBytes)
            {
                MessageBox.Show(
                    LanguageManager.Instance["MsgSizeMustBeLarger"],
                    LanguageManager.Instance["Error"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            NewSizeInBytes = newSizeInBytes;
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
