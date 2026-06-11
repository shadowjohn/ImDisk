# ImDisk GUI 與 Driver Payload 編譯說明 (GUI Build & Driver Payload Instructions)

本專案已整合 C# WPF GUI 管理介面。GUI 與 driver payload 分開發佈，`ImDiskGui.exe` 只負責管理與呼叫 driver 安裝/移除流程，不再把 driver 合體進單一執行檔。

## 開發動機與設計亮點 (Motivation & Highlights)

### 💡 開發動機
原生的 ImDisk 是一款歷史悠久、穩定性極高的 Windows 虛擬磁碟驅動程式，但其自帶的控制台介面（imdisk.cpl）相對簡陋，且在使用上有一些不夠貼心的地方（例如卸載時若遇鎖定會直接報錯、重開機後清單殘留幽靈磁碟、缺少直觀的效能測試與資料保存設定等）。
為此，本專案基於 **WPF (C# .NET 4.8)** 進行了全新的「拉皮」與功能重構，旨在打造一個現代化、美觀且極度強健的虛擬硬碟管理工具。

### 🌟 設計亮點
* **極致美感與現代化主題**：導入了主流的深色/淺色/跟隨系統主題切換，搭配卡片式扁平化設計，字體也經過了嚴格的 DPI 與顏色同步校正。
* **強健的核心狀態對齊 (0-Ghosting)**：重寫底層 `imdisk.cpl` API 封送（P/Invoke），解決了原版計數索引錯位與字串封送崩潰的陳年 Bug，確保 GUI 清單與核心驅動狀態 100% 同步，電腦重開機後自動清理失效磁碟。
* **安全的核心層強拆機制**：當磁碟被進程鎖定無法正常解除掛載時，GUI 會自動獲取精確裝置編號，設置 `ForceDismount` 並透過核心 API 強拆，確保磁碟順利卸載且完全不彈出報錯。
* **視覺化記憶體效能評測**：內建 Benchmark 功能，免去另外開啟 CrystalDiskMark 的繁瑣，可即時量測極限循序與隨機 4K 讀寫吞吐，並同步監測 CPU、RAM 的本機規格資訊，版面加寬防切字。
* **分離式安全打包架構**：不再將 Driver payload 硬塞入 GUI 執行檔，降低被 Windows Defender 誤判攔截的機率，並將安裝與移除流程做精美的防呆與動態隱藏。

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

## ImDisk GUI 主要功能特性 (GUI Features)

* **現代化主題介面**：基於 WPF 打造的精美 UI，支援深色、淺色與跟隨系統主題，中英文介面無縫切換。
* **強健的狀態同步與強制解除掛載**：
  - 開機或重新載入時自動透過 Windows 底層 API 與真實驅動狀態同步，防止幽靈磁碟殘留。
  - 對於檔案被鎖定的虛擬硬碟，內建 `ForceDismount` 與強制核心卸載 API，確保卸載成功不報錯。
* **內建效能測試 (Benchmark)**：
  - 支援快速、標準與壓力測試，直觀呈現虛擬記憶體磁碟的循序與 4K 隨機寫入/讀取吞吐量。
  - 同步檢視本機 CPU 與系統記憶體狀態。
* **背景防呆驅動管理**：
  - 動態識別驅動載入狀態，智能隱藏目前不可用的操作按鈕（例如安裝完即自動隱藏安裝按鈕並轉為移除按鈕），防止二次操作。
  - 免重啟自動重載介面：一旦驅動安裝完成即可直接進入主畫面。
* **無痛背景資料存檔**：開關機時可自動將記憶碟資料同步保存至指定的實體映像檔，兼顧效能與資料安全。

## 介面展示 (UI Screenshots)

| 主管理介面 (Main Window - Light Theme) | 驅動維護面板 (Maintenance Window) |
|---|---|
| ![主畫面](screenshot/S0.png) | ![驅動維護](screenshot/S1.png) |

| 效能測試面板 (Benchmark - Light Theme) | 效能測試面板 (Benchmark - Dark Theme) |
|---|---|
| ![效能測試 1](screenshot/S3_benchmark.png) | ![效能測試 2](screenshot/S4_benchmark.png) |

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
