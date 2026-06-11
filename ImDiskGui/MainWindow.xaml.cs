using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace ImDiskGui
{
    public partial class MainWindow : Window
    {
        public class RamDiskConfig : INotifyPropertyChanged
        {
            private uint _deviceNumber = 0xFFFFFFFF;
            private char _driveLetter;
            private long _sizeInBytes;
            private string _imagePath;
            private string _fileSystem;
            private bool _saveOnShutdown;
            private bool _isRemovable;
            private bool _isMounted;

            public uint DeviceNumber
            {
                get => _deviceNumber;
                set { _deviceNumber = value; OnPropertyChanged(nameof(DeviceNumber)); }
            }

            public char DriveLetter
            {
                get => _driveLetter;
                set { _driveLetter = value; OnPropertyChanged(nameof(DriveLetter)); OnPropertyChanged(nameof(DriveLetterString)); }
            }

            public long SizeInBytes
            {
                get => _sizeInBytes;
                set { _sizeInBytes = value; OnPropertyChanged(nameof(SizeInBytes)); OnPropertyChanged(nameof(SizeString)); }
            }

            public string ImagePath
            {
                get => _imagePath;
                set { _imagePath = value; OnPropertyChanged(nameof(ImagePath)); }
            }

            public string FileSystem
            {
                get => _fileSystem;
                set { _fileSystem = value; OnPropertyChanged(nameof(FileSystem)); }
            }

            public bool SaveOnShutdown
            {
                get => _saveOnShutdown;
                set { _saveOnShutdown = value; OnPropertyChanged(nameof(SaveOnShutdown)); OnPropertyChanged(nameof(SaveOnShutdownString)); }
            }

            public bool IsRemovable
            {
                get => _isRemovable;
                set { _isRemovable = value; OnPropertyChanged(nameof(IsRemovable)); }
            }

            public bool IsMounted
            {
                get => _isMounted;
                set { _isMounted = value; OnPropertyChanged(nameof(IsMounted)); }
            }

            public string DriveLetterString => DriveLetter.ToString() + ":";
            public string SizeString => (SizeInBytes / (1024.0 * 1024.0)).ToString("N0") + " MB";
            public string SaveOnShutdownString => SaveOnShutdown ? "Yes" : "No";

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ObservableCollection<RamDiskConfig> RamDisks { get; set; }
        private readonly string _configFilePath;
        private bool _isExplicitClose = false;

        public MainWindow()
        {
            InitializeComponent();

            _configFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ImDiskGui",
                "config.json"
            );

            RamDisks = new ObservableCollection<RamDiskConfig>();
            GridRamDisks.ItemsSource = RamDisks;

            Loaded += MainWindow_Loaded;

            // Hook session ending to save disks
            SystemEvents.SessionEnding += SystemEvents_SessionEnding;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            bool serviceStarted = await CheckAndStartServiceAsync();

            if (!serviceStarted)
            {
                var result = MessageBox.Show(
                    LanguageManager.Instance["DriverRequiredMessage"],
                    LanguageManager.Instance["DriverRequiredTitle"],
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var win = new DriverMaintenanceWindow
                        {
                            Owner = this
                        };
                        
                        if (win.ShowDialog() == true)
                        {
                            serviceStarted = await CheckAndStartServiceAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("DriverMaintenanceWindow failed: " + ex.Message);
                    }
                }

                if (!serviceStarted)
                {
                    CloseAndExit();
                    return;
                }
            }


            // Initialize Language ComboBox
            if (LanguageManager.Instance.CurrentLanguage == "zh-TW")
            {
                ComboLanguage.SelectedIndex = 0;
            }
            else
            {
                ComboLanguage.SelectedIndex = 1;
            }

            // Initialize Theme ComboBox
            switch (ThemeManager.CurrentTheme)
            {
                case ThemeManager.ThemeMode.Light:
                    ComboTheme.SelectedIndex = 0;
                    break;
                case ThemeManager.ThemeMode.Dark:
                    ComboTheme.SelectedIndex = 1;
                    break;
                case ThemeManager.ThemeMode.System:
                    ComboTheme.SelectedIndex = 2;
                    break;
            }

            LoadConfig();
            RefreshMountedState();
            UpdateMemoryStatus();

            // Auto-mount any unmounted disks on startup
            foreach (var disk in RamDisks.ToList())
            {
                if (!disk.IsMounted)
                {
                    await MountDiskAsync(disk, isNewDisk: false);
                }
            }
        }

        private async Task<bool> CheckAndStartServiceAsync()
        {
            try
            {
                return await Task.Run(() =>
                {
                    using (var sc = new System.ServiceProcess.ServiceController("ImDisk"))
                    {
                        if (sc.Status != System.ServiceProcess.ServiceControllerStatus.Running)
                        {
                            sc.Start();
                            sc.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Running, TimeSpan.FromSeconds(5));
                        }
                    }
                    return true;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ImDisk service check failed: " + ex.Message);
                return false;
            }
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    string json = File.ReadAllText(_configFilePath);
                    // Minimal JSON parser helper to avoid dependency on Newtonsoft.Json
                    var disks = SimpleJsonParser.Deserialize<RamDiskConfig>(json);
                    if (disks != null)
                    {
                        foreach (var disk in disks)
                        {
                            RamDisks.Add(disk);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error loading config: " + ex.Message);
            }
        }

        private void SaveConfig()
        {
            try
            {
                string dir = Path.GetDirectoryName(_configFilePath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string json = SimpleJsonParser.Serialize(RamDisks.ToArray());
                File.WriteAllText(_configFilePath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error saving config: " + ex.Message);
            }
        }

        private void SyncMountedStatesWithSystem()
        {
            try
            {
                uint[] deviceList = new uint[1024];
                if (!ImDiskGetDeviceListEx((uint)deviceList.Length, deviceList))
                {
                    return;
                }

                uint count = deviceList[0];
                var activeDevices = new System.Collections.Generic.Dictionary<char, uint>();

                for (uint i = 1; i <= count; i++)
                {
                    uint devNum = deviceList[i];
                    byte[] buffer = new byte[1024];
                    if (ImDiskQueryDevice(devNum, buffer, buffer.Length))
                    {
                        char deviceDrive = BitConverter.ToChar(buffer, 44);
                        if (deviceDrive != '\0')
                        {
                            char letter = char.ToUpper(deviceDrive);
                            activeDevices[letter] = devNum;
                        }
                    }
                }

                foreach (var disk in RamDisks)
                {
                    char letter = char.ToUpper(disk.DriveLetter);
                    if (activeDevices.TryGetValue(letter, out uint devNum))
                    {
                        disk.IsMounted = true;
                        disk.DeviceNumber = devNum;
                    }
                    else
                    {
                        disk.IsMounted = false;
                        disk.DeviceNumber = 0xFFFFFFFF;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error syncing mounted states: " + ex.Message);
            }
        }

        private void RefreshMountedState()
        {
            SyncMountedStatesWithSystem();

            // Update TxtStatus
            int count = RamDisks.Count(d => d.IsMounted);
            TxtStatus.Text = $"{count} RAM Disk(s) Mounted";
            StatusDot.Fill = count > 0 ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Gray;
        }

        private void UpdateMemoryStatus()
        {
            TxtSystemRam.Text = "Updating system resources...";
            try
            {
                var memStatus = new ImDiskNativeApi.MEMORYSTATUSEX();
                if (ImDiskNativeApi.GlobalMemoryStatusEx(memStatus))
                {
                    double availMB = memStatus.ullAvailPhys / (1024.0 * 1024.0);
                    TxtSystemRam.Text = $"Available System RAM: {availMB:N0} MB";
                }
                else
                {
                    TxtSystemRam.Text = "System RAM info unavailable.";
                }
            }
            catch
            {
                TxtSystemRam.Text = "System RAM info unavailable.";
            }
        }

        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new DiskDialog
            {
                Owner = this
            };

            if (dlg.ShowDialog() == true)
            {
                var newDisk = new RamDiskConfig
                {
                    DriveLetter = dlg.SelectedDriveLetter,
                    SizeInBytes = dlg.SelectedSizeInBytes,
                    ImagePath = dlg.SelectedImagePath,
                    FileSystem = dlg.SelectedFileSystem,
                    SaveOnShutdown = dlg.SaveOnShutdown,
                    IsRemovable = dlg.IsRemovable
                };

                RamDisks.Add(newDisk);
                SaveConfig();

                await MountDiskAsync(newDisk, isNewDisk: true);
            }
        }

        private async Task MountDiskAsync(RamDiskConfig disk, bool isNewDisk = false)
        {
            TxtStatus.Text = LanguageManager.Instance.Format("MsgMounting", disk.DriveLetterString);

            bool success = await Task.Run(() =>
            {
                uint deviceNumber = 0xFFFFFFFF; // Auto
                var geometry = new ImDiskNativeApi.DISK_GEOMETRY
                {
                    Cylinders = disk.SizeInBytes,
                    MediaType = disk.IsRemovable ? ImDiskNativeApi.MediaType.RemovableMedia : ImDiskNativeApi.MediaType.FixedMedia,
                    BytesPerSector = 512,
                    SectorsPerTrack = 63,
                    TracksPerCylinder = 255
                };

                long offset = 0;
                uint flags = ImDiskNativeApi.IMDISK_TYPE_VM | ImDiskNativeApi.IMDISK_DEVICE_TYPE_HD;
                if (disk.IsRemovable) flags |= ImDiskNativeApi.IMDISK_OPTION_REMOVABLE;

                string mountPoint = disk.DriveLetterString;
                string filename = string.IsNullOrEmpty(disk.ImagePath) ? null : disk.ImagePath;

                // If backing image exists and "資料保存" is enabled, we load it.
                if (!string.IsNullOrEmpty(filename) && File.Exists(filename))
                {
                    // Driver will load the file contents automatically into VM memory
                }
                else
                {
                    filename = null; // Fresh RAM disk
                }

                bool mountOk = ImDiskNativeApi.ImDiskCreateDeviceEx(
                    IntPtr.Zero,
                    ref deviceNumber,
                    ref geometry,
                    ref offset,
                    flags,
                    filename,
                    false,
                    mountPoint
                );

                if (mountOk)
                {
                    disk.DeviceNumber = deviceNumber;
                    disk.IsMounted = true;

                    // Format the drive if it is a fresh VM disk (not preloaded from an image)
                    if (string.IsNullOrEmpty(filename))
                    {
                        FormatDrive(mountPoint, disk.FileSystem);
                    }
                    return true;
                }
                return false;
            });

            if (success)
            {
                TxtStatus.Text = LanguageManager.Instance.Format("MsgMountSuccess", disk.DriveLetterString);
                ImDiskNativeApi.ImDiskNotifyShellDriveLetter(IntPtr.Zero, disk.DriveLetterString);
            }
            else
            {
                MessageBox.Show(
                    LanguageManager.Instance.Format("MsgMountFailed", disk.DriveLetterString),
                    LanguageManager.Instance["Error"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                if (isNewDisk)
                {
                    RamDisks.Remove(disk);
                    SaveConfig();
                }
            }

            RefreshMountedState();
            UpdateMemoryStatus();
        }

        private static void FormatDrive(string drivePath, string fsType)
        {
            // Format using fmifs.dll natively
            ImDiskNativeApi.FormatCallback formatCallback = (command, modifier, parameter) =>
            {
                return true;
            };

            ImDiskNativeApi.FormatEx(
                drivePath,
                0x0C, // FMIFS_HARDDISK
                fsType,
                "RAMDISK",
                true, // Quick format
                0,    // Cluster size default
                formatCallback
            );
        }

        [System.Runtime.InteropServices.DllImport("imdisk.cpl", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        private static extern bool ImDiskGetDeviceListEx(uint ListLength, uint[] DeviceList);

        [System.Runtime.InteropServices.DllImport("imdisk.cpl", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        private static extern bool ImDiskQueryDevice(uint DeviceNumber, byte[] CreateData, int CreateDataSize);

        private uint? GetDeviceNumberForDriveLetter(char driveLetter)
        {
            try
            {
                uint[] deviceList = new uint[1024];
                if (!ImDiskGetDeviceListEx((uint)deviceList.Length, deviceList))
                {
                    return null;
                }

                uint count = deviceList[0];
                char targetUpper = char.ToUpper(driveLetter);

                for (uint i = 1; i <= count; i++)
                {
                    uint devNum = deviceList[i];
                    byte[] buffer = new byte[1024];
                    if (ImDiskQueryDevice(devNum, buffer, buffer.Length))
                    {
                        char deviceDrive = BitConverter.ToChar(buffer, 44);
                        if (char.ToUpper(deviceDrive) == targetUpper)
                        {
                            return devNum;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error getting device number: " + ex.Message);
            }
            return null;
        }

        private async void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (GridRamDisks.SelectedItem is RamDiskConfig disk)
            {
                BtnRemove.IsEnabled = false;

                // If 資料保存 is enabled, save now before dismount
                if (disk.SaveOnShutdown && !string.IsNullOrEmpty(disk.ImagePath))
                {
                    await PerformSyncAsync(disk);
                }

                uint? actualDeviceNumber = GetDeviceNumberForDriveLetter(disk.DriveLetter);
                if (actualDeviceNumber == null)
                {
                    // Already not mounted or not found in system
                    disk.IsMounted = false;
                    RamDisks.Remove(disk);
                    SaveConfig();
                    TxtStatus.Text = LanguageManager.Instance.Format("MsgDismountSuccess", disk.DriveLetterString);
                    BtnRemove.IsEnabled = true;
                    RefreshMountedState();
                    UpdateMemoryStatus();
                    return;
                }

                uint devNum = actualDeviceNumber.Value;
                disk.DeviceNumber = devNum;

                TxtStatus.Text = LanguageManager.Instance.Format("MsgDismounting", disk.DriveLetterString);

                bool success = await Task.Run(() =>
                {
                    ImDiskNativeApi.ImDiskNotifyRemovePending(IntPtr.Zero, disk.DriveLetter);

                    // Set API flag to force dismount
                    try
                    {
                        var flags = ImDiskNativeApi.ImDiskGetAPIFlags();
                        ImDiskNativeApi.ImDiskSetAPIFlags(flags | ImDiskNativeApi.ImDiskAPIFlags.ForceDismount);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Error setting API flags: " + ex.Message);
                    }

                    // Try to remove by MountPoint & DeviceNumber
                    if (ImDiskNativeApi.ImDiskRemoveDevice(IntPtr.Zero, devNum, disk.DriveLetterString))
                    {
                        return true;
                    }

                    // Fallback to force remove
                    return ImDiskNativeApi.ImDiskForceRemoveDevice(IntPtr.Zero, devNum);
                });

                if (success)
                {
                    disk.IsMounted = false;
                    RamDisks.Remove(disk);
                    SaveConfig();
                    TxtStatus.Text = LanguageManager.Instance.Format("MsgDismountSuccess", disk.DriveLetterString);
                }
                else
                {
                    MessageBox.Show(
                        LanguageManager.Instance.Format("MsgDismountFailed", disk.DriveLetterString),
                        LanguageManager.Instance["Error"],
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }

                BtnRemove.IsEnabled = true;
                RefreshMountedState();
                UpdateMemoryStatus();
            }
        }

        private async void BtnSync_Click(object sender, RoutedEventArgs e)
        {
            if (GridRamDisks.SelectedItem is RamDiskConfig disk)
            {
                if (string.IsNullOrEmpty(disk.ImagePath))
                {
                    MessageBox.Show(
                        LanguageManager.Instance["MsgNoBackupPath"],
                        LanguageManager.Instance["Info"],
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    return;
                }

                await PerformSyncAsync(disk);
            }
        }

        private async Task PerformSyncAsync(RamDiskConfig disk)
        {
            TxtStatus.Text = LanguageManager.Instance.Format("MsgSyncing", disk.DriveLetterString, Path.GetFileName(disk.ImagePath));

            bool success = await SyncEngine.SaveRamDiskToImageAsync(disk.DriveLetter, disk.ImagePath, (pct) =>
            {
                Dispatcher.Invoke(() =>
                {
                    TxtStatus.Text = LanguageManager.Instance.Format("MsgSyncingProgress", disk.DriveLetterString, pct.ToString("F0"));
                });
            });

            if (success)
            {
                TxtStatus.Text = LanguageManager.Instance.Format("MsgSyncSuccess", disk.DriveLetterString);
            }
            else
            {
                TxtStatus.Text = LanguageManager.Instance.Format("MsgSyncFailed", disk.DriveLetterString);
                MessageBox.Show(
                    LanguageManager.Instance.Format("MsgSyncFailedDetail", disk.ImagePath),
                    LanguageManager.Instance["Error"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void BtnBenchmark_Click(object sender, RoutedEventArgs e)
        {
            if (GridRamDisks.SelectedItem is RamDiskConfig disk && disk.IsMounted)
            {
                var benchWin = new BenchmarkWindow(disk.DriveLetter)
                {
                    Owner = this
                };
                benchWin.ShowDialog();
            }
        }

        private void GridRamDisks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool hasSelection = GridRamDisks.SelectedItem != null;
            BtnRemove.IsEnabled = hasSelection;

            if (GridRamDisks.SelectedItem is RamDiskConfig disk)
            {
                BtnSync.IsEnabled = !string.IsNullOrEmpty(disk.ImagePath) && disk.IsMounted;
                BtnBenchmark.IsEnabled = disk.IsMounted;
            }
            else
            {
                BtnSync.IsEnabled = false;
                BtnBenchmark.IsEnabled = false;
            }
        }

        private async void GridRamDisks_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (GridRamDisks.SelectedItem is RamDiskConfig disk)
            {
                if (!disk.IsMounted)
                {
                    await MountDiskAsync(disk, isNewDisk: false);
                }
                else
                {
                    var benchWin = new BenchmarkWindow(disk.DriveLetter)
                    {
                        Owner = this
                    };
                    benchWin.ShowDialog();
                }
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!_isExplicitClose)
            {
                // Just hide the window, don't exit the application
                e.Cancel = true;
                Hide();

                try
                {
                    var app = System.Windows.Application.Current as App;
                    if (app != null)
                    {
                        app.ShowTrayNotification(
                            LanguageManager.Instance["TrayMinTitle"],
                            LanguageManager.Instance["TrayMinText"]
                        );
                    }
                }
                catch { }
            }
        }

        private void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
        {
            // Windows is shutting down. Save all configured disks synchronously.
            SaveAllDisksSync();
        }

        private void SaveAllDisksSync()
        {
            foreach (var disk in RamDisks)
            {
                if (disk.IsMounted && disk.SaveOnShutdown && !string.IsNullOrEmpty(disk.ImagePath))
                {
                    SyncEngine.SaveRamDiskToImage(disk.DriveLetter, disk.ImagePath);
                }
            }
        }

        public void CloseAndExit()
        {
            _isExplicitClose = true;
            SaveAllDisksSync();
            Close();
            Application.Current.Shutdown();
        }

        private void ComboLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboLanguage == null || ComboLanguage.SelectedItem == null) return;
            var item = (ComboBoxItem)ComboLanguage.SelectedItem;
            string tag = item.Tag.ToString();

            LanguageManager.Instance.CurrentLanguage = tag;

            try
            {
                string settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ImDiskGui");
                if (!Directory.Exists(settingsDir))
                {
                    Directory.CreateDirectory(settingsDir);
                }
                string langFile = Path.Combine(settingsDir, "language.txt");
                File.WriteAllText(langFile, tag);
            }
            catch { }

            if (GridRamDisks != null)
            {
                foreach (var col in GridRamDisks.Columns)
                {
                    var headerBinding = col.Header as System.Windows.Data.BindingExpression;
                    headerBinding?.UpdateTarget();
                }
                GridRamDisks.Items.Refresh();
            }

            RefreshMountedState();
            UpdateMemoryStatus();
        }

        private void ComboTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboTheme == null || ComboTheme.SelectedItem == null) return;
            var item = (ComboBoxItem)ComboTheme.SelectedItem;
            string tag = item.Tag.ToString();

            if (Enum.TryParse(tag, out ThemeManager.ThemeMode mode))
            {
                ThemeManager.CurrentTheme = mode;

                try
                {
                    string settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ImDiskGui");
                    if (!Directory.Exists(settingsDir))
                    {
                        Directory.CreateDirectory(settingsDir);
                    }
                    string themeFile = Path.Combine(settingsDir, "theme.txt");
                    File.WriteAllText(themeFile, tag);
                }
                catch { }
            }
        }

        private void BtnAbout_Click(object sender, RoutedEventArgs e)
        {
            var aboutWin = new AboutWindow
            {
                Owner = this
            };
            aboutWin.ShowDialog();
        }

    }

    // --- SIMPLE JSON PARSER HELPER ---
    // Avoids external library dependencies to remain clean
    public static class SimpleJsonParser
    {
        public static string Serialize<T>(T[] items) where T : MainWindow.RamDiskConfig
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("[");
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                sb.Append("{");
                sb.Append($"\"DeviceNumber\":{item.DeviceNumber},");
                sb.Append($"\"DriveLetter\":\"{item.DriveLetter}\",");
                sb.Append($"\"SizeInBytes\":{item.SizeInBytes},");
                sb.Append($"\"ImagePath\":\"{item.ImagePath?.Replace("\\", "\\\\")}\",");
                sb.Append($"\"FileSystem\":\"{item.FileSystem}\",");
                sb.Append($"\"SaveOnShutdown\":{(item.SaveOnShutdown ? "true" : "false")},");
                sb.Append($"\"IsRemovable\":{(item.IsRemovable ? "true" : "false")}");
                sb.Append("}");
                if (i < items.Length - 1) sb.Append(",");
            }
            sb.Append("]");
            return sb.ToString();
        }

        public static T[] Deserialize<T>(string json)
        {
            try
            {
                json = json.Trim();
                if (string.IsNullOrEmpty(json) || json == "[]") return new T[0];

                // Remove outer brackets
                if (json.StartsWith("[") && json.EndsWith("]"))
                {
                    json = json.Substring(1, json.Length - 2);
                }

                string[] objects = json.Split(new string[] { "}," }, StringSplitOptions.RemoveEmptyEntries);
                var list = new System.Collections.Generic.List<MainWindow.RamDiskConfig>();

                foreach (var objStr in objects)
                {
                    string cleanObj = objStr.Trim().TrimStart('{').TrimEnd('}');
                    string[] properties = cleanObj.Split(',');

                    var config = new MainWindow.RamDiskConfig();
                    foreach (var prop in properties)
                    {
                        string[] kv = prop.Split(new char[] { ':' }, 2);
                        if (kv.Length < 2) continue;

                        string key = kv[0].Trim().Trim('"');
                        string val = kv[1].Trim().Trim('"').Replace("\\\\", "\\");

                        switch (key)
                        {
                            case "DeviceNumber": config.DeviceNumber = uint.Parse(val); break;
                            case "DriveLetter": config.DriveLetter = val[0]; break;
                            case "SizeInBytes": config.SizeInBytes = long.Parse(val); break;
                            case "ImagePath": config.ImagePath = val; break;
                            case "FileSystem": config.FileSystem = val; break;
                            case "SaveOnShutdown": config.SaveOnShutdown = bool.Parse(val); break;
                            case "IsRemovable": config.IsRemovable = bool.Parse(val); break;
                        }
                    }
                    list.Add(config);
                }

                return (T[])(object)list.ToArray();
            }
            catch
            {
                return new T[0];
            }
        }
    }
}
