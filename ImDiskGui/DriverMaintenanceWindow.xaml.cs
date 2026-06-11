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
            UpdateButtonsState();
        }

        private bool IsDriverServiceInstalled()
        {
            try
            {
                using (var sc = new System.ServiceProcess.ServiceController("ImDisk"))
                {
                    var status = sc.Status;
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private void UpdateButtonsState()
        {
            bool installed = IsDriverServiceInstalled();
            BtnInstall.Visibility = installed ? Visibility.Collapsed : Visibility.Visible;
            BtnUninstall.Visibility = installed ? Visibility.Visible : Visibility.Collapsed;
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

            string targetInf = ResolvePath("driver", "imdisk.inf");
            string tempScript = ResolvePath("driver", "uninstall_imdisk.cmd");
            string sourceScript = ResolvePath("uninstall_imdisk.cmd");

            try
            {
                if (File.Exists(sourceScript))
                {
                    File.Copy(sourceScript, tempScript, true);
                }
                RunInfInstall(targetInf);
            }
            finally
            {
                try
                {
                    if (File.Exists(tempScript))
                    {
                        File.Delete(tempScript);
                    }
                }
                catch { }
            }
        }

        private void BtnUninstall_Click(object sender, RoutedEventArgs e)
        {
            // Check if the service exists before attempting uninstall
            if (!IsDriverServiceInstalled())
            {
                DialogResult = false;
                Close();
                return;
            }

            string localScript = ResolvePath("uninstall_imdisk.cmd");
            string systemScript = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "uninstall_imdisk.cmd");

            string scriptToRun = null;
            if (File.Exists(localScript))
            {
                scriptToRun = localScript;
            }
            else if (File.Exists(systemScript))
            {
                scriptToRun = systemScript;
            }

            if (scriptToRun == null)
            {
                MessageBox.Show(
                    "找不到 `uninstall_imdisk.cmd`，無法執行移除。",
                    LanguageManager.Instance["Error"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            RunElevated(
                "cmd.exe",
                $"/c \"\"{scriptToRun}\"\"",
                Path.GetDirectoryName(scriptToRun)
            );

            // Verify if the service is successfully removed
            bool isRemoved = false;
            try
            {
                using (var sc = new System.ServiceProcess.ServiceController("ImDisk"))
                {
                    var status = sc.Status;
                }
            }
            catch
            {
                isRemoved = true;
            }

            if (isRemoved)
            {
                MessageBox.Show(
                    "驅動程式已成功移除！",
                    LanguageManager.Instance["Info"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                
                DialogResult = false;
                Close();
            }
            else
            {
                MessageBox.Show(
                    "驅動程式移除程式已執行，但偵測到服務仍存在，可能需要重新啟動電腦以完成移除，或是請先確認磁碟是否皆已卸載。",
                    LanguageManager.Instance["Warning"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
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
                    if (proc.ExitCode == 0)
                    {
                        DialogResult = true;
                        MessageBox.Show(
                            "驅動程式安裝成功！",
                            LanguageManager.Instance["Info"],
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        Close();
                    }
                    else
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
