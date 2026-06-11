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

- 已在 `D:\key` 建立 OpenSSH ed25519 key。
- private key：`D:\key\imdisk_github_ed25519`
- public key：`D:\key\imdisk_github_ed25519.pub`
- fingerprint：`SHA256:c9UYU5tYGkgnbyIchY1nX8hsU+NHUYkkuvE5n5nNqHE`
- comment：`imdisk-github-push-20260611`
