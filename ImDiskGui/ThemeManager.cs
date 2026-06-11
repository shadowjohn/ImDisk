using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace ImDiskGui
{
    public static class ThemeManager
    {
        public enum ThemeMode { Light, Dark, System }
        private static ThemeMode _currentTheme = ThemeMode.Light; // Default to Light

        public static ThemeMode CurrentTheme
        {
            get => _currentTheme;
            set
            {
                _currentTheme = value;
                ApplyTheme();
            }
        }

        public static void ApplyTheme()
        {
            bool isDark = false;
            if (_currentTheme == ThemeMode.Dark)
            {
                isDark = true;
            }
            else if (_currentTheme == ThemeMode.System)
            {
                isDark = GetWindowsThemeIsDark();
            }

            var resources = Application.Current.Resources;
            if (isDark)
            {
                resources["WindowBackgroundBrush"] = NewBrush("#121212");
                resources["ToolbarBackgroundBrush"] = NewBrush("#1E1E1E");
                resources["TextBrush"] = NewBrush("#FFFFFF");
                resources["SecondaryTextBrush"] = NewBrush("#888888");
                resources["BorderBrush"] = NewBrush("#2D2D2D");
                resources["DataGridRowBackground"] = NewBrush("#121212");
                resources["DataGridAlternatingRowBackground"] = NewBrush("#181818");
                resources["DataGridHeaderBackground"] = NewBrush("#1E1E1E");
                resources["DataGridCellSelectedBackground"] = NewBrush("#1F3D20");
                resources["DataGridCellSelectedText"] = NewBrush("#38E54D");
                resources["ToolbarButtonHoverBackground"] = NewBrush("#2D2D2D");
                resources["ButtonBackground"] = NewBrush("#2D2D2D");
                resources["ButtonHoverBackground"] = NewBrush("#3D3D3D");
                resources["TextBoxBackgroundBrush"] = NewBrush("#1E1E1E");
                resources["TextBoxForegroundBrush"] = NewBrush("#FFFFFF");
            }
            else
            {
                resources["WindowBackgroundBrush"] = NewBrush("#F8F9FA");
                resources["ToolbarBackgroundBrush"] = NewBrush("#FFFFFF");
                resources["TextBrush"] = NewBrush("#212529");
                resources["SecondaryTextBrush"] = NewBrush("#495057");
                resources["BorderBrush"] = NewBrush("#DEE2E6");
                resources["DataGridRowBackground"] = NewBrush("#FFFFFF");
                resources["DataGridAlternatingRowBackground"] = NewBrush("#F1F3F5");
                resources["DataGridHeaderBackground"] = NewBrush("#E9ECEF");
                resources["DataGridCellSelectedBackground"] = NewBrush("#E2F0D9");
                resources["DataGridCellSelectedText"] = NewBrush("#2B8A3E");
                resources["ToolbarButtonHoverBackground"] = NewBrush("#E9ECEF");
                resources["ButtonBackground"] = NewBrush("#E9ECEF");
                resources["ButtonHoverBackground"] = NewBrush("#DEE2E6");
                resources["TextBoxBackgroundBrush"] = NewBrush("#FFFFFF");
                resources["TextBoxForegroundBrush"] = NewBrush("#212529");
            }
        }

        private static SolidColorBrush NewBrush(string hex)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }

        private static bool GetWindowsThemeIsDark()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    var val = key?.GetValue("AppsUseLightTheme");
                    if (val is int i)
                    {
                        return i == 0;
                    }
                }
            }
            catch { }
            return false; // Default to Light if cannot query registry
        }
    }
}
