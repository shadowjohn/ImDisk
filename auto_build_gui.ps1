# UTF-8 with BOM or UTF-8
$OutputEncoding = [System.Text.Encoding]::UTF8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "===================================================" -ForegroundColor Cyan
Write-Host "   ImDisk GUI + Driver Payload 編譯助手 (PowerShell版)" -ForegroundColor Cyan
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Environment Check
Write-Host "[1/4] 正在檢查編譯環境..." -ForegroundColor Yellow

$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vswhere)) {
    $vswhere = "${env:ProgramFiles}\Microsoft Visual Studio\Installer\vswhere.exe"
}

if (-not (Test-Path $vswhere)) {
    Write-Host "[錯誤] 找不到 vswhere.exe。請確認是否已安裝 Visual Studio。" -ForegroundColor Red
    Write-Host "官方下載網址: https://visualstudio.microsoft.com/" -ForegroundColor Gray
    Exit 1
}

$vsPath = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
if ([string]::IsNullOrEmpty($vsPath)) {
    Write-Host "[錯誤] 找不到已安裝 MSBuild 的 Visual Studio 版本。" -ForegroundColor Red
    Write-Host "請透過 Visual Studio Installer 安裝 'MSBuild' 組件。" -ForegroundColor Gray
    Exit 1
}

$msbuildPath = Join-Path $vsPath "MSBuild\Current\Bin\MSBuild.exe"
if (-not (Test-Path $msbuildPath)) {
    Write-Host "[錯誤] 找不到 MSBuild.exe (路徑: $msbuildPath)" -ForegroundColor Red
    Exit 1
}

Write-Host "[資訊] 偵測到 MSBuild: $msbuildPath" -ForegroundColor Green

# Check WDK
$wdkFound = $false
$vcPaths = Get-ChildItem (Join-Path $vsPath "MSBuild\Microsoft\VC\v*") -ErrorAction SilentlyContinue
foreach ($p in $vcPaths) {
    $wdkProps = Join-Path $p.FullName "WDKConversion\PreConfiguration.props"
    if (Test-Path $wdkProps) {
        $wdkFound = $true
        break
    }
}

