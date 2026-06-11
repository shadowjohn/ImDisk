# Changelog

All notable changes to the ImDisk GUI project will be documented in this file.

## [2026-06-11] - 優化與修復版 (Optimization & Bugfix Release)

### Added
- **實體掛載狀態同步 (`SyncMountedStatesWithSystem`)**：動態對齊系統底層真實的 ImDisk 掛載裝置，徹底解決電腦重開機後 GUI 出現「幽靈磁碟」（已實際被系統釋放但 UI 仍顯示為綠燈）的問題。
- **縮小至系統列氣球提示**：主視窗關閉時自動隱藏並移至 Tray 系統列，顯示「已縮小至系統列」的氣球通知（支援中英文翻譯切換）。
- **強行卸載安全防護**：如遇磁碟被檔案或進程鎖定，自動使用 `ForceDismount` 旗標與 `ImDiskForceRemoveDevice` 核心 API 進行強拆，防止「卸載磁碟失敗」彈窗的干擾。如果該槽早已在系統中移除，則自動作為無痛防呆直接自 UI 列表中抹除，免除錯誤提醒。

### Changed
- **防呆按鈕動態隱藏/顯示**：在驅動安裝維護視窗中，改用 `Visibility.Collapsed` 隱藏目前不可用的按鈕（未安裝驅動時僅顯示「安裝」；已安裝時僅顯示「解除安裝」），取代原先的 `IsEnabled = false` (停用) 樣式。
- **效能測試 (Benchmark) 視窗比例優化**：視窗寬度由 `720` 加寬至 `800`，右側資訊欄加寬至 `240`，防止高達 4.5 GB/s 以上的讀寫數據與單位被截斷，同時讓過長的 CPU 名稱排版更自然。
- **移除腳本權限與靜默機制優化**：動態偵測移除驅動時的系統權限，若無管理員權限則暫停並輸出「請使用系統管理員執行」提示；當系統已無安裝驅動卻誤觸時，會直接靜默退出，防範 setupapi 的「安裝失敗」錯誤彈窗。
- **About 協作字體顏色同步**：調整關於視窗中的「Codex」字體顏色為動態主題資源 `{DynamicResource TextBrush}`，解決在淺色模式下字體為白色的問題。
- **深色模式選單文字顏色修復**：修復深色模式下下拉選單 (ComboBox) 與其選項 (ComboBoxItem) 在關閉或選取時文字呈黑色/暗灰色的對比度問題，改為正確讀取前景色筆刷（深色模式下為白色），大幅提升對比度與辨識度。

---

## [2026-06-11] - 第一版 (Initial Version)
- **全新 WPF GUI 主程式 (`ImDiskGui.exe`)**：基於 .NET Framework 4.8 / WPF 技術重構的現代化磁碟管理介面，支援淺色/深色主題與中英文切換。
- **效能測試 (Benchmark)**：內建快速、標準、壓力測試模式，精確量測虛擬記憶體磁碟的循序與隨機 4K 讀寫吞吐量，並即時讀取系統環境（CPU/RAM/記憶體狀態）。
- **驅動與 binaries 包裝隔離**：主程式不再內嵌 driver payload，採用分離發佈（ package 包含 `ImDiskGui.exe`、`uninstall_imdisk.cmd` 與整個 `driver\` 目錄），降低防毒軟體誤判。
- **資料保存同步**：支援開關機時與實體映像檔（Image file）進行背景同步保存，防止記憶體內資料因斷電或重啟遺失。
