using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace ImDiskGui
{
    public class LanguageManager : INotifyPropertyChanged
    {
        private static LanguageManager _instance;
        public static LanguageManager Instance => _instance ?? (_instance = new LanguageManager());

        private string _currentLanguage = "zh-TW"; // Default to zh-TW

        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    OnPropertyChanged("CurrentLanguage");
                    OnPropertyChanged("Item[]"); // Notifies indexer binding
                }
            }
        }

        private readonly Dictionary<string, Dictionary<string, string>> _translations = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "zh-TW", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "AppTitle", "ImDisk RAM 磁碟管理員" },
                    { "AddDisk", "新增磁碟" },
                    { "Dismount", "卸載磁碟" },
                    { "SaveNow", "立即存檔" },
                    { "Benchmark", "效能測試" },
                    { "ColDrive", "磁碟" },
                    { "ColSize", "大小" },
                    { "ColFS", "檔案系統" },
                    { "ColSave", "資料保存 (關機時)" },
                    { "ColBackup", "備份路徑" },
                    { "StatusMounted", "{0} 個 RAM 磁碟已掛載" },
                    { "SystemRam", "系統可用記憶體: {0} MB" },
                    { "About", "關於" },
                    { "LangZh", "正體中文" },
                    { "LangEn", "English" },
                    { "DriverRequiredTitle", "需要 ImDisk 驅動程式" },
                    { "DriverRequiredMessage", "此系統未安裝或無法啟動 ImDisk 虛擬磁碟驅動程式。\n\n是否開啟驅動安裝/移除介面？" },
                    { "DriverInstallTitle", "安裝 ImDisk 驅動程式" },
                    { "DriverInstallMessage", "偵測到本機已編譯的 ImDisk 驅動程式檔案。\n\n是否要立即安裝此驅動程式？" },
                    { "DriverInstallSuccess", "ImDisk 驅動程式已成功安裝並啟動！" },
                    { "DriverInstallFailed", "安裝 ImDisk 驅動程式失敗: {0}" },
                    { "DriverUninstall", "移除驅動程式" },
                    { "DriverMaintenance", "驅動安裝/移除" },
                    { "DriverMaintenanceFailed", "無法開啟 Windows 程式管理介面。" },
                    { "UninstallConfirmTitle", "確認移除" },
                    { "UninstallConfirmMessage", "您確定要完全移除 ImDisk 虛擬磁碟驅動程式與其相關元件嗎？\n\n請先確認所有虛擬磁碟皆已卸載。" },
                    { "UninstallDismountFirst", "偵測到尚有掛載中的虛擬磁碟。請先將所有磁碟卸載後，再進行移除。" },
                    { "UninstallSuccess", "ImDisk 驅動程式已成功移除！請重新啟動電腦以完成完全清除。" },
                    { "UninstallFailed", "移除驅動程式失敗: {0}" },
                    { "UninstallNeedAdmin", "移除驅動程式需要系統管理員權限。\n請以系統管理員身分執行此程式。" },
                    { "UninstallNotInstalled", "目前系統未安裝 ImDisk 驅動程式" },
                    { "AlreadyRunning", "ImDisk RAM 磁碟管理員已在執行中。" },
                    { "Info", "資訊" },
                    { "Error", "錯誤" },
                    { "DlgAddTitle", "新增 RAM 磁碟" },
                    { "DlgSize", "磁碟大小 (MB):" },
                    { "DlgDrive", "磁碟代號:" },
                    { "DlgFS", "檔案系統:" },
                    { "DlgBackup", "備份映像檔路徑 (選填): 例如: C:\\temp\\R_disk.img" },
                    { "DlgBrowse", "瀏覽..." },
                    { "DlgSave", "資料保存 (關機時自動存檔)" },
                    { "DlgRemovable", "抽取式磁碟 (仿真 USB/軟碟)" },
                    { "DlgFreeRam", "系統可用 RAM: {0} MB / 總計 {1} GB" },
                    { "DlgOK", "確定" },
                    { "DlgCancel", "取消" },
                    { "BenchTitle", "RAM 磁碟效能測試" },
                    { "BenchDrive", "測試磁碟:" },
                    { "BenchStart", "開始測試" },
                    { "BenchSeqRead", "循序讀取" },
                    { "BenchSeqWrite", "循序寫入" },
                    { "BenchRandRead", "隨機 4K 讀取" },
                    { "BenchRandWrite", "隨機 4K 寫入" },
                    { "BenchTesting", "測試中..." },
                    { "BenchFinished", "測試完成" },
                    { "MsgNoBackupPath", "此 RAM 磁碟未設定備份映像檔路徑。" },
                    { "MsgSaveOnShutdownInfo", "請指定映像檔備份路徑以啟用資料保存。" },
                    { "MsgInvalidSize", "請輸入有效的正整數磁碟大小 (MB)。" },
                    { "MsgNoDriveLetter", "請選擇一個可用的磁碟代號。" },
                    { "MsgNoBackupPathSave", "若要使用資料保存，必須指定目標映像檔路徑。" },
                    { "MsgMounting", "正在掛載磁碟 {0}..." },
                    { "MsgMountSuccess", "磁碟 {0} 已成功掛載並格式化。" },
                    { "MsgMountFailed", "掛載磁碟 {0} 失敗。" },
                    { "MsgDismountFailed", "卸載磁碟 {0} 失敗。" },
                    { "MsgSyncFailed", "磁碟 {0} 同步失敗。" },
                    { "MsgSyncFailedDetail", "無法儲存 RAM 磁碟內容至 {0}。" },
                    { "MsgSyncSuccess", "磁碟 {0} 同步存檔成功。" },
                    { "MsgSyncing", "正在將磁碟 {0} 內容儲存至 {1}..." },
                    { "MsgSyncingProgress", "正在儲存磁碟 {0}... {1}%" },
                    { "MsgDismounting", "正在卸載磁碟 {0}..." },
                    { "MsgDismountSuccess", "磁碟 {0} 已成功卸載。" },
                    { "MsgInitFailed", "驅動程式初始化失敗: {0}" },
                    { "AboutTitle", "關於 ImDisk RAM 磁碟管理員" },
                    { "AboutAuthor", "GUI 介面作者" },
                    { "AboutRole", "RAM 磁碟 GUI 管理工具" },
                    { "AboutCore", "核心元件" },
                    { "AboutCollab", "AI 協作" },
                    { "AboutVersion", "版本" },
                    { "TrayOpen", "開啟管理員" },
                    { "TrayExit", "結束" },
                    { "Theme", "介面配色:" },
                    { "ThemeLight", "淡色系" },
                    { "ThemeDark", "深色系" },
                    { "ThemeSystem", "系統預設" },
                    { "DlgOptions", "進階設定:" },
                    { "BenchDesc", "測量檔案系統層的循序與隨機讀寫吞吐量。" },
                    { "DlgClose", "關閉" },
                    { "BenchTestingSeqWrite", "正在測試循序寫入..." },
                    { "BenchTestingSeqRead", "正在測試循序讀取..." },
                    { "BenchTestingRandWrite", "正在測試隨機 4K 寫入..." },
                    { "BenchTestingRandRead", "正在測試隨機 4K 讀取..." },
                    { "BenchModeHint", "真實檔案 I/O 測試" },
                    { "BenchProfile", "測試模式:" },
                    { "BenchQuick", "快速測試 (256 MB)" },
                    { "BenchStandard", "標準測試 (1 GB)" },
                    { "BenchStress", "壓力測試 (4 GB)" },
                    { "BenchSystemSummary", "測試環境" },
                    { "BenchCpu", "CPU" },
                    { "BenchRam", "RAM" },
                    { "BenchRamDisk", "RAMDisk" },
                    { "BenchElapsed", "測試耗時" },
                    { "BenchReady", "準備測試。" },
                    { "BenchFailed", "測試失敗。" },
                    { "BenchNotEnoughSpace", "測試需要 {0} 可用空間，目前磁碟只剩 {1}。" },
                    { "BenchElapsedValue", "{0} 秒" },
                    { "BenchPhysicalRam", "{0} physical" },
                    { "BenchRamDiskValue", "{0} {1}，測試檔 {2}" },
                    { "TrayMinTitle", "已縮小至系統列" },
                    { "TrayMinText", "ImDisk RAM 磁碟管理員仍在背景運行。雙擊此圖示可重新開啟。" },
                    { "ResizeDisk", "調整大小" },
                    { "ResizeTitle", "調整 RAM 磁碟大小" },
                    { "ResizePrompt", "磁碟 {0}: 目前容量為 {1} MB。\n請輸入新的容量 (MB，只能放大)：" },
                    { "MsgSizeMustBeLarger", "新的容量必須大於目前容量。" },
                    { "MsgResizing", "正在調整磁碟 {0} 的大小..." },
                    { "MsgResizeSuccess", "磁碟 {0} 已成功調整大小為 {1} MB。" },
                    { "MsgResizeFailed", "調整磁碟 {0} 大小失敗。" }
                }
            },
            {
                "en-US", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "AppTitle", "ImDisk RAM Disk Manager" },
                    { "AddDisk", "Add Disk" },
                    { "Dismount", "Dismount" },
                    { "SaveNow", "Save Now" },
                    { "Benchmark", "Benchmark" },
                    { "ColDrive", "Drive" },
                    { "ColSize", "Size" },
                    { "ColFS", "File System" },
                    { "ColSave", "Save on Shutdown" },
                    { "ColBackup", "Backup Path" },
                    { "StatusMounted", "{0} RAM Disk(s) Mounted" },
                    { "SystemRam", "Available System RAM: {0} MB" },
                    { "About", "About" },
                    { "LangZh", "正體中文" },
                    { "LangEn", "English" },
                    { "DriverRequiredTitle", "ImDisk Driver Required" },
                    { "DriverRequiredMessage", "The ImDisk Virtual Disk Driver is not installed or cannot be started on this system.\n\nWould you like to open the driver install/uninstall panel?" },
                    { "DriverInstallTitle", "Install ImDisk Driver" },
                    { "DriverInstallMessage", "Locally compiled ImDisk driver files detected.\n\nWould you like to install the driver now?" },
                    { "DriverInstallSuccess", "ImDisk driver has been successfully installed and started!" },
                    { "DriverInstallFailed", "Failed to install ImDisk driver: {0}" },
                    { "DriverUninstall", "Uninstall Driver" },
                    { "DriverMaintenance", "Driver Setup" },
                    { "DriverMaintenanceFailed", "Failed to open Windows app management." },
                    { "UninstallConfirmTitle", "Confirm Uninstall" },
                    { "UninstallConfirmMessage", "Are you sure you want to completely remove the ImDisk Virtual Disk Driver and all related components?\n\nPlease make sure all virtual disks are dismounted first." },
                    { "UninstallDismountFirst", "There are still mounted virtual disks. Please dismount all disks before uninstalling." },
                    { "UninstallSuccess", "ImDisk driver has been successfully removed! Please restart your computer to complete the cleanup." },
                    { "UninstallFailed", "Failed to uninstall driver: {0}" },
                    { "UninstallNeedAdmin", "Uninstalling the driver requires Administrator privileges.\nPlease run this application as Administrator." },
                    { "UninstallNotInstalled", "ImDisk driver is not currently installed" },
                    { "AlreadyRunning", "ImDisk RAM Disk Manager is already running." },
                    { "Info", "Information" },
                    { "Error", "Error" },
                    { "DlgAddTitle", "Add RAM Disk" },
                    { "DlgSize", "Disk Size (MB):" },
                    { "DlgDrive", "Drive Letter:" },
                    { "DlgFS", "File System:" },
                    { "DlgBackup", "Backup Image File Path (Optional): e.g. C:\\temp\\R_disk.img" },
                    { "DlgBrowse", "Browse..." },
                    { "DlgSave", "Data Preservation (Save on Shutdown)" },
                    { "DlgRemovable", "Removable Disk (Emulates USB/Floppy)" },
                    { "DlgFreeRam", "Available RAM: {0} MB / {1} GB Total" },
                    { "DlgOK", "OK" },
                    { "DlgCancel", "Cancel" },
                    { "BenchTitle", "RAM Disk Benchmark" },
                    { "BenchDrive", "Test Drive:" },
                    { "BenchStart", "Run Benchmark" },
                    { "BenchSeqRead", "Sequential Read" },
                    { "BenchSeqWrite", "Sequential Write" },
                    { "BenchRandRead", "Random 4K Read" },
                    { "BenchRandWrite", "Random 4K Write" },
                    { "BenchTesting", "Testing..." },
                    { "BenchFinished", "Finished" },
                    { "MsgNoBackupPath", "This RAM disk is not configured with a backup image file path." },
                    { "MsgSaveOnShutdownInfo", "Please specify a backing Image File Path to enable Data Preservation." },
                    { "MsgInvalidSize", "Please enter a valid positive disk size in MB." },
                    { "MsgNoDriveLetter", "Please select an available Drive Letter." },
                    { "MsgNoBackupPathSave", "To use Data Preservation, you must specify a target Image File Path." },
                    { "MsgMounting", "Mounting drive {0}..." },
                    { "MsgMountSuccess", "Drive {0} mounted and formatted." },
                    { "MsgMountFailed", "Failed to mount drive {0}." },
                    { "MsgDismountFailed", "Failed to dismount drive {0}." },
                    { "MsgSyncFailed", "Sync failed for drive {0}." },
                    { "MsgSyncFailedDetail", "Failed to save RAM Disk contents to {0}." },
                    { "MsgSyncSuccess", "Drive {0} synchronized successfully." },
                    { "MsgSyncing", "Saving drive {0} contents to {1}..." },
                    { "MsgSyncingProgress", "Saving drive {0}... {1}%" },
                    { "MsgDismounting", "Dismounting drive {0}..." },
                    { "MsgDismountSuccess", "Drive {0} dismounted successfully." },
                    { "MsgInitFailed", "Failed to initialize driver: {0}" },
                    { "AboutTitle", "About ImDisk RAM Disk Manager" },
                    { "AboutAuthor", "GUI Author" },
                    { "AboutRole", "RAM Disk GUI Manager" },
                    { "AboutCore", "Core" },
                    { "AboutCollab", "AI Collaboration" },
                    { "AboutVersion", "Version" },
                    { "TrayOpen", "Open Manager" },
                    { "TrayExit", "Exit" },
                    { "Theme", "Theme:" },
                    { "ThemeLight", "Light Theme" },
                    { "ThemeDark", "Dark Theme" },
                    { "ThemeSystem", "System Default" },
                    { "DlgOptions", "Options:" },
                    { "BenchDesc", "Measures real file-system sequential and random I/O throughput." },
                    { "DlgClose", "Close" },
                    { "BenchTestingSeqWrite", "Testing Sequential Write..." },
                    { "BenchTestingSeqRead", "Testing Sequential Read..." },
                    { "BenchTestingRandWrite", "Testing Random 4K Write..." },
                    { "BenchTestingRandRead", "Testing Random 4K Read..." },
                    { "BenchModeHint", "Real file I/O test" },
                    { "BenchProfile", "Profile:" },
                    { "BenchQuick", "Quick Test (256 MB)" },
                    { "BenchStandard", "Standard Test (1 GB)" },
                    { "BenchStress", "Stress Test (4 GB)" },
                    { "BenchSystemSummary", "Test Environment" },
                    { "BenchCpu", "CPU" },
                    { "BenchRam", "RAM" },
                    { "BenchRamDisk", "RAMDisk" },
                    { "BenchElapsed", "Elapsed" },
                    { "BenchReady", "Ready to run." },
                    { "BenchFailed", "Failed." },
                    { "BenchNotEnoughSpace", "The selected test needs {0} free space. This drive only has {1} available." },
                    { "BenchElapsedValue", "{0} sec" },
                    { "BenchPhysicalRam", "{0} physical" },
                    { "BenchRamDiskValue", "{0} {1}, test file {2}" },
                    { "TrayMinTitle", "Minimized to Tray" },
                    { "TrayMinText", "ImDisk RAM Disk Manager is still running in the background. Double-click this icon to restore." },
                    { "ResizeDisk", "Resize" },
                    { "ResizeTitle", "Resize RAM Disk" },
                    { "ResizePrompt", "Drive {0}: Current size is {1} MB.\nEnter new size (MB, can only grow):" },
                    { "MsgSizeMustBeLarger", "New size must be larger than current size." },
                    { "MsgResizing", "Resizing drive {0}..." },
                    { "MsgResizeSuccess", "Drive {0} successfully resized to {1} MB." },
                    { "MsgResizeFailed", "Failed to resize drive {0}." }
                }
            }
        };

        public string this[string key]
        {
            get
            {
                if (_translations.TryGetValue(CurrentLanguage, out var langDict) && langDict.TryGetValue(key, out var value))
                {
                    return value;
                }
                // Fallback to English, then to key itself
                if (_translations["en-US"].TryGetValue(key, out value))
                {
                    return value;
                }
                return key;
            }
        }

        public string Format(string key, params object[] args)
        {
            try
            {
                return string.Format(this[key], args);
            }
            catch
            {
                return this[key];
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
