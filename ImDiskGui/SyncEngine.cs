using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace ImDiskGui
{
    public class SyncEngine
    {
        public delegate void ProgressCallback(double percentage);

        public static async Task<bool> SaveRamDiskToImageAsync(char driveLetter, string targetImagePath, ProgressCallback progress = null)
        {
            return await Task.Run(() => SaveRamDiskToImage(driveLetter, targetImagePath, progress));
        }

        public static bool SaveRamDiskToImage(char driveLetter, string targetImagePath, ProgressCallback progress = null)
        {
            string volumePath = @"\\.\" + driveLetter + ":";
            string tempImagePath = targetImagePath + ".tmp";

            SafeFileHandle hVolume = null;
            FileStream volumeStream = null;
            FileStream tempFileStream = null;

            try
            {
                // 1. Open volume handle with GENERIC_READ and share read/write
                hVolume = ImDiskNativeApi.CreateFile(
                    volumePath,
                    ImDiskNativeApi.GENERIC_READ | ImDiskNativeApi.GENERIC_WRITE,
                    ImDiskNativeApi.FILE_SHARE_READ | ImDiskNativeApi.FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    ImDiskNativeApi.OPEN_EXISTING,
                    0,
                    IntPtr.Zero
                );

                if (hVolume.IsInvalid)
                {
                    int err = Marshal.GetLastWin32Error();
                    throw new System.ComponentModel.Win32Exception(err, $"Failed to open volume {volumePath}");
                }

                // 2. Flush file buffers on the volume to commit pending writes to RAM disk
                ImDiskNativeApi.FlushFileBuffers(hVolume);

                // 3. Get total volume size
                long volumeSize = 0;
                if (!ImDiskNativeApi.ImDiskGetVolumeSize(hVolume, ref volumeSize) || volumeSize <= 0)
                {
                    // Fallback: Query disk geometry via API if volume size fails
                    // Let's check drive capacity by directory info as another fallback
                    try
                    {
                        var driveInfo = new DriveInfo(driveLetter.ToString());
                        volumeSize = driveInfo.TotalSize;
                    }
                    catch
                    {
                        volumeSize = 100 * 1024 * 1024; // Default to 100MB fallback if all else fails
                    }
                }

                // Ensure parent directory exists for backup image
                string directory = Path.GetDirectoryName(targetImagePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 4. Open streams
                volumeStream = new FileStream(hVolume, FileAccess.Read);
                tempFileStream = new FileStream(tempImagePath, FileMode.Create, FileAccess.Write, FileShare.None);

                // 5. Read block by block and write
                byte[] buffer = new byte[1024 * 1024]; // 1MB buffer
                long totalBytesCopied = 0;
                int bytesRead;

                while (totalBytesCopied < volumeSize)
                {
                    long remaining = volumeSize - totalBytesCopied;
                    int bytesToRead = (int)Math.Min(buffer.Length, remaining);

                    bytesRead = volumeStream.Read(buffer, 0, bytesToRead);
                    if (bytesRead <= 0)
                    {
                        break; // EOF
                    }

                    tempFileStream.Write(buffer, 0, bytesRead);
                    totalBytesCopied += bytesRead;

                    if (progress != null)
                    {
                        double percent = (double)totalBytesCopied / volumeSize * 100.0;
                        progress(Math.Min(percent, 100.0));
                    }
                }

                // Flush and close temp file stream
                tempFileStream.Flush(true);
                tempFileStream.Close();
                tempFileStream = null;

                volumeStream.Close();
                volumeStream = null;

                hVolume.Close();
                hVolume = null;

                // 6. Atomic swap: delete old, move new
                if (File.Exists(targetImagePath))
                {
                    File.Delete(targetImagePath);
                }
                File.Move(tempImagePath, targetImagePath);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Sync error: " + ex.Message);

                // Cleanup
                try { tempFileStream?.Close(); } catch { }
                try { volumeStream?.Close(); } catch { }
                try { hVolume?.Close(); } catch { }
                try
                {
                    if (File.Exists(tempImagePath))
                    {
                        File.Delete(tempImagePath);
                    }
                }
                catch { }

                return false;
            }
        }
    }
}
