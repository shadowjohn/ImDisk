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

## 資料保存與映像檔同步機制 (Data Preservation & Image Sync)

為了兼顧記憶體磁碟的極致效能、實體硬碟的壽命，以及使用者資料的安全性，本專案實作了一套健壯且透明的資料保存與備份同步機制：

### 1. 同步原理與方法
* **扇區級備份 (Sector-level Copy)**：同步時，程式會以唯讀模式打開虛擬磁碟的磁區控制控制代碼（例如 `\\.\R:`），直接讀取底層的 Raw 區段資料，並逐區塊寫入目標映像檔中。
* **強制快取清空 (Flush Buffers)**：在開始同步前，程式會呼叫 Windows 核心 API `FlushFileBuffers`，強迫作業系統將目前記憶體中尚未寫入磁碟的快取資料（Dirty Pages）全數排空回寫入 RAM 磁碟，確保映像檔資料與系統當下的檔案狀態一致。
* **原子寫入 (Atomic Swap)**：寫入時會先將資料寫入暫存檔（例如 `backup.img.tmp`）。只有當整份資料 100% 完整複製且排空檔案快取後，才會刪除舊的 `.img` 檔並將 `.tmp` 重新命名。這能確保若在存檔過程中遭遇斷電或異常，原本已備份好的舊映像檔絕對不會損毀。
* **開機載入 (Restore)**：當程式啟動或掛載時，若設定的備份映像檔存在，ImDisk 驅動程式會直接讀取該 `.img` 檔並將其內容載入至分配的記憶體中，完美還原上次關機前的狀態。

### 2. 自動同步頻率與時機
* **不使用背景計時同步**：本程式 **預設不會** 在背景進行每隔幾分鐘的定時循環同步（例如每 5 分鐘寫入一次）。因為 RAM 磁碟通常讀寫極為頻繁，頻繁的背景自動寫入會對實體 SSD 造成巨大的**壽命損耗（Write Endurance Wear）**與額外的 CPU 開銷。
* **觸發式同步時機**：
  1. **系統關機/重啟**：當作業系統關機或重啟時，GUI 會監聽 Windows 關機事件並**同步阻塞**儲存所有已掛載的資料保存記憶碟。
  2. **工作階段結束 (使用者登出)**：使用者登出或 Windows Session 結束時亦會自動觸發存檔。
  3. **手動卸載**：當使用者在 GUI 中點擊「卸載磁碟」時，程式會先自動執行一次同步，確認存檔無誤後再安全解除掛載。
  4. **手動立即存檔**：使用者可在主畫面的工具列隨時點擊「💾 立即存檔」按鈕進行即時備份。

### 3. 安全性與安全信任度
* **斷電與藍屏（BSOD）風險**：因為本質上是記憶體碟，若遭遇突發性斷電、Windows 藍屏當機等未經正常關機程序的異常，**所有自上次同步以來新寫入的資料皆會遺失**。若您正在進行重要工作，強烈建議在關鍵時刻隨手點擊工具列的「立即存檔」。
* **映像檔完好性保證**：由於使用了「原子寫入」機制，即使存檔過程中系統崩潰，也只會遺留一個未完成的 `.tmp` 檔案，原有的備份映像檔仍會保持在「上一次存檔成功」的完好狀態，不會因此而損壞。
* **實體儲存安全**：無背景頻繁寫入，100% 保護 SSD 寫入壽命。

## 常見問答 (FAQ)

### Q1：如果開啟了「資料保存模式」，為什麼主畫面的「效能測試」按鈕是灰色的（無法點選）？
* **A**：這**並非**因為資料保存模式限制了效能測試，而是因為該虛擬磁碟目前在 Windows 中**尚未掛載 (Offline)**（例如電腦剛重新啟動）。
  * 由於 RAM 磁碟是基於記憶體的易失性媒介，當系統重開機時，虛擬磁碟會消失。雖然 GUI 還留有設定檔，但此時 Windows 中並無該磁碟（狀態列會顯示 `0 RAM Disk(s) Mounted`）。
  * 由於沒有實體的磁碟機路徑，測試程式自然無法進行讀寫測試。**只要雙擊該欄位重新掛載，或是當管理程式啟動自動掛載後，按鈕便會恢復可用**。

### Q2：資料保存模式有設定「定時自動同步」的時間嗎？
* **A**：**預設沒有設定任何背景定時自動同步（例如每 5 分鐘存檔一次）。**
  * **原因**：RAM 磁碟通常讀寫極為頻繁，定時自動同步會導致實體 SSD 產生持續不斷的寫入，造成嚴重的**寫入壽命磨損 (SSD Wear)** 並消耗系統效能。
  * **自動同步的時機**：程式採用事件觸發機制，僅在**「系統關機/重新啟動」**、**「使用者工作階段結束/登出」**，以及**「手動於 GUI 解除掛載」**時才會自動執行安全同步。
  * **建議**：若您剛完成極為重要的工作，可以隨時手動點選主畫面工具列的「💾 立即存檔」進行即時備份。

### Q3：如果我卸載了有備份映像檔的磁碟，之後要如何重新掛載回來使用？
* **A**：
  1. 點擊 **「新增磁碟」** 按鈕。
  2. 在 **「備份映像檔路徑 (選填)」** 點擊 **「瀏覽...」** 選擇您原本儲存的 `.img` 備份檔。
  3. 勾選 **「資料保存 (關機時自動存檔)」**。
  4. 輸入大小與磁碟代號，點擊 **「確定」** 即可。底層驅動會自動載入備份檔中的檔案與分割區，不需要重新格式化，完美還原資料。

## 介面展示 (UI Screenshots)

| 主管理介面 (Main Window - Light Theme) | 驅動維護面板 (Maintenance Window) |
|---|---|
| ![主畫面](screenshot/S0.png) | ![驅動維護](screenshot/S1.png) |

| 效能測試面板 (Benchmark - Light Theme) | 效能測試面板 (Benchmark - Dark Theme) |
|---|---|
| ![效能測試 1](screenshot/S3_benchmark.png) | ![效能測試 2](screenshot/S4_benchmark.png) |

| 新增磁碟對話框 (Add Disk Dialog) | |
|---|---|
| ![新增磁碟](screenshot/S5_data_keep_img.png) | |

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
