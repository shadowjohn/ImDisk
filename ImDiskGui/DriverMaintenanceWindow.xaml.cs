using System;
using System.IO;
using System.Windows;
using System.Diagnostics;

namespace ImDiskGui
{
    public partial class DriverMaintenanceWindow : Window
    {
        public DriverMaintenanceWindow()
        {
            InitializeComponent();
        }

        private string AppBasePath => AppDomain.CurrentDomain.BaseDirectory;

        private string ResolvePath(params string[] parts)
        {
            string path = AppBasePath;
            foreach (var part in parts)
            {
                path = Path.Combine(path, part);
            }
            return path;
        }

        private bool EnsureDriverPackageReady()
        {
            return File.Exists(ResolvePath("driver", "imdisk.inf")) &&
                   File.Exists(ResolvePath("driver", "sys", "amd64", "imdisk.sys")) &&
                   File.Exists(ResolvePath("driver", "svc", "amd64", "imdsksvc.exe")) &&
                   File.Exists(ResolvePath("driver", "cpl", "amd64", "imdisk.cpl")) &&
                   File.Exists(ResolvePath("driver", "cli", "amd64", "imdisk.exe")) &&
                   File.Exists(ResolvePath("driver", "sys", "i386", "imdisk.sys")) &&
                   File.Exists(ResolvePath("driver", "svc", "i386", "imdsksvc.exe")) &&
                   File.Exists(ResolvePath("driver", "cpl", "i386", "imdisk.cpl")) &&
                   File.Exists(ResolvePath("driver", "cli", "i386", "imdisk.exe")) &&
                   File.Exists(ResolvePath("driver", "uninstall_imdisk.cmd")) &&
                   File.Exists(ResolvePath("uninstall_imdisk.cmd"));
        }

        private void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureDriverPackageReady())
            {
                MessageBox.Show(
                    "找不到 driver payload。請確認 `driver` 目錄與 `uninstall_imdisk.cmd` 已隨程式發佈。",
                    LanguageManager.Instance["Error"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            RunInfInstall(ResolvePath("driver", "imdisk.inf"));
        }

        private void BtnUninstall_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(ResolvePath("uninstall_imdisk.cmd")))
            {
                MessageBox.Show(
                    "找不到 `uninstall_imdisk.cmd`，無法執行移除。",
                    LanguageManager.Instance["Error"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var uninstallScript = string.Join(" ", new[]
            {
                "$drv = Get-CimInstance Win32_PnPSignedDriver | Where-Object {",
                "($_.DeviceName -like '*ImDisk*') -or",
                "($_.Description -like '*ImDisk*') -or",
                "($_.DriverProviderName -like '*LTR Data*') -or",
                "($_.InfName -ieq 'imdisk.inf')",
                "} | Select-Object -First 1",
                "if (-not $drv) { throw '找不到已安裝的 ImDisk driver package。' }",
                "& pnputil.exe /delete-driver $($drv.InfName) /uninstall /force"
            });

            RunElevated(
                "powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -Command \"{uninstallScript.Replace("\"", "\\\"")}\"",
                AppBasePath
            );
        }

        private void RunElevated(string fileName, string arguments, string workingDirectory)
        {
            try
            {
                var psi = new ProcessStartInfo(fileName, arguments)
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    WorkingDirectory = workingDirectory
                };

                var proc = Process.Start(psi);
                if (proc != null)
                {
                    proc.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    LanguageManager.Instance["Error"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void RunInfInstall(string infPath)
        {
            try
            {
                string installer = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                    "System32",
                    "InfDefaultInstall.exe"
                );

                var psi = new ProcessStartInfo(installer, $"\"{infPath}\"")
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    WorkingDirectory = Path.GetDirectoryName(infPath)
                };

                var proc = Process.Start(psi);
                if (proc != null)
                {
                    proc.WaitForExit();
                    if (proc.ExitCode != 0)
                    {
                        MessageBox.Show(
                            LanguageManager.Instance.Format("DriverInstallFailed", $"ExitCode={proc.ExitCode}"),
                            LanguageManager.Instance["Error"],
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    LanguageManager.Instance["Error"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
