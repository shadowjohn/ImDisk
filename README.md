# ImDisk GUI 與 Driver Payload 編譯說明 (GUI Build & Driver Payload Instructions)

本專案已整合 C# WPF GUI 管理介面。GUI 與 driver payload 分開發佈，`ImDiskGui.exe` 只負責管理與呼叫 driver 安裝/移除流程，不再把 driver 合體進單一執行檔。

## 編譯與打包步驟

1. **下載官方已數位簽署的驅動包**：
   - 官方下載連結：[https://static.ltr-data.se/files/imdisk.zip](https://static.ltr-data.se/files/imdisk.zip) (或 [https://www.ltr-data.se/files/imdiskinst.zip](https://www.ltr-data.se/files/imdiskinst.zip))
2. **放置檔案**：
   - 將下載好的 `imdisk.zip` (或 `imdiskinst.zip`) 直接放入此專案的根目錄 `d:\mytools\ImDisk\`。
3. **執行編譯與 payload 整理**：
   - 執行根目錄的 [auto_build_gui.bat](file:///D:/mytools/ImDisk/auto_build_gui.bat)。
   - 指令檔會自動執行：
     - 計算並顯示該 ZIP 的 **SHA-256 哈希值**。
     - 解壓縮並透過 Windows Authenticode 驗證 `sys\amd64\imdisk.sys` 是否由作者 **Olof Lagerkvist** / **LTR Data** 所數位簽署。
     - 驗證成功後，將 binaries 整理到獨立的 `driver\` 目錄，供 GUI 的安裝/移除功能使用。
     - x64 安裝段仍會使用 `cli\i386` 與 `cpl\i386` helper，發佈時請保留完整 `driver\` 結構。
     - `uninstall_imdisk.cmd` 會與 `ImDiskGui.exe` 同層發佈。
     - 編譯產出的執行檔與套件將存放在 [ImDiskGui\bin\x64\Release\net48](file:///D:/mytools/ImDisk/ImDiskGui/bin/x64/Release/net48) 目錄。
     - 發佈與分發時，請提供該目錄下的 `ImDiskGui.exe`、`uninstall_imdisk.cmd` 與整個 `driver\` 目錄。

---

# ImDisk Virtual Disk Driver for Windows NT/2000/XP/2003/Vista/7/8/8.1/10

PLEASE NOTE: This project is not recommended on recent versions of Windows
and many applications written for Windows Vista and later require features
that are not supported. It is based on an old design for compatibility with
as old versions as Windows NT 3.51. No new features will be added to this
project but it will remain available here because it could still be useful in
certain scenarios.

I will continue development of Arsenal Image Mounter instead. That has a
different design and emulates complete disks and is compatible with most
cases where physical disk are normally used.
https://github.com/ArsenalRecon/Arsenal-Image-Mounter

Back to this project, ImDisk Virtual Disk driver.
This driver emulates harddisk partitions, floppy drives and CD/DVD-ROM drives
from disk image files, in virtual memory or by redirecting I/O requests
somewhere else, possibly to another machine, through a co-operating user-mode
service, ImDskSvc.

To install this driver, service and command line tool, right-click on the
imdisk.inf file and select 'Install'. To uninstall, use the Add/Remove
Programs applet in the Control Panel.

You can get syntax help to the command line tool by typing just imdisk
without parameters.

I have tested this product under 32-bit versions of Windows NT 3.51, NT 4.0,
2000, XP, Server 2003, Vista, 7, 8, 8.1 and 10 and x86-64 versions of XP,
Server 2003, Vista, 7, 8, 8.1 and 10. Primary target are older versions and
there are several known compatibility issues on modern version of Windows.
Please see website for more details: https://ltr-data.se/opencode.html#ImDisk

The install/uninstall routines do not work under NT 3.51. If you want to use
this product under NT 3.51 you have to manually add registry entries needed
by driver and service or use resource kit tools to add necessary settings.

To install/uninstall on ARM or ARM64 architectures a manual setup is needed.
More about that and other frequently asked questions in the wiki:
https://github.com/LTRData/ImDisk/wiki

  Copyright (c) 2005-2021 Olof Lagerkvist
  https://www.ltr-data.se      olof@ltr-data.se

  Permission is hereby granted, free of charge, to any person
  obtaining a copy of this software and associated documentation
  files (the "Software"), to deal in the Software without
  restriction, including without limitation the rights to use,
  copy, modify, merge, publish, distribute, sublicense, and/or
  sell copies of the Software, and to permit persons to whom the
  Software is furnished to do so, subject to the following
  conditions:

  The above copyright notice and this permission notice shall be
  included in all copies or substantial portions of the Software.

  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
  EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
  OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
  NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
  HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
  WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
  OTHER DEALINGS IN THE SOFTWARE.

  This software contains some GNU GPL licensed code:
  - Parts related to floppy emulation based on VFD by Ken Kato.
    https://web.archive.org/web/20100902032534/http://chitchat.at.infoseek.co.jp:80/vmware/vfd.html
  Copyright (C) Free Software Foundation, Inc.
  Read gpl.txt for the full GNU GPL license.

  This software may contain BSD licensed code:
  - Some code ported to NT from the FreeBSD md driver by Olof Lagerkvist.
    https://www.ltr-data.se
  Copyright (c) The FreeBSD Project.
  Copyright (c) The Regents of the University of California.
