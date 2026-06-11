using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace ImDiskGui
{
    public static class ImDiskNativeApi
    {
        // --- CONSTANTS ---
        public const uint GENERIC_READ = 0x80000000;
        public const uint GENERIC_WRITE = 0x40000000;
        public const uint FILE_SHARE_READ = 0x00000001;
        public const uint FILE_SHARE_WRITE = 0x00000002;
        public const uint OPEN_EXISTING = 3;

        // ImDisk Device types
        public const uint IMDISK_DEVICE_TYPE_HD = 0x00000010;
        public const uint IMDISK_DEVICE_TYPE_FD = 0x00000020;
        public const uint IMDISK_DEVICE_TYPE_CD = 0x00000030;
        public const uint IMDISK_DEVICE_TYPE_RAW = 0x00000040;

        // ImDisk Backing types
        public const uint IMDISK_TYPE_FILE = 0x00000100;
        public const uint IMDISK_TYPE_VM = 0x00000200;
        public const uint IMDISK_TYPE_PROXY = 0x00000300;

        // ImDisk Options
        public const uint IMDISK_OPTION_RO = 0x00000001;
        public const uint IMDISK_OPTION_REMOVABLE = 0x00000002;
        public const uint IMDISK_OPTION_SPARSE_FILE = 0x00000004;
        public const uint IMDISK_OPTION_BYTE_SWAP = 0x00000008;

        // --- DELEGATES & ENUMS ---
        [Flags]
        public enum ImDiskAPIFlags : ulong
        {
            NoBroadcastNotify = 0x1,
            ForceDismount = 0x2
        }

        public enum MediaType : int
        {
            Unknown = 0,
            F5_1Pt2_512 = 1,     // 5.25", 1.2MB, 512 bytes/sector
            F3_1Pt44_512 = 2,    // 3.5", 1.44MB, 512 bytes/sector
            F3_2Pt88_512 = 3,    // 3.5", 2.88MB, 512 bytes/sector
            F3_20Pt8_512 = 4,    // 3.5", 20.8MB, 512 bytes/sector
            F3_720_512 = 5,      // 3.5", 720KB, 512 bytes/sector
            F5_360_512 = 6,      // 5.25", 360KB, 512 bytes/sector
            F5_320_512 = 7,      // 5.25", 320KB, 512 bytes/sector
            F5_320_1024 = 8,     // 5.25", 320KB, 1024 bytes/sector
            F5_180_512 = 9,      // 5.25", 180KB, 512 bytes/sector
            F5_160_512 = 10,     // 5.25", 160KB, 512 bytes/sector
            RemovableMedia = 11, // Removable media other than floppy
            FixedMedia = 12      // Fixed hard disk media
        }

        // --- STRUCTS ---
        [StructLayout(LayoutKind.Sequential)]
        public struct DISK_GEOMETRY
        {
            public long Cylinders;
            public MediaType MediaType;
            public uint TracksPerCylinder;
            public uint SectorsPerTrack;
            public uint BytesPerSector;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct ImDiskCreateData
        {
            public int DeviceNumber;
            private readonly int _Dummy; // 4-byte padding to align Int64
            public long DiskSize;
            public int MediaType;
            public uint TracksPerCylinder;
            public uint SectorsPerTrack;
            public uint BytesPerSector;
            public long ImageOffset;
            public uint Flags;
            public char DriveLetter;
            private ushort _FilenameLength;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16384)]
            private string _Filename;

            public string Filename
            {
                get
                {
                    if (_Filename != null && _Filename.Length > _FilenameLength / 2)
                    {
                        return _Filename.Substring(0, _FilenameLength / 2);
                    }
                    return _Filename;
                }
                set
                {
                    if (value == null)
                    {
                        _Filename = null;
                        _FilenameLength = 0;
                        return;
                    }
                    _Filename = value;
                    _FilenameLength = (ushort)(_Filename.Length * 2);
                }
            }
        }

        // --- P/INVOKE DECLARATIONS (imdisk.cpl) ---

        [DllImport("imdisk.cpl", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern ImDiskAPIFlags ImDiskGetAPIFlags();

        [DllImport("imdisk.cpl", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern ImDiskAPIFlags ImDiskSetAPIFlags(ImDiskAPIFlags Flags);

        [DllImport("imdisk.cpl", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ImDiskStartService([MarshalAs(UnmanagedType.LPWStr)] string ServiceName);

        [DllImport("imdisk.cpl", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ImDiskCreateDeviceEx(
            IntPtr hWndStatusText,
            ref uint DeviceNumber,
            ref DISK_GEOMETRY DiskGeometry,
            ref long ImageOffset,
            uint Flags,
            [MarshalAs(UnmanagedType.LPWStr)] string Filename,
            bool NativePath,
            [MarshalAs(UnmanagedType.LPWStr)] string MountPoint
        );

        [DllImport("imdisk.cpl", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ImDiskRemoveDevice(
            IntPtr hWndStatusText,
            uint DeviceNumber,
            [MarshalAs(UnmanagedType.LPWStr)] string MountPoint
        );

        [DllImport("imdisk.cpl", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ImDiskForceRemoveDevice(
            SafeFileHandle DeviceHandle,
            uint DeviceNumber
        );

        [DllImport("imdisk.cpl", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ImDiskForceRemoveDevice(
            IntPtr DeviceHandle,
            uint DeviceNumber
        );

        [DllImport("imdisk.cpl", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ImDiskQueryDevice(
            uint DeviceNumber,
            ref ImDiskCreateData CreateData,
            int CreateDataSize
        );

        [DllImport("imdisk.cpl", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern char ImDiskFindFreeDriveLetter();

        [DllImport("imdisk.cpl", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ImDiskGetDeviceListEx(
            int ListLength,
            int[] DeviceList
        );

        [DllImport("imdisk.cpl", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ImDiskSaveRegistrySettings(ref ImDiskCreateData CreateData);

        [DllImport("imdisk.cpl", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ImDiskRemoveRegistrySettings(uint DeviceNumber);

        [DllImport("imdisk.cpl", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ImDiskNotifyShellDriveLetter(
            IntPtr WindowHandle,
            [MarshalAs(UnmanagedType.LPWStr)] string DriveLetterPath
        );

        [DllImport("imdisk.cpl", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ImDiskNotifyRemovePending(
            IntPtr WindowHandle,
            char DriveLetter
        );

        [DllImport("imdisk.cpl", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ImDiskSaveImageFile(
            SafeFileHandle DeviceHandle,
            SafeFileHandle FileHandle,
            uint BufferSize,
            IntPtr CancelFlagPtr
        );

        [DllImport("imdisk.cpl", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ImDiskGetVolumeSize(
            SafeFileHandle Handle,
            ref long Size
        );

        [DllImport("imdisk.cpl", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ImDiskExtendDevice(
            IntPtr WindowHandle,
            uint DeviceNumber,
            ref long ExtendSize
        );

        // --- P/INVOKE DECLARATIONS (fmifs.dll - Native Formatting) ---
        public delegate bool FormatCallback(int command, uint modifier, IntPtr parameter);

        [DllImport("fmifs.dll", CharSet = CharSet.Unicode, EntryPoint = "FormatEx")]
        public static extern void FormatEx(
            string driveLetter,
            uint mediaFlag,
            string fsType,
            string label,
            bool quickFormat,
            uint clusterSize,
            FormatCallback callback
        );

        // --- P/INVOKE DECLARATIONS (kernel32.dll) ---
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetDllDirectory(string lpPathName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped
        );

        public const uint IOCTL_IMDISK_SET_DEVICE_FLAGS = 0x83722014;
        public const uint IMDISK_IMAGE_MODIFIED = 0x00010000;

        [StructLayout(LayoutKind.Sequential)]
        public struct IMDISK_SET_DEVICE_FLAGS
        {
            public uint FlagsToChange;
            public uint FlagValues;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            uint dwIoControlCode,
            ref IMDISK_SET_DEVICE_FLAGS lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool FlushFileBuffers(SafeFileHandle hFile);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool GetDiskFreeSpaceEx(
            string lpDirectoryName,
            out ulong lpFreeBytesAvailable,
            out ulong lpTotalNumberOfBytes,
            out ulong lpTotalNumberOfFreeBytes
        );

    }
}
