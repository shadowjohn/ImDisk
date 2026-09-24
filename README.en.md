# ImDisk RAM Disk Manager (shadowjohn fork)

[繁體中文 README](README.md) · [Releases](https://github.com/shadowjohn/ImDisk/releases)

This is an independent fork of [ImDisk Virtual Disk Driver](https://github.com/LTRData/ImDisk). The original driver and its core design are the work of **Olof Lagerkvist (LTR Data)**. We deeply respect and appreciate his work. This GUI, installer and release stream are maintained by FeatherMountain (shadowjohn); they are not official LTR Data releases or endorsed by Olof.

The project adds a Windows WPF manager for RAM disks. It supports light and dark themes, English and Traditional Chinese, mounting and removal, RAM disk resizing, benchmarks, image backups, periodic saves when data changed, and driver maintenance. The GUI targets .NET Framework 4.8 and x64 Windows. The underlying ImDisk design has known limitations on recent Windows versions; see the [original project](https://github.com/LTRData/ImDisk) before using it for a workload that requires full physical disk behavior.

## Install

Download `ImDiskGui-Setup-<version>.exe` from [this fork's Releases](https://github.com/shadowjohn/ImDisk/releases). The setup wizard selects Traditional Chinese or English according to Windows language and lets you choose either language manually. It installs the GUI, license, and the separate driver payload. It **does not silently install or replace the system driver**.

Launch the app and use its driver maintenance window to install the driver with administrator approval if needed. Keep the full driver directory beside `ImDiskGui.exe`. Uninstalling the GUI does not remove the system driver or your RAM disk image files; use driver maintenance separately, after dismounting all disks.

The GUI's About window has **Check Updates**. It checks the latest release of `shadowjohn/ImDisk`, compares the GUI version, and offers to open the release page. It does not download or run an installer automatically. When upgrading, close the GUI first. Back up any RAM disk data before changing drivers or rebooting.

## Build

1. Install Visual Studio with MSBuild and .NET Framework 4.8 targeting pack.
2. Obtain the original signed `imdisk.zip` driver package and put it in the repository root. The build script checks the signature of `sys/amd64/imdisk.sys` and collects the driver payload.
3. Run `auto_build_gui.bat` on Windows. Output is under `ImDiskGui/bin/x64/Release/net48`.
4. For a setup build, package the GUI output into `ImDiskGui_Release` and compile `installer/ImDiskGui.iss` with Inno Setup 6. The release workflow builds both the ZIP and setup EXE.

The Windows build workflow checks that all driver files exist before packaging. Driver signing and trust depend on the actual payload: a locally rebuilt or modified `imdisk.sys` is **not** made signed by packaging it. The original INF identifies LTR Data as the driver provider. This fork's installer identifies shadowjohn as the GUI publisher.

## Data persistence

RAM disks are volatile. Data written since the last successful image save can be lost after a power failure or crash. Optional image backups and save intervals reduce this window, but do not replace independent backups.

## License and credit

Read [LICENSE.md](LICENSE.md) for the upstream copyright notices and licensing terms, including GPL licensed components. This fork retains the original attribution. Olof Lagerkvist develops [Arsenal Image Mounter](https://github.com/ArsenalRecon/Arsenal-Image-Mounter) for newer use cases. Issues about this fork's GUI, installer, or updates belong in [shadowjohn/ImDisk issues](https://github.com/shadowjohn/ImDisk/issues).
