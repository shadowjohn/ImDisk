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

        public static async Task<bool> SaveRamDiskToImageAsync(char driveLetter, string targetImagePath, long diskSize, ProgressCallback progress = null)
        {
            return await Task.Run(() => SaveRamDiskToImage(driveLetter, targetImagePath, diskSize, progress));
        }

        public static bool SaveRamDiskToImage(char driveLetter, string targetImagePath, long diskSize, ProgressCallback progress = null)
        {
            string volumePath = @"\\.\" + driveLetter + ":";
            string tempImagePath = targetImagePath + ".tmp";

            SafeFileHandle hVolume = null;
            SafeFileHandle hFile = null;

            try
            {
                // 1. Open volume handle (needed for flush and modified flag)
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

                // Ensure parent directory exists for backup image
                string directory = Path.GetDirectoryName(targetImagePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 3. Open temp file for writing
                hFile = ImDiskNativeApi.CreateFile(
                    tempImagePath,
                    ImDiskNativeApi.GENERIC_WRITE,
                    0, // No sharing
                    IntPtr.Zero,
                    2, // CREATE_ALWAYS
                    0,
                    IntPtr.Zero
                );

                if (hFile.IsInvalid)
                {
                    int err = Marshal.GetLastWin32Error();
                    throw new System.ComponentModel.Win32Exception(err, $"Failed to create temp image file {tempImagePath}");
                }

                // 4. Use ImDiskSaveImageFile to save the ENTIRE disk image (disk level, not volume level)
                //    This ensures partition table and all disk structures are saved correctly.
                progress?.Invoke(10);

                bool saveOk = ImDiskNativeApi.ImDiskSaveImageFile(
                    hVolume,
                    hFile,
                    1024 * 1024, // 1MB buffer
                    IntPtr.Zero  // No cancel flag
                );

                progress?.Invoke(90);

                if (!saveOk)
                {
                    int err = Marshal.GetLastWin32Error();
                    throw new System.ComponentModel.Win32Exception(err, "ImDiskSaveImageFile failed");
                }

                // 5. Close temp file handle
                hFile.Close();
                hFile = null;

                // 6. Atomic swap: delete old, move new
                if (File.Exists(targetImagePath))
                {
                    File.Delete(targetImagePath);
                }
                File.Move(tempImagePath, targetImagePath);

                progress?.Invoke(95);

                // 7. Clear modified flag since save succeeded
                try
                {
                    var deviceFlags = new ImDiskNativeApi.IMDISK_SET_DEVICE_FLAGS
                    {
                        FlagsToChange = ImDiskNativeApi.IMDISK_IMAGE_MODIFIED,
                        FlagValues = 0
                    };
                    uint bytesReturned;
                    ImDiskNativeApi.DeviceIoControl(
                        hVolume,
                        ImDiskNativeApi.IOCTL_IMDISK_SET_DEVICE_FLAGS,
                        ref deviceFlags,
                        (uint)Marshal.SizeOf(deviceFlags),
                        IntPtr.Zero,
                        0,
                        out bytesReturned,
                        IntPtr.Zero
                    );
                }
                catch (Exception ioEx)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to clear modified flag: " + ioEx.Message);
                }

                hVolume.Close();
                hVolume = null;

                progress?.Invoke(100);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Sync error: " + ex.Message);

                // Cleanup
                try { hFile?.Close(); } catch { }
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
