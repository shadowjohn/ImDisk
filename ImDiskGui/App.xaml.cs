using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace ImDiskGui
{
    public partial class App : Application
    {
        private static Mutex _mutex = null;
        private NotifyIcon _notifyIcon;
        private MainWindow _mainWindow;

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) => LogException(e.ExceptionObject as Exception, "AppDomain");
            DispatcherUnhandledException += (s, e) => {
                LogException(e.Exception, "Dispatcher");
                e.Handled = true;
            };
        }

        private void LogException(Exception ex, string source)
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
                string message = $"[{DateTime.Now}] [{source}] {ex?.ToString()}{Environment.NewLine}{Environment.NewLine}";
                File.AppendAllText(logPath, message);
                System.Windows.MessageBox.Show($"An unhandled exception occurred ({source}): {ex?.Message}\n\nSee crash.log for details.", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch { }
        }

        private ToolStripMenuItem _menuOpen;
        private ToolStripMenuItem _menuExit;

        private void UpdateTrayLanguage()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Text = LanguageManager.Instance["AppTitle"];
            }
            if (_menuOpen != null)
            {
                _menuOpen.Text = LanguageManager.Instance["TrayOpen"];
            }
            if (_menuExit != null)
            {
                _menuExit.Text = LanguageManager.Instance["TrayExit"];
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            ConfigureImDiskNativePath();

            // Load language settings
            try
            {
                string settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ImDiskGui");
                string langFile = Path.Combine(settingsDir, "language.txt");
                if (File.Exists(langFile))
                {
                    string savedLang = File.ReadAllText(langFile).Trim();
                    if (savedLang == "zh-TW" || savedLang == "en-US")
                    {
                        LanguageManager.Instance.CurrentLanguage = savedLang;
                    }
                }
            }
            catch { }

            // Load theme settings
            try
            {
                string settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ImDiskGui");
                string themeFile = Path.Combine(settingsDir, "theme.txt");
                if (File.Exists(themeFile))
                {
                    string savedTheme = File.ReadAllText(themeFile).Trim();
                    if (Enum.TryParse(savedTheme, out ThemeManager.ThemeMode mode))
                    {
                        ThemeManager.CurrentTheme = mode;
                    }
                    else
                    {
                        ThemeManager.CurrentTheme = ThemeManager.ThemeMode.Light; // Default to Light
                    }
                }
                else
                {
                    ThemeManager.CurrentTheme = ThemeManager.ThemeMode.Light; // Default to Light
                }
            }
            catch
            {
                ThemeManager.CurrentTheme = ThemeManager.ThemeMode.Light;
            }

            // Listen for language changes to update tray
            LanguageManager.Instance.PropertyChanged += (s, ev) => {
                if (ev.PropertyName == "CurrentLanguage" || string.IsNullOrEmpty(ev.PropertyName))
                {
                    UpdateTrayLanguage();
                }
            };

            const string mutexName = "Global\\ImDiskGuiMutex_stw_s";
            _mutex = new Mutex(true, mutexName, out bool isNewInstance);

            if (!isNewInstance)
            {
                System.Windows.MessageBox.Show(
                    LanguageManager.Instance["AlreadyRunning"],
                    LanguageManager.Instance["Info"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                Shutdown();
                return;
            }

            base.OnStartup(e);

            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Load tray icon from the executable itself so the package does not need a loose .ico file.
            _notifyIcon = new NotifyIcon();

            try
            {
                string exePath = Assembly.GetExecutingAssembly().Location;
                Icon appIcon = Icon.ExtractAssociatedIcon(exePath);
                if (appIcon != null)
                {
                    _notifyIcon.Icon = appIcon;
                }
                else
                {
                    _notifyIcon.Icon = SystemIcons.Application;
                }
            }
            catch
            {
                _notifyIcon.Icon = SystemIcons.Application;
            }

            _notifyIcon.Visible = true;
            _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            // Context Menu for Tray
            var contextMenu = new ContextMenuStrip();
            _menuOpen = new ToolStripMenuItem(LanguageManager.Instance["TrayOpen"], null, (s, ev) => ShowMainWindow());
            _menuExit = new ToolStripMenuItem(LanguageManager.Instance["TrayExit"], null, (s, ev) => ExitApp());
            contextMenu.Items.Add(_menuOpen);
            contextMenu.Items.Add("-");
            contextMenu.Items.Add(_menuExit);
            _notifyIcon.ContextMenuStrip = contextMenu;

            UpdateTrayLanguage();

            // Show main window on startup
            ShowMainWindow();
        }

        private static void ConfigureImDiskNativePath()
        {
            try
            {
                string cplDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "driver", "cpl", "amd64");
                if (Directory.Exists(cplDir))
                {
                    ImDiskNativeApi.SetDllDirectory(cplDir);
                }
            }
            catch { }
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowMainWindow();
        }

        public void ShowMainWindow()
        {
            if (_mainWindow == null)
            {
                _mainWindow = new MainWindow();
                _mainWindow.Closed += (s, e) => _mainWindow = null;
                _mainWindow.Show();
            }
            else
            {
                if (_mainWindow.WindowState == WindowState.Minimized)
                {
                    _mainWindow.WindowState = WindowState.Normal;
                }
                _mainWindow.Activate();
            }
        }

        private void ExitApp()
        {
            if (_mainWindow != null)
            {
                _mainWindow.CloseAndExit();
            }
            else
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                }
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            base.OnExit(e);
        }
    }
}
