using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace ImDiskGui
{
    public partial class BenchmarkWindow : Window
    {
        private readonly char _driveLetter;
        private bool _isRunning = false;

        private sealed class BenchmarkProfile
        {
            public string Key { get; set; }
            public long SizeBytes { get; set; }
        }

        public BenchmarkWindow(char driveLetter)
        {
            InitializeComponent();
            _driveLetter = driveLetter;
            TxtDriveLetter.Text = driveLetter.ToString() + ":";
            UpdateHardwareSummary(null);
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning) return;

            BenchmarkProfile profile = GetSelectedProfile();
            string testFilePath = $"{_driveLetter}:\\__imdisk_bench_test.dat";

            try
            {
                var driveInfo = new DriveInfo(_driveLetter.ToString());
                const long safetyReserve = 64L * 1024 * 1024;
                if (driveInfo.AvailableFreeSpace < profile.SizeBytes + safetyReserve)
                {
                    MessageBox.Show(
                        LanguageManager.Instance.Format("BenchNotEnoughSpace", FormatBytes(profile.SizeBytes), FormatBytes(driveInfo.AvailableFreeSpace)),
                        LanguageManager.Instance["Error"],
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }
            }
            catch
            {
                // 若磁碟資訊暫時查不到，仍允許測試嘗試執行，由檔案 I/O 回報實際錯誤。
            }

            _isRunning = true;
            BtnStart.IsEnabled = false;
            BtnClose.IsEnabled = false;
            ComboTestProfile.IsEnabled = false;

            TxtSeqRead.Text = "...";
            TxtSeqWrite.Text = "...";
            TxtRandRead.Text = "...";
            TxtRandWrite.Text = "...";
            TxtElapsedInfo.Text = "-";
            UpdateHardwareSummary(profile);

            var totalStopwatch = Stopwatch.StartNew();

            try
            {
                await Task.Run(() => RunTestProfile(testFilePath, profile));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Benchmark failed: " + ex.Message, LanguageManager.Instance["Error"], MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatus.Text = LanguageManager.Instance["BenchFailed"];
            }
            finally
            {
                totalStopwatch.Stop();
                TxtElapsedInfo.Text = LanguageManager.Instance.Format("BenchElapsedValue", totalStopwatch.Elapsed.TotalSeconds.ToString("F1"));

                try
                {
                    if (File.Exists(testFilePath))
                    {
                        File.Delete(testFilePath);
                    }
                }
                catch { }

                _isRunning = false;
                BtnStart.IsEnabled = true;
                BtnClose.IsEnabled = true;
                ComboTestProfile.IsEnabled = true;
            }
        }

        private BenchmarkProfile GetSelectedProfile()
        {
            string key = "Quick";
            if (ComboTestProfile.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                key = item.Tag.ToString();
            }

            switch (key)
            {
                case "Stress":
                    return new BenchmarkProfile { Key = key, SizeBytes = 4L * 1024 * 1024 * 1024 };
                case "Standard":
                    return new BenchmarkProfile { Key = key, SizeBytes = 1024L * 1024 * 1024 };
                default:
                    return new BenchmarkProfile { Key = "Quick", SizeBytes = 256L * 1024 * 1024 };
            }
        }

        private void RunTestProfile(string filePath, BenchmarkProfile profile)
        {
            const int seqBlockSize = 4 * 1024 * 1024;
            const int randBlockSize = 4 * 1024;
            long testSize = profile.SizeBytes;
            long randTestSize = Math.Min(testSize / 16, 64L * 1024 * 1024);

            byte[] seqBuffer = new byte[seqBlockSize];
            byte[] randBuffer = new byte[randBlockSize];
            Random dataRandom = new Random(42);
            dataRandom.NextBytes(seqBuffer);
            dataRandom.NextBytes(randBuffer);

            Stopwatch sw = new Stopwatch();

            UpdateStatus(LanguageManager.Instance["BenchTestingSeqWrite"], 0);
            sw.Restart();
            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, seqBlockSize, FileOptions.WriteThrough))
            {
                long written = 0;
                while (written < testSize)
                {
                    int bytesToWrite = (int)Math.Min(seqBlockSize, testSize - written);
                    fs.Write(seqBuffer, 0, bytesToWrite);
                    written += bytesToWrite;
                    UpdateProgress((int)((double)written / testSize * 25));
                }
                fs.Flush(true);
            }
            sw.Stop();
            UpdateSpeedResult(TxtSeqWrite, testSize, sw.Elapsed);

            UpdateStatus(LanguageManager.Instance["BenchTestingSeqRead"], 25);
            sw.Restart();
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, seqBlockSize, FileOptions.SequentialScan))
            {
                long read = 0;
                while (read < testSize)
                {
                    int bytesRead = fs.Read(seqBuffer, 0, (int)Math.Min(seqBlockSize, testSize - read));
                    if (bytesRead <= 0) break;
                    read += bytesRead;
                    UpdateProgress(25 + (int)((double)read / testSize * 25));
                }
            }
            sw.Stop();
            UpdateSpeedResult(TxtSeqRead, testSize, sw.Elapsed);

            UpdateStatus(LanguageManager.Instance["BenchTestingRandWrite"], 50);
            sw.Restart();
            Random offsetRandom = new Random(2026);
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None, randBlockSize, FileOptions.WriteThrough))
            {
                long written = 0;
                long maxBlock = Math.Max(1, (testSize - randBlockSize) / randBlockSize);
                while (written < randTestSize)
                {
                    long offset = offsetRandom.Next(0, (int)Math.Min(maxBlock, int.MaxValue)) * (long)randBlockSize;
                    fs.Position = offset;
                    fs.Write(randBuffer, 0, randBlockSize);
                    written += randBlockSize;
                    UpdateProgress(50 + (int)((double)written / randTestSize * 25));
                }
                fs.Flush(true);
            }
            sw.Stop();
            UpdateSpeedResult(TxtRandWrite, randTestSize, sw.Elapsed);

            UpdateStatus(LanguageManager.Instance["BenchTestingRandRead"], 75);
            sw.Restart();
            offsetRandom = new Random(2026);
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, randBlockSize, FileOptions.RandomAccess))
            {
                long read = 0;
                long maxBlock = Math.Max(1, (testSize - randBlockSize) / randBlockSize);
                while (read < randTestSize)
                {
                    long offset = offsetRandom.Next(0, (int)Math.Min(maxBlock, int.MaxValue)) * (long)randBlockSize;
                    fs.Position = offset;
                    int bytesRead = fs.Read(randBuffer, 0, randBlockSize);
                    if (bytesRead <= 0) break;
                    read += bytesRead;
                    UpdateProgress(75 + (int)((double)read / randTestSize * 25));
                }
            }
            sw.Stop();
            UpdateSpeedResult(TxtRandRead, randTestSize, sw.Elapsed);

            UpdateStatus(LanguageManager.Instance["BenchFinished"], 100);
        }

        private void UpdateHardwareSummary(BenchmarkProfile profile)
        {
            TxtCpuInfo.Text = GetCpuName();
            TxtRamInfo.Text = GetMemorySummary();
            TxtRamDiskInfo.Text = GetRamDiskSummary(profile);
        }

        private string GetCpuName()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0"))
                {
                    string cpuName = key?.GetValue("ProcessorNameString") as string;
                    if (!string.IsNullOrWhiteSpace(cpuName))
                    {
                        return cpuName.Trim();
                    }
                }
            }
            catch { }

            return Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "-";
        }

        private string GetMemorySummary()
        {
            try
            {
                ulong totalBytes = 0;
                uint speed = 0;
                ushort memoryType = 0;

                using (var searcher = new ManagementObjectSearcher("SELECT Capacity, Speed, SMBIOSMemoryType FROM Win32_PhysicalMemory"))
                using (ManagementObjectCollection results = searcher.Get())
                {
                    foreach (ManagementObject item in results)
                    {
                        if (item["Capacity"] != null)
                        {
                            totalBytes += Convert.ToUInt64(item["Capacity"]);
                        }
                        if (item["Speed"] != null)
                        {
                            speed = Math.Max(speed, Convert.ToUInt32(item["Speed"]));
                        }
                        if (memoryType == 0 && item["SMBIOSMemoryType"] != null)
                        {
                            memoryType = Convert.ToUInt16(item["SMBIOSMemoryType"]);
                        }
                    }
                }

                if (totalBytes > 0)
                {
                    string type = GetMemoryTypeName(memoryType);
                    string speedText = speed > 0 ? "-" + speed : string.Empty;
                    return $"{FormatBytes((long)totalBytes)} {type}{speedText}";
                }
            }
            catch { }

            try
            {
                var memStatus = new ImDiskNativeApi.MEMORYSTATUSEX();
                if (ImDiskNativeApi.GlobalMemoryStatusEx(memStatus))
                {
                    return LanguageManager.Instance.Format("BenchPhysicalRam", FormatBytes((long)memStatus.ullTotalPhys));
                }
            }
            catch { }

            return "-";
        }

        private string GetRamDiskSummary(BenchmarkProfile profile)
        {
            try
            {
                var drive = new DriveInfo(_driveLetter.ToString());
                string testSize = profile != null ? FormatBytes(profile.SizeBytes) : "-";
                return LanguageManager.Instance.Format("BenchRamDiskValue", FormatBytes(drive.TotalSize), drive.DriveFormat, testSize);
            }
            catch
            {
                return "-";
            }
        }

        private static string GetMemoryTypeName(ushort type)
        {
            switch (type)
            {
                case 24:
                    return "DDR3";
                case 26:
                    return "DDR4";
                case 34:
                    return "DDR5";
                default:
                    return "RAM";
            }
        }

        private static string FormatBytes(long bytes)
        {
            const double kb = 1024.0;
            const double mb = kb * 1024.0;
            const double gb = mb * 1024.0;

            if (bytes >= gb)
            {
                return (bytes / gb).ToString(bytes % (long)gb == 0 ? "N0" : "N1") + " GB";
            }
            return (bytes / mb).ToString("N0") + " MB";
        }

        private void UpdateStatus(string message, int percent)
        {
            Dispatcher.Invoke(() =>
            {
                TxtStatus.Text = message;
                ProgressBenchmark.Value = percent;
            });
        }

        private void UpdateProgress(int percent)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBenchmark.Value = percent;
            });
        }

        private void UpdateSpeedResult(TextBlock textBlock, long bytes, TimeSpan elapsed)
        {
            double seconds = Math.Max(elapsed.TotalSeconds, 0.001);
            double speed = bytes / (1024.0 * 1024.0) / seconds;

            Dispatcher.Invoke(() =>
            {
                textBlock.Text = $"{speed:F2} MB/s";
            });
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning) return;
            Close();
        }
    }
}