if (-not $wdkFound) {
    Write-Host "[警告] 偵測到此環境未安裝 Windows Driver Kit (WDK)。" -ForegroundColor Yellow
    Write-Host "[警告] 編譯 C++ 驅動核心需要 WDK。如果您尚未編譯過驅動程式，編譯整個專案 (選項 2) 將會失敗。" -ForegroundColor Yellow
    Write-Host "提示：若您僅修改 GUI 介面，選擇模式 1 即可；driver payload 會獨立整理到輸出目錄。" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "[資訊] 編譯環境檢查完成。" -ForegroundColor Green
Write-Host ""

# 2. Prompt user
Write-Host "[2/4] 請選擇編譯模式：" -ForegroundColor Yellow
Write-Host "1. 僅編譯 C# GUI 介面 (ImDiskGui.csproj)"
Write-Host "2. 編譯整個專案 (包括 C++ 驅動核心與 C# GUI，需要安裝 WDK 與 Windows SDK 8.1)"
$buildMode = Read-Host "請輸入選項 (1 或 2，預設為 1)"
if ([string]::IsNullOrEmpty($buildMode)) { $buildMode = "1" }

if ($buildMode -eq "2") {
    Write-Host ""
    Write-Host "[3/4] 正在編譯 C++ 驅動核心 (Release|x64)..." -ForegroundColor Yellow
    & $msbuildPath ImDisk.sln /p:Configuration=Release /p:Platform=x64 /p:WindowsTargetPlatformVersion=10.0 /t:Rebuild
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[警告] C++ 驅動核心編譯結束 (部分組件可能未成功編譯，例如未安裝 WDK)。" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "[資訊] 正在自動收集與整理驅動核心二進位檔..." -ForegroundColor Green

$dirs = @("sys\amd64", "svc\amd64", "cpl\amd64", "cli\amd64")
foreach ($d in $dirs) {
    if (-not (Test-Path $d)) { New-Item -ItemType Directory -Path $d | Out-Null }
}

# 2.5. Check for official zip package (imdiskinst.zip or imdisk.zip) to extract and verify
$zipPath = "imdiskinst.zip"
if (-not (Test-Path $zipPath)) {
    $zipPath = "imdisk.zip"
}
if (Test-Path $zipPath) {
    Write-Host ""
    Write-Host "[驗證] 偵測到根目錄有 $zipPath，正在進行 SHA-256 計算與數位簽章驗證..." -ForegroundColor Green
    $hash = (Get-FileHash $zipPath -Algorithm SHA256).Hash
    Write-Host "ZIP 檔案 SHA-256 哈希值: $hash" -ForegroundColor Cyan

    $tempExtractDir = Join-Path $env:TEMP "imdisk_temp_extract"
    if (Test-Path $tempExtractDir) { Remove-Item $tempExtractDir -Recurse -Force -ErrorAction SilentlyContinue }
    New-Item -ItemType Directory -Path $tempExtractDir | Out-Null

    try {
        Write-Host "正在解壓縮 $zipPath..." -ForegroundColor Gray
        Expand-Archive -Path $zipPath -DestinationPath $tempExtractDir -Force

        # Verify signature of extracted driver
        $sysExtractPath = Join-Path $tempExtractDir "sys\amd64\imdisk.sys"
        if (Test-Path $sysExtractPath) {
            Write-Host "正在驗證驅動程式的數位簽章..." -ForegroundColor Gray
            $sig = Get-AuthenticodeSignature $sysExtractPath

            if ($sig.Status -eq "Valid" -and ($sig.SignerCertificate.Subject -like "*Olof Lagerkvist*" -or $sig.SignerCertificate.Subject -like "*LTR Data*" -or $sig.SignerCertificate.Subject -like "*Microsoft Windows Hardware Compatibility Publisher*" -or $sig.SignerCertificate.Subject -like "*Microsoft Corporation*")) {
                Write-Host "[成功] 驅動程式數位簽章驗證通過！證實為 Olof Lagerkvist / LTR Data 官方簽署版本。" -ForegroundColor Green
                Write-Host "簽署者主體: $($sig.SignerCertificate.Subject)" -ForegroundColor Gray

                # Copy from zip to the proper locations
                $zipFiles = @(
                    "sys\amd64\imdisk.sys",
                    "svc\amd64\imdsksvc.exe",
                    "cpl\amd64\imdisk.cpl",
                    "cli\amd64\imdisk.exe",
                    "sys\i386\imdisk.sys",
                    "svc\i386\imdsksvc.exe",
                    "cpl\i386\imdisk.cpl",
                    "cli\i386\imdisk.exe",
                    "uninstall_imdisk.cmd"
                )
                foreach ($zf in $zipFiles) {
                    $src = Join-Path $tempExtractDir $zf
                    $dst = $zf
                    if (Test-Path $src) {
                        $parentDst = Split-Path -Parent $dst
                        if (![string]::IsNullOrEmpty($parentDst) -and -not (Test-Path $parentDst)) {
                            New-Item -ItemType Directory -Path $parentDst | Out-Null
                        }
                        Copy-Item $src $dst -Force
                        Write-Host "已從 ZIP 複製並覆寫: $zf" -ForegroundColor Gray
                    }
                }
            } else {
                Write-Host "[警告] 數位簽章驗證不通過或不是由 Olof Lagerkvist 簽署！" -ForegroundColor Yellow
                if ($sig.Status) {
                    Write-Host "簽章狀態: $($sig.Status)" -ForegroundColor Yellow
                    Write-Host "簽署者主體: $($sig.SignerCertificate.Subject)" -ForegroundColor Yellow
                } else {
                    Write-Host "該檔案無數位簽章。" -ForegroundColor Yellow
                }
                Write-Host "基於安全性考量，不自動複製檔案。您可以手動處理。" -ForegroundColor Yellow
            }
        } else {
            Write-Host "[錯誤] ZIP 壓縮包內找不到 sys\amd64\imdisk.sys，無法驗證簽章與進行自動複製。" -ForegroundColor Red
        }
    } catch {
        Write-Host "[錯誤] 處理 ZIP 檔案時發生異常: $_" -ForegroundColor Red
    } finally {
        if (Test-Path $tempExtractDir) { Remove-Item $tempExtractDir -Recurse -Force -ErrorAction SilentlyContinue }
    }
    Write-Host ""
}

$copiedCount = 0

function Copy-BinaryHelper($possiblePaths, $destPath) {
    if (Test-Path $destPath) {
        Write-Host "[資訊] $destPath 已存在 (可能來自 ZIP 或舊有編譯結果)，直接使用。" -ForegroundColor Gray
        return $true
    }
    foreach ($p in $possiblePaths) {
        if (Test-Path $p) {
            Copy-Item $p $destPath -Force
            return $true
        }
    }
    return $false
}

# sys
$sysPaths = @(
    "sys\Win7Release\x64\imdisk.sys",
    "sys\Win8Release\x64\imdisk.sys",
    "sys\Win8.1Release\x64\imdisk.sys",
    "sys\Release\x64\imdisk.sys",
    "sys\x64\Release\imdisk.sys"
)
if (Copy-BinaryHelper $sysPaths "sys\amd64\imdisk.sys") { $copiedCount++ }

# svc
$svcPaths = @(
    "svc\x64\imdsksvc.exe",
    "svc\Release\x64\imdsksvc.exe",
    "svc\x64\Release\imdsksvc.exe"
)
if (Copy-BinaryHelper $svcPaths "svc\amd64\imdsksvc.exe") { $copiedCount++ }

# cpl
$cplPaths = @(
    "cpl\x64\imdisk.cpl",
    "cplcore\x64\imdisk.cpl",
    "cpl\Release\x64\imdisk.cpl",
    "cpl\x64\Release\imdisk.cpl"
)
if (Copy-BinaryHelper $cplPaths "cpl\amd64\imdisk.cpl") { $copiedCount++ }

# cli
$cliPaths = @(
    "cli\x64\imdisk.exe",
    "cli\Release\x64\imdisk.exe",
    "cli\x64\Release\imdisk.exe"
)
if (Copy-BinaryHelper $cliPaths "cli\amd64\imdisk.exe") { $copiedCount++ }

Write-Host "[資訊] 已自動收集與複製 $copiedCount 個編譯好的驅動二進位檔！" -ForegroundColor Green

Write-Host ""
Write-Host "[3/4] 正在編譯 C# GUI 介面 (Release|x64)..." -ForegroundColor Yellow

# Check if ImDiskGui.exe is currently running (file lock)
$guiProc = Get-Process -Name "ImDiskGui" -ErrorAction SilentlyContinue
if ($guiProc) {
    Write-Host "[警告] 偵測到 ImDiskGui.exe 正在執行中，無法覆寫。" -ForegroundColor Yellow
    Write-Host "請先關閉 ImDisk RAM Disk Manager 再重新編譯。" -ForegroundColor Yellow
    Exit 1
}

# Restore NuGet packages first
& $msbuildPath ImDiskGui\ImDiskGui.csproj /t:Restore /p:Configuration=Release /p:Platform=x64 /v:minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "[警告] NuGet 套件還原可能未完全成功，嘗試繼續編譯..." -ForegroundColor Yellow
}

# Build (not Rebuild to avoid unnecessary clean)
& $msbuildPath ImDiskGui\ImDiskGui.csproj /p:Configuration=Release /p:Platform=x64 /t:Build
if ($LASTEXITCODE -ne 0) {
    Write-Host "[錯誤] GUI 介面編譯失敗。" -ForegroundColor Red
    Exit 1
}

Write-Host ""
Write-Host "[4/4] 正在整理最終輸出套件..." -ForegroundColor Yellow
$outDir = "ImDiskGui\bin\x64\Release\net48"
$exePath = Join-Path $outDir "ImDiskGui.exe"

# Fallback: check AnyCPU output path
if (-not (Test-Path $exePath)) {
    $outDir = "ImDiskGui\bin\Release\net48"
    $exePath = Join-Path $outDir "ImDiskGui.exe"
}

$driverOutDir = Join-Path $outDir "driver"
if (-not (Test-Path $driverOutDir)) {
    New-Item -ItemType Directory -Path $driverOutDir | Out-Null
}

$driverCopyMap = @(
    @{ Src = "imdisk.inf"; Dst = "imdisk.inf" },
    @{ Src = "sys\amd64\imdisk.sys"; Dst = "sys\amd64\imdisk.sys" },
    @{ Src = "svc\amd64\imdsksvc.exe"; Dst = "svc\amd64\imdsksvc.exe" },
    @{ Src = "cpl\amd64\imdisk.cpl"; Dst = "cpl\amd64\imdisk.cpl" },
    @{ Src = "cli\amd64\imdisk.exe"; Dst = "cli\amd64\imdisk.exe" },
    @{ Src = "sys\i386\imdisk.sys"; Dst = "sys\i386\imdisk.sys" },
    @{ Src = "svc\i386\imdsksvc.exe"; Dst = "svc\i386\imdsksvc.exe" },
    @{ Src = "cpl\i386\imdisk.cpl"; Dst = "cpl\i386\imdisk.cpl" },
    @{ Src = "cli\i386\imdisk.exe"; Dst = "cli\i386\imdisk.exe" }
)
foreach ($item in $driverCopyMap) {
    $src = $item.Src
    $dst = Join-Path $driverOutDir $item.Dst
    $dstDir = Split-Path -Parent $dst
    if (-not (Test-Path $dstDir)) {
        New-Item -ItemType Directory -Path $dstDir | Out-Null
    }
    if (Test-Path $src) {
        Copy-Item $src $dst -Force
    }
}

if (Test-Path "uninstall_imdisk.cmd") {
    $rootCmd = "uninstall_imdisk.cmd"
    $content = Get-Content $rootCmd -Raw
    $targetLine = 'start "" "%SystemRoot%\system32\rundll32.exe" setupapi.dll,InstallHinfSection DefaultUninstall 132 %SystemRoot%\inf\imdisk.inf'
    $patchedLine = 'if exist "%SystemRoot%\inf\imdisk.inf" (' + "`r`n" + '  "%SystemRoot%\system32\rundll32.exe" setupapi.dll,InstallHinfSection DefaultUninstall 132 %SystemRoot%\inf\imdisk.inf' + "`r`n" + ')'
    if ($content.Contains($targetLine)) {
        $content = $content.Replace($targetLine, $patchedLine)
    }
    $targetTop = 'title ImDisk Virtual Disk Driver Uninstall'
    $replacementTop = 'title ImDisk Virtual Disk Driver Uninstall' + "`r`n`r`n" +
'reg query HKLM\SYSTEM\CurrentControlSet\Services\ImDisk >nul 2>&1' + "`r`n" +
'if %errorlevel% neq 0 (' + "`r`n" +
'    endlocal' + "`r`n" +
'    goto :eof' + "`r`n" +
')' + "`r`n`r`n" +
'if not exist "%SystemRoot%\inf\imdisk.inf" (' + "`r`n" +
'    endlocal' + "`r`n" +
'    goto :eof' + "`r`n" +
')' + "`r`n`r`n" +
'fltmc >nul 2>&1' + "`r`n" +
'if %errorlevel% neq 0 (' + "`r`n" +
'    echo.' + "`r`n" +
'    echo 錯誤：此指令檔必須以「系統管理員」身分執行！' + "`r`n" +
'    echo 請在 uninstall_imdisk.cmd 上按滑鼠右鍵，選擇「以系統管理員身分執行」。' + "`r`n" +
'    echo.' + "`r`n" +
'    pause' + "`r`n" +
'    endlocal' + "`r`n" +
'    goto :eof' + "`r`n" +
')'
    if ($content.Contains($targetTop)) {
        $content = $content.Replace($targetTop, $replacementTop)
    }
    $content | Out-File $rootCmd -Encoding default -Force
    Copy-Item "uninstall_imdisk.cmd" (Join-Path $outDir "uninstall_imdisk.cmd") -Force
}
$driverUninstallScript = Join-Path $driverOutDir "uninstall_imdisk.cmd"
if (Test-Path $driverUninstallScript) {
    Remove-Item $driverUninstallScript -Force
}

if (Test-Path $exePath) {
    Write-Host "===================================================" -ForegroundColor Green
    Write-Host "   [成功] 編譯完成！" -ForegroundColor Green
    Write-Host "===================================================" -ForegroundColor Green
    Write-Host "輸出執行檔位置:"
    Write-Host "  $((Get-Item $exePath).FullName)" -ForegroundColor White
    Write-Host "driver payload 位置:"
    Write-Host "  $((Get-Item $driverOutDir).FullName)" -ForegroundColor White
    $uninstallScript = Join-Path $outDir 'uninstall_imdisk.cmd'
    if (Test-Path $uninstallScript) {
        Write-Host "卸載腳本位置:"
        Write-Host "  $((Get-Item $uninstallScript).FullName)" -ForegroundColor White
    } else {
        Write-Host "卸載腳本位置:"
        Write-Host "  [未就緒] 尚未置入 uninstall_imdisk.cmd (可下載官方 imdisk.zip 置於根目錄重新執行，以自動擷取)" -ForegroundColor Yellow
    }
    Write-Host ""
    Write-Host "說明：" -ForegroundColor Green
    Write-Host "  此執行檔不內嵌 driver；driver payload 已獨立整理到 driver 目錄。"
    Write-Host "  分發時請提供 `ImDiskGui.exe`、`uninstall_imdisk.cmd` 與 `driver` 目錄。"
    Write-Host "===================================================" -ForegroundColor Green
} else {
    Write-Host "[錯誤] 找不到編譯好的 ImDiskGui.exe。" -ForegroundColor Red
    Exit 1
}
