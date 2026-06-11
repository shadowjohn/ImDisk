# ImDisk GUI 開發紀錄

日期：2026-06-11

## 目前方向

- 不採用 All-in-One 合體 exe。
- `ImDiskGui.exe` 與 driver payload 分開發佈，降低防毒誤判風險。
- GUI 仍保留 driver 安裝 / 移除入口。
- 發佈結構目前設計為：
  - `ImDiskGui.exe`
  - `ImDiskGui.exe.config`
  - `uninstall_imdisk.cmd`
  - `driver\imdisk.inf`
  - `driver\sys\amd64\imdisk.sys`
  - `driver\svc\amd64\imdsksvc.exe`
  - `driver\cpl\amd64\imdisk.cpl`
  - `driver\cli\amd64\imdisk.exe`
  - `driver\sys\i386\imdisk.sys`
  - `driver\svc\i386\imdsksvc.exe`
  - `driver\cpl\i386\imdisk.cpl`
  - `driver\cli\i386\imdisk.exe`
  - `driver\uninstall_imdisk.cmd`

## 已完成

- 新增 WPF GUI 專案 `ImDiskGui`。
- 使用使用者提供的角色圖做 icon / tray icon / About avatar。
- About 視窗改版，加入 Gemini / Codex 協作資訊。
- Benchmark 視窗改成快速 / 標準 / 壓力測試，並顯示 CPU、RAM、RAMDisk、耗時。
- GUI output 不再散落 `.ico`。
- `auto_build_gui.ps1` 改成輸出 `ImDiskGui.exe + driver\` 分離式 package。
- 補齊 x64 安裝仍會用到的 i386 helper 檔案，因 `imdisk.inf` 的 `DefaultInstall.ntamd64` 會複製 `ImDiskExe32Files`。
- GUI 啟動時會將 `driver\cpl\amd64` 加入 DLL 搜尋路徑，讓 `[DllImport("imdisk.cpl")]` 能找到外部 payload。
- 卸載 RAM disk 流程改成先用 mount point 正常卸載：
  - `ImDiskRemoveDevice(IntPtr.Zero, 0, disk.DriveLetterString)`
  - 失敗後再用 device number 強制卸載：
  - `ImDiskForceRemoveDevice(IntPtr.Zero, disk.DeviceNumber)`

## Driver 安裝 / 移除目前狀態

- 手動右鍵安裝 `driver\imdisk.inf` 會成功。
- GUI 內安裝流程目前已改成呼叫 Windows 內建：
  - `C:\Windows\System32\InfDefaultInstall.exe "driver\imdisk.inf"`
- 這是為了貼近 Explorer 右鍵「安裝 INF」的行為。
- 仍需要使用實機管理員權限再測一次 GUI 安裝。
- 如果 GUI 安裝仍失敗，下一步優先看：
  - `C:\Windows\inf\setupapi.dev.log`
  - `InfDefaultInstall.exe` exit code
  - 是否因目前 GUI 正在執行，導致 driver / cpl / service 更新被鎖住

## 目前已知注意事項

- `README.md` 與 `auto_build_gui.ps1` 有換行警告：Git 之後可能會把 LF 轉 CRLF，屬於 text=auto 行為。
- 工作樹原本已有多個非 GUI 相關變更，不要隨手 revert：
  - `cli/*.vcxproj`
  - `cpl/*.vcxproj`
  - `svc/*.vcxproj`
  - `sys/*.vcxproj`
  - `imdisk.props`
  - `wdk*.props`
  - `ImDiskNet/VB6test` 刪除狀態
  - `deviotst` 刪除狀態
- 若要驗證 Release 輸出，先關閉正在執行的 `ImDiskGui.exe`，否則 MSBuild 會因 exe 鎖檔而無法覆寫。

## 下一步建議

1. 關閉目前執行中的 GUI。
2. 重新跑 `auto_build_gui.bat`。
3. 從 `ImDiskGui\bin\x64\Release\net48\ImDiskGui.exe` 測試 driver 安裝。
4. 若 GUI 安裝仍失敗，直接打開 `C:\Windows\inf\setupapi.dev.log` 查最後一段 ImDisk / imdisk.inf 記錄。
5. 再測 R: 卸載，若仍失敗，加入 Win32 last error 顯示與 force remove 結果分段訊息。

## Git Push Key

- GitHub push 使用既有 PuTTY PPK key，不另建 OpenSSH key。
- private key：`D:\key\rsa-key-20260513.ppk`
- plink：`C:\Program Files\Common Files\MariaDBShared\HeidiSQL\plink.exe`
- push 時使用：
  - `GIT_SSH_COMMAND="plink.exe -batch -i D:/key/rsa-key-20260513.ppk"`
  - `GIT_SSH_VARIANT=plink`

## 日期：2026-06-11 (優化與修復版)

### 1. 驅動安裝/移除維護流程優化
- **安裝完畢自動重載**：在 `DriverMaintenanceWindow` 中，當驅動安裝順利完成後會回傳 `DialogResult = true`；主視窗 `MainWindow` 接收後，會立刻非同步重新偵測 `ImDisk` 服務並自動初始化主畫面內容，無須重啟程式。
- **徹底移除驅動服務與檔案**：移除驅動改為呼叫官方的 `uninstall_imdisk.cmd`（優先使用本地版本，若無則呼叫系統目錄中的版本），並以管理員權限執行。
- **動態修正移除腳本**：`auto_build_gui.ps1` 會在編譯整理時動態修改 `uninstall_imdisk.cmd`，加入 `if exist "%SystemRoot%\inf\imdisk.inf"` 判斷，若驅動不存在時靜默退出，防範 setupapi 的警告彈窗；若非管理員執行，則輸出提示訊息並 pause。
- **動態隱藏/顯示按鈕**：在驅動維護介面中，如果驅動未安裝，則完全隱藏「解除安裝」按鈕，只顯示「安裝」；如果已安裝，則完全隱藏「安裝」按鈕，只顯示「解除安裝」，防呆體驗更上一層樓。

### 2. 磁碟卸載 (Dismount) 與掛載狀態對齊優化
- **實體掛載狀態同步**：新增 `SyncMountedStatesWithSystem()` 核心方法。之前因 `ImDiskGetDeviceListEx` 的 1-based 陣列索引規則（其 index 0 為裝置總數，裝置編號從 index 1 開始）以及 `ImDiskQueryDevice` 的 struct Unicode ByValTStr 封送處理時常因驅動內部非 null 結束字元崩潰，導致狀態同步失效。
- **P/Invoke 重構**：改為比照 official `ImDiskNet` 函式庫使用 `byte[]` 緩衝區呼叫 `ImDiskQueryDevice` 並直接從 Offset 44 處讀取 `char` (DriveLetter)，100% 避開 Marshaling 錯誤；同時修正 `ImDiskGetDeviceListEx` 陣列索引與長度，成功精準取得系統中真實的掛載狀態與裝置號。這解決了電腦重開機後，磁碟已不存在但 UI 仍顯示為「已掛載」的幽靈磁碟問題。
- **安全防護與強行卸載**：
  - 若卸載時系統中已無此磁碟（例如重開機後），作為無痛防呆直接從 UI 列表中清除，不跳錯誤彈窗。
  - 若磁碟被檔案佔用（In use）導致一般卸載失敗，程式會動態取得當前最新的裝置編號，設置 `ForceDismount` 旗標後，自動以 `ImDiskForceRemoveDevice` 進行核心層級強制拆卸，徹底排除「卸載失敗」的彈窗干擾。

### 3. 介面外觀與細節調整
- **效能測試視窗加寬**：將測試視窗寬度由原本的 `720` 加寬至 `800`，右側系統資訊欄位由 `220` 加寬至 `240`，增加單張測速數據卡片的寬度（從 224px 增加至 260px），能完全避免 MB/s 單位在數據較大時邊緣被截斷的問題。
- **視窗縮小至系統列**：在 `App` 類別中新增了 `ShowTrayNotification` 方法，點擊視窗右上角「X」時，會隱藏主視窗並顯示系統列通知氣球。並已在 `LanguageManager.cs` 中補齊 `TrayMinTitle` 與 `TrayMinText` 的繁體中文與英文翻譯。
- **文字顏色調整**：修正關於視窗中 "Codex" 與 "Gemini" 字體顏色不同步的問題，一律採用動態主題資源 `{DynamicResource TextBrush}`，確保在淺色主題下亦呈黑色。

## 日期：2026-06-11 (1.01 定時自動儲存版)

### README 與發布紀錄
- `README.md` 已同步 1.01 行為，移除舊版「不使用背景計時同步」說明，補上資料保存模式的 `1/3/5/10/30/60` 分鐘自動儲存選項。
- 文件補充 Dirty Check 機制：定時同步會先查詢 `IMDISK_IMAGE_MODIFIED`，只有磁碟真的被修改才寫回備份映像檔。
- 文件補充同步引擎改為透過 `ImDiskSaveImageFile` 儲存完整磁碟映像，成功後清除 modified flag，並保留原子寫入 `.tmp` 交換保護。
- 文件補充既有 `.img` / `.bin` 備份檔會自動偵測實際大小，避免重新掛載時因 MB 換算造成映像檔截斷或大小錯位。
- `CHANGELOG.md` 已補齊 1.01 發布紀錄，包含線上調整 RAM 磁碟大小、備份映像檔同步延伸、完整磁碟映像同步、既有映像檔精準掛載與 README 文件同步。

## 日期：2026-06-11 (GitHub Release 權限修正)

### GitHub Actions
- 推送 `v1.01` tag 後，`Build and Release` workflow 的 build、zip、artifact upload 皆成功，但 `softprops/action-gh-release@v2` 在建立 Release 時失敗：`Resource not accessible by integration`。
- 原因是 workflow 未宣告 `GITHUB_TOKEN` 的 release 寫入權限；已在 `.github/workflows/build.yml` 新增 `permissions: contents: write`，讓後續 `v*` tag 可自動建立 GitHub Release 並上傳 `ImDiskGui_Release.zip`。
- 本次 `v1.01` Release 已手動建立並用 `gh release upload` 補上 `ImDiskGui_Release.zip`。
