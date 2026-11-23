@echo off
:: ============================================================
::   BU 7MAID ZERO v17.17  -  One-Click Max FPS / Low Delay
::   Progress bar + auto GPU detect + auto restart + full RESTORE
:: ============================================================
setlocal EnableExtensions EnableDelayedExpansion
title BU 7MAID ZERO v17.17
color 0A
set "HIDDEN="
if /i "%~2"=="HIDDEN" set "HIDDEN=1"
set "ERRS=0"
set "ERRLIST="
set "G="
set "R="
if not defined HIDDEN for /f %%a in ('echo prompt $E^| cmd') do set "ESC=%%a"
if defined ESC set "G=!ESC![92m"
if defined ESC set "R=!ESC![91m"
if defined G <nul set /p "=!G!"
if not defined HIDDEN mode con: cols=96 lines=42

net session >nul 2>&1
if %errorlevel% neq 0 (
    powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

set "SELF=%~f0"
set "BK=%SystemDrive%\Bu7maid_Backup"
if /i "%~1"=="COLOR" (set "COLORMODE=REAPPLY" & call :PSRUN COLOR & exit /b)
if not exist "%BK%" mkdir "%BK%" >nul 2>&1
if /i "%~1"=="CLEANBG" (call :CLEAN & echo 1>"%BK%\clean.ok" & exit /b)
set "LAPTOP=0"
for /f %%B in ('powershell -NoProfile -Command "@(Get-CimInstance Win32_Battery -EA SilentlyContinue).Count"') do if not "%%B"=="0" set "LAPTOP=1"
echo   Reading your PC specs...
set "CPUNAME=CPU" & set "GPUNAME=GPU" & set "RAMGB=?" & set "DISK=SSD" & set "KILLER=0"
for /f "tokens=1-5 delims=|" %%a in ('powershell -NoProfile -Command "$c=(Get-CimInstance Win32_Processor|Select -First 1).Name.Trim(); $g=(Get-CimInstance Win32_VideoController|?{$_.Name -notmatch 'Basic|Virtual|Parsec|Meta'}|Sort-Object {$_.Name -notmatch 'NVIDIA|Radeon'}|Select -First 1).Name; $r=[math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory/1GB); $d='SSD'; try{$n=(Get-Partition -DriveLetter $env:SystemDrive[0] -EA Stop).DiskNumber; if((Get-PhysicalDisk|?{$_.DeviceId -eq [string]$n}).MediaType -eq 'HDD'){$d='HDD'}}catch{}; $k=[int]([bool](Get-NetAdapter -EA SilentlyContinue|?{$_.InterfaceDescription -match 'Killer'})); if(-not $g){$g='Unknown GPU'}; @($c,$g,$r,$d,$k) -join '|'"') do (
    set "CPUNAME=%%a" & set "GPUNAME=%%b" & set "RAMGB=%%c" & set "DISK=%%d" & set "KILLER=%%e"
)
set "CPUVEN=OTHER" & set "ISX3D=0"
echo !CPUNAME! | findstr /i /c:"AMD" >nul && set "CPUVEN=AMD"
echo !CPUNAME! | findstr /i /c:"Intel" >nul && set "CPUVEN=INTEL"
echo !CPUNAME! | findstr /i "X3D" >nul && set "ISX3D=1"

:: ---- called by the EXE with a command: run it, then close ----
set "CLIMODE="
if not "%~1"=="" set "CLIMODE=1"
if /i "%~1"=="BOOST"   (set "SEC=OFF" & goto AUTO)
if /i "%~1"=="SAFE"    (set "SEC=ON" & goto AUTO)
if /i "%~1"=="GPUTEST" goto TOOLS
if /i "%~1"=="DELAY"   goto DELAYTEST
if /i "%~1"=="DPCTOOL" goto DELAYTEST
if /i "%~1"=="BIOS"    goto BIOS
if /i "%~1"=="STATUS"  goto STATUS
if /i "%~1"=="CLEAN"   goto CLEANONLY
if /i "%~1"=="FIXWIN"  goto FIXWIN
if /i "%~1"=="RAM"     goto RAMCLEAN
if /i "%~1"=="SCAN"    goto SCAN
if /i "%~1"=="PANEL"   goto GPUGUIDE
if /i "%~1"=="RESTORE" goto RESTORE
if /i "%~1"=="NAVFIX"  goto NAVFIX
if /i "%~1"=="VBSFIX"  goto VBSFIX
if /i "%~1"=="GAMES"   goto GAMESONLY

:MENU
if defined CLIMODE exit /b
cls
for /f %%P in ('powershell -NoProfile -Command "(Get-Process).Count"') do set "PROC=%%P"
echo.
echo   +===================================================================+
echo   ^|                  B U   7 M A I D    Z E R O    v17.17               ^|
echo   ^|            more FPS  -  less delay  -  fewer processes            ^|
echo   +===================================================================+
echo     CPU  !CPUNAME!
echo     GPU  !GPUNAME!
echo     RAM  !RAMGB! GB   ^|  Disk  !DISK!   ^|  Processes  !PROC!
if "%LAPTOP%"=="1" echo     LAPTOP mode: battery stays safe, full power when plugged in
echo   +-------------------------------------------------------------------+
echo.
echo      [1]  BOOST          everything, max performance  (your PC)
echo      [2]  BOOST SAFE     same, but antivirus stays ON (friends)
echo.
echo      [3]  TEST           GPU test  /  delay test
echo      [4]  BIOS           what to change in YOUR BIOS + where
echo      [5]  STATUS         what is ON / OFF right now
echo      [6]  TOOLS          clean  /  RAM  /  virus scan  /  GPU panel
echo.
echo      [9]  RESTORE        undo everything
echo      [0]  EXIT
echo.
choice /c 12345690 /n /m "     Press a number: "
if errorlevel 8 exit /b
if errorlevel 7 goto RESTORE
if errorlevel 6 goto TOOLMENU
if errorlevel 5 goto STATUS
if errorlevel 4 goto BIOS
if errorlevel 3 goto TESTMENU
if errorlevel 2 (set "SEC=ON" & goto AUTO)
if errorlevel 1 (set "SEC=OFF" & goto AUTO)
goto MENU

:TESTMENU
cls
echo.
echo   +----------------------------- TEST --------------------------------+
echo.
echo      [1]  GPU TEST     FPS + max temperature + score   (2-3 min)
echo      [2]  DPC TOOL     download + open DPC Latency Checker (saved on Desktop)
echo      [3]  PING TEST    ping + jitter to the internet
echo      [0]  Back
echo.
choice /c 1230 /n /m "     Press a number: "
if errorlevel 4 goto MENU
if errorlevel 3 goto PINGTEST
if errorlevel 2 goto DELAYTEST
if errorlevel 1 goto TOOLS
goto MENU

:TOOLMENU
cls
echo.
echo   +----------------------------- TOOLS -------------------------------+
echo.
echo      [1]  CLEAN        temp + RAM + free disk space
echo      [2]  RAM CLEAN    free RAM now (use anytime)
echo      [3]  VIRUS SCAN   quick scan + remove (built-in)
echo      [4]  GPU PANEL    NVIDIA / AMD settings guide
echo      [0]  Back
echo.
choice /c 12340 /n /m "     Press a number: "
if errorlevel 5 goto MENU
if errorlevel 4 goto GPUGUIDE
if errorlevel 3 goto SCAN
if errorlevel 2 goto RAMCLEAN
if errorlevel 1 goto CLEANONLY
goto MENU

:: ============================================================
:AUTO
cls
echo.
echo   Processes before: %PROC%
echo %PROC%> "%BK%\proc_before.txt"
echo.
set /a STEP=0
set "TOTAL=25"

call :STEP "Backup + System Restore point"
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore" /v SystemRestorePointCreationFrequency /t REG_DWORD /d 0 /f >nul 2>&1
if not exist "%BK%\rp.done" powershell -NoProfile -Command "Enable-ComputerRestore -Drive '%SystemDrive%\' -EA SilentlyContinue; Checkpoint-Computer -Description 'Bu7maid Zero Before' -RestorePointType MODIFY_SETTINGS -EA SilentlyContinue" >nul 2>&1
echo 1>"%BK%\rp.done" 2>nul
if not exist "%BK%\services_original.csv" powershell -NoProfile -Command "Get-CimInstance Win32_Service | Select-Object Name,StartMode | Export-Csv -NoTypeInformation -Path '%BK%\services_original.csv'" >nul 2>&1

call :STEP "Health check: fixing Windows problems, skipping risky tweaks if unstable"
set "SAFE=0"
del /f /q "%BK%\health.txt" >nul 2>&1
call :PSRUN HEALTH
if exist "%BK%\health.txt" (
    for /f "usebackq delims=" %%L in ("%BK%\health.txt") do (
        if /i "%%L"=="UNSTABLE" (set "SAFE=1") else if defined HIDDEN echo @@DO=%%L
    )
)
if "!SAFE!"=="1" set "SEC=ON"
set "MSIMODE=USBFIX"
call :PSRUN MSI
reg delete "HKLM\SYSTEM\CurrentControlSet\Services\kbdclass\Parameters" /v KeyboardDataQueueSize /f >nul 2>&1
reg delete "HKLM\SYSTEM\CurrentControlSet\Services\mouclass\Parameters" /v MouseDataQueueSize /f >nul 2>&1

call :STEP "Ultimate power plan, no core parking, boost aggressive"
powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 99999999-9999-9999-9999-999999999999 >nul 2>&1
powercfg -changename 99999999-9999-9999-9999-999999999999 "Bu 7maid Zero" "Max FPS" >nul 2>&1
powercfg -setactive 99999999-9999-9999-9999-999999999999 >nul 2>&1
if errorlevel 1 call :BAD "power plan could not be activated"
if "!ISX3D!"=="1" (
    powercfg -setacvalueindex scheme_current sub_processor CPMINCORES 50 >nul 2>&1
) else (
    powercfg -setacvalueindex scheme_current sub_processor CPMINCORES 100 >nul 2>&1
    powercfg -setacvalueindex scheme_current sub_processor CPMINCORES1 100 >nul 2>&1
)
powercfg -setacvalueindex scheme_current sub_processor PROCTHROTTLEMIN 100 >nul 2>&1
powercfg -attributes sub_processor PERFBOOSTMODE -ATTRIB_HIDE >nul 2>&1
powercfg -setacvalueindex scheme_current sub_processor PERFBOOSTMODE 2 >nul 2>&1
powercfg -setacvalueindex scheme_current sub_processor PERFEPP 0 >nul 2>&1
powercfg -setacvalueindex scheme_current 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0 >nul 2>&1
powercfg -setacvalueindex scheme_current sub_disk DISKIDLE 0 >nul 2>&1
if "%LAPTOP%"=="1" (
    powercfg -setdcvalueindex scheme_current sub_processor PROCTHROTTLEMIN 5 >nul 2>&1
    powercfg -setdcvalueindex scheme_current sub_processor PERFBOOSTMODE 1 >nul 2>&1
    powercfg -setdcvalueindex scheme_current sub_processor PERFEPP 50 >nul 2>&1
)
powercfg -setactive scheme_current >nul 2>&1
powercfg -h off >nul 2>&1
if errorlevel 1 call :BAD "hibernate could not be turned off"
reg add "HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling" /v PowerThrottlingOff /t REG_DWORD /d 1 /f >nul 2>&1

call :STEP "Merging svchost (big process drop)"
powershell -NoProfile -Command "$kb=[int]((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory/1KB)+1024000; Set-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control' -Name SvcHostSplitThresholdInKB -Value $kb -Type DWord" >nul 2>&1

call :STEP "Disabling useless services"
for %%S in (DiagTrack dmwappushservice SysMain WSearch MapsBroker RetailDemo Fax WerSvc lfsvc RemoteRegistry TrkWks WMPNetworkSvc PcaSvc DPS WdiServiceHost WdiSystemHost diagsvc wercplsupport SEMgrSvc PhoneSvc WpcMonSvc wisvc icssvc SharedAccess AJRouter ALG CscService) do (
    start "" /b sc stop %%S >nul 2>&1
    sc config %%S start= disabled >nul 2>&1
)
for %%S in (XblAuthManager XblGameSave XboxNetApiSvc XboxGipSvc BITS wuauserv DoSvc TabletInputService PrintNotify edgeupdate edgeupdatem MicrosoftEdgeElevationService gupdate gupdatem TokenBroker CDPSvc InstallService StorSvc UsoSvc WaaSMedicSvc SCardSvr ScDeviceEnum stisvc WbioSrvc WalletService SensorService SensrSvc CloudBackupRestoreSvc cloudidsvc DsSvc PushToInstall WpnService AssignedAccessManagerSvc autotimesvc DisplayEnhancementService) do sc config %%S start= demand >nul 2>&1
if "!KILLER!"=="0" reg add "HKLM\SYSTEM\CurrentControlSet\Services\Ndu" /v Start /t REG_DWORD /d 4 /f >nul 2>&1
:: hardware-aware: turn off services for hardware this PC does not have
call :PSRUN HWSVC
:: game launchers / updaters: no auto-start at boot (open them yourself when you play)
call :PSRUN STARTOFF
call :PSRUN STARTALL
:: per-user bloat (clipboard history, sync, contacts, messaging) - cuts running process count
powershell -NoProfile -Command "foreach($s in 'OneSyncSvc','PimIndexMaintenanceSvc','UnistoreSvc','UserDataSvc','MessagingService','BcastDVRUserService','CDPUserSvc','diagnosticshub.standardcollector.service','xbgm','WpnService','OneSyncSvc','AarSvc','DevicePickerUserSvc','PrintWorkflowUserSvc','WalletService','MixedRealityOpenXRSvc','AppVClient','RetailDemo','Fax','SharedRealitySvc','MapsBroker','lfsvc','WpcMonSvc'){ $p='HKLM:\SYSTEM\CurrentControlSet\Services\'+$s; if(Test-Path $p){ Set-ItemProperty -Path $p -Name Start -Value 4 -EA SilentlyContinue } }" >nul 2>&1
rem copy + paste + Win+V must keep working: make sure the clipboard services are on (repairs PCs where an older version turned them off)
powershell -NoProfile -Command "foreach($s in 'cbdhsvc','TabletInputService','TextInputManagementService'){ $p='HKLM:\SYSTEM\CurrentControlSet\Services\'+$s; if(Test-Path $p){ Set-ItemProperty -Path $p -Name Start -Value 3 -EA SilentlyContinue } }; Get-ChildItem 'HKLM:\SYSTEM\CurrentControlSet\Services' -EA SilentlyContinue | Where-Object { $_.PSChildName -like 'cbdhsvc_*' } | ForEach-Object { Set-ItemProperty -Path $_.PSPath -Name Start -Value 3 -EA SilentlyContinue }; foreach($n in 'AllowClipboardHistory','AllowCrossDeviceClipboard'){ Remove-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\System' -Name $n -EA SilentlyContinue }" >nul 2>&1

rem sign-in safety: Windows Hello PIN / password services must stay available
call :PINFIX
sc config KeyIso start= demand >nul 2>&1
sc config VaultSvc start= demand >nul 2>&1
start "" /min cmd /c "sc start NgcSvc >nul 2>&1"
start "" /min cmd /c "sc start NgcCtnrSvc >nul 2>&1"

call :STEP "Telemetry, Copilot, Widgets, Cortana, background apps OFF"
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection" /v AllowTelemetry /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection" /v AllowTelemetry /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo" /v DisabledByGroupPolicy /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search" /v AllowCortana /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Software\Policies\Microsoft\Windows\WindowsCopilot" /v TurnOffWindowsCopilot /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot" /v TurnOffWindowsCopilot /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Dsh" /v AllowNewsAndInterests /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications" /v GlobalUserDisabled /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Edge" /v StartupBoostEnabled /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Edge" /v BackgroundModeEnabled /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Edge" /v HubsSidebarEnabled /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Edge" /v CopilotCDPPageContext /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Edge" /v ShowRecommendationsEnabled /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v ShowCopilotButton /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v SystemPaneSuggestionsEnabled /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v SilentInstalledAppsEnabled /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent" /v DisableWindowsConsumerFeatures /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization" /v DODownloadMode /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors" /v DisableLocation /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\MicrosoftEdge\Main" /v AllowPrelaunch /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\MicrosoftEdge\TabPreloader" /v AllowTabPreloading /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\Windows Error Reporting" /v Disabled /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\StorageSense" /v AllowStorageSenseGlobal /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search" /v AllowCloudSearch /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v TaskbarMn /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v TaskbarDa /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Search" /v BingSearchEnabled /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Search" /v CortanaConsent /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\Explorer" /v DisableSearchBoxSuggestions /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI" /v DisableAIDataAnalysis /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI" /v AllowRecallEnablement /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDeveloperSettings" /v TaskbarEndTask /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKCU\Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\Bags\AllFolders\Shell" /v FolderType /t REG_SZ /d NotSpecified /f >nul 2>&1
for %%L in ("Microsoft-Windows-SleepStudy/Diagnostic" "Microsoft-Windows-Kernel-Processor-Power/Diagnostic" "Microsoft-Windows-UserModePowerService/Diagnostic") do wevtutil sl %%L /e:false >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\AutoLogger-Diagtrack-Listener" /v Start /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\SQMLogger" /v Start /t REG_DWORD /d 0 /f >nul 2>&1
call :TASKS /Disable

call :STEP "Removing bloat apps (Xbox / Store / Photos / Calc kept)"
powershell -NoProfile -Command "$l='Clipchamp.Clipchamp','Microsoft.BingNews','Microsoft.BingWeather','Microsoft.BingSearch','Microsoft.GetHelp','Microsoft.Getstarted','Microsoft.MicrosoftSolitaireCollection','Microsoft.People','Microsoft.Todos','Microsoft.WindowsFeedbackHub','Microsoft.WindowsMaps','Microsoft.MicrosoftOfficeHub','Microsoft.SkypeApp','Microsoft.ZuneVideo','Microsoft.YourPhone','Microsoft.PowerAutomateDesktop','MicrosoftCorporationII.QuickAssist','Microsoft.549981C3F5F10','MicrosoftTeams','MSTeams','Microsoft.Windows.DevHome','Microsoft.OutlookForWindows','Microsoft.Copilot','Microsoft.WindowsAlarms','Microsoft.MixedReality.Portal','Microsoft.3DBuilder','Microsoft.Microsoft3DViewer','MicrosoftWindows.Client.WebExperience','Microsoft.Windows.Ai.Copilot.Provider','Microsoft.BingTranslator','Microsoft.News','Microsoft.WindowsSoundRecorder','microsoft.windowscommunicationsapps','Microsoft.Office.OneNote','Microsoft.MSPaint','Microsoft.Wallet','Microsoft.Messaging','Microsoft.OneConnect','Microsoft.Print3D','Microsoft.MicrosoftJournal','Microsoft.Windows.Teams','Microsoft.Advertising.Xaml','Microsoft.MinecraftEducationEdition','Microsoft.WindowsReadingList','Microsoft.BingFinance','Microsoft.BingSports','Microsoft.BingFoodAndDrink','Microsoft.BingHealthAndFitness','Microsoft.BingTravel','Microsoft.Copilot','Microsoft.Windows.Ai.Copilot.Provider','Microsoft.MicrosoftOfficeHub','MicrosoftWindows.Client.CoPilot'; foreach($a in $l){Get-AppxPackage -AllUsers -Name $a -EA SilentlyContinue | Remove-AppxPackage -AllUsers -EA SilentlyContinue; Get-AppxProvisionedPackage -Online -EA SilentlyContinue | ?{$_.DisplayName -eq $a} | Remove-AppxProvisionedPackage -Online -EA SilentlyContinue}" >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot" /v TurnOffWindowsCopilot /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKCU\Software\Policies\Microsoft\Windows\WindowsCopilot" /v TurnOffWindowsCopilot /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI" /v DisableAIDataAnalysis /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v ShowCopilotButton /t REG_DWORD /d 0 /f >nul 2>&1
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\People" /v PeopleBand /f >nul 2>&1
if not exist "%BK%\copilot.done" dism /Online /Remove-Capability /CapabilityName:Microsoft.Windows.Copilot~~~~ /NoRestart >nul 2>&1
if not exist "%BK%\copilot.done" powershell -NoProfile -Command "Get-AppxPackage -AllUsers *Copilot* -EA SilentlyContinue | Remove-AppxPackage -AllUsers -EA SilentlyContinue; Get-AppxProvisionedPackage -Online -EA SilentlyContinue | ?{$_.DisplayName -match 'Copilot'} | Remove-AppxProvisionedPackage -Online -EA SilentlyContinue; Get-AppxPackage -AllUsers *WebExperience* -EA SilentlyContinue | Remove-AppxPackage -AllUsers -EA SilentlyContinue" >nul 2>&1
echo 1>"%BK%\copilot.done" 2>nul

call :STEP "Game Mode ON, Game DVR + Presence Writer OFF, HAGS ON, FSO fix"
reg add "HKCU\Software\Microsoft\GameBar" /v AutoGameModeEnabled /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\GameBar" /v AllowAutoGameMode /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\GameBar" /v UseNexusForGameBarEnabled /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\GameBar" /v ShowStartupPanel /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\GameDVR" /v AppCaptureEnabled /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\System\GameConfigStore" /v GameDVR_Enabled /t REG_DWORD /d 0 /f >nul 2>&1
reg delete "HKCU\System\GameConfigStore" /v GameDVR_FSEBehaviorMode /f >nul 2>&1
reg delete "HKCU\System\GameConfigStore" /v GameDVR_HonorUserFSEBehaviorMode /f >nul 2>&1
reg delete "HKCU\System\GameConfigStore" /v GameDVR_DXGIHonorFSEWindowsCompatible /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\GameDVR" /v AllowGameDVR /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v HwSchMode /t REG_DWORD /d 2 /f >nul 2>&1
if errorlevel 1 call :BAD "GPU scheduling (HAGS) could not be set"
reg add "HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers" /v TdrDelay /t REG_DWORD /d 10 /f >nul 2>&1
reg delete "HKCU\System\GameConfigStore" /v GameDVR_FSEBehavior /f >nul 2>&1
reg add "HKCU\System\GameConfigStore" /v GameDVR_EFSEFeatureFlags /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\DirectX\UserGpuPreferences" /v DirectXUserGlobalSettings /t REG_SZ /d "SwapEffectUpgradeEnable=1;VRROptimizeEnable=0;" /f >nul 2>&1
set "GBV=0"
call :PSRUN GBPW
set "MM=HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"
reg add "%MM%" /v SystemResponsiveness /t REG_DWORD /d 0 /f >nul 2>&1
reg add "%MM%" /v NetworkThrottlingIndex /t REG_DWORD /d 4294967295 /f >nul 2>&1
reg add "%MM%\Tasks\Games" /v "GPU Priority" /t REG_DWORD /d 8 /f >nul 2>&1
reg add "%MM%\Tasks\Games" /v Priority /t REG_DWORD /d 6 /f >nul 2>&1
reg add "%MM%\Tasks\Games" /v "Scheduling Category" /t REG_SZ /d High /f >nul 2>&1
reg add "%MM%\Tasks\Games" /v "SFIO Priority" /t REG_SZ /d High /f >nul 2>&1
for %%G in (FortniteClient-Win64-Shipping.exe VALORANT-Win64-Shipping.exe cs2.exe r5apex.exe r5apex_dx12.exe cod.exe RainbowSix.exe RainbowSix_Vulkan.exe TslGame.exe GTA5.exe GTA5_Enhanced.exe RustClient.exe Overwatch.exe MarvelRivals-Win64-Shipping.exe "League of Legends.exe" javaw.exe RobloxPlayerBeta.exe FiveM_GTAProcess.exe) do (
    reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\%%~G\PerfOptions" /v CpuPriorityClass /t REG_DWORD /d 3 /f >nul 2>&1
    reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\%%~G\PerfOptions" /v IoPriority /t REG_DWORD /d 3 /f >nul 2>&1
)

call :STEP "Lower input delay (clean timer, scheduler, mouse, keyboard)"
reg add "HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl" /v Win32PrioritySeparation /t REG_DWORD /d 38 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel" /v GlobalTimerResolutionRequests /t REG_DWORD /d 1 /f >nul 2>&1
rem timer settings: kept at Windows defaults - forcing platform tick / tsc policy made DPC latency worse on this PC
bcdedit /deletevalue useplatformclock >nul 2>&1
bcdedit /deletevalue useplatformtick >nul 2>&1
bcdedit /deletevalue tscsyncpolicy >nul 2>&1
if not "!SAFE!"=="1" bcdedit /set disabledynamictick yes >nul 2>&1
bcdedit /set bootmenupolicy standard >nul 2>&1
bcdedit /timeout 3 >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power" /v HiberbootEnabled /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Control Panel\Mouse" /v MouseSpeed /t REG_SZ /d 0 /f >nul 2>&1
reg add "HKCU\Control Panel\Mouse" /v MouseThreshold1 /t REG_SZ /d 0 /f >nul 2>&1
reg add "HKCU\Control Panel\Mouse" /v MouseThreshold2 /t REG_SZ /d 0 /f >nul 2>&1
reg add "HKCU\Control Panel\Keyboard" /v KeyboardDelay /t REG_SZ /d 0 /f >nul 2>&1
reg add "HKCU\Control Panel\Desktop" /v MenuShowDelay /t REG_SZ /d 0 /f >nul 2>&1
reg add "HKCU\Control Panel\Desktop" /v AutoEndTasks /t REG_SZ /d 1 /f >nul 2>&1
reg add "HKCU\Control Panel\Desktop" /v HungAppTimeout /t REG_SZ /d 1000 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Control" /v WaitToKillServiceTimeout /t REG_SZ /d 2000 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize" /v StartupDelayInMSec /t REG_DWORD /d 0 /f >nul 2>&1
reg delete "HKLM\SYSTEM\CurrentControlSet\Services\mouclass\Parameters" /v MouseDataQueueSize /f >nul 2>&1
reg delete "HKLM\SYSTEM\CurrentControlSet\Services\kbdclass\Parameters" /v KeyboardDataQueueSize /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Services\Ntfs" /v NtfsDisableLastAccessUpdate /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKCU\Control Panel\Accessibility\StickyKeys" /v Flags /t REG_SZ /d 506 /f >nul 2>&1
reg add "HKCU\Control Panel\Accessibility\Keyboard Response" /v Flags /t REG_SZ /d 122 /f >nul 2>&1
reg add "HKCU\Control Panel\Accessibility\ToggleKeys" /v Flags /t REG_SZ /d 58 /f >nul 2>&1

call :STEP "Network: lowest ping, faster downloads, fast DNS"
powershell -NoProfile -Command "Get-ChildItem 'HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces' | %%{ New-ItemProperty -Path $_.PSPath -Name TcpAckFrequency -Value 1 -PropertyType DWord -Force; New-ItemProperty -Path $_.PSPath -Name TCPNoDelay -Value 1 -PropertyType DWord -Force; New-ItemProperty -Path $_.PSPath -Name TcpDelAckTicks -Value 0 -PropertyType DWord -Force }" >nul 2>&1
powershell -NoProfile -Command "Get-NetAdapter -Physical -EA SilentlyContinue | ?{$_.Status -eq 'Up'} | %%{ Set-DnsClientServerAddress -InterfaceIndex $_.ifIndex -ServerAddresses '1.1.1.1','8.8.8.8' -EA SilentlyContinue }" >nul 2>&1
netsh int tcp set global autotuninglevel=normal >nul 2>&1
if errorlevel 1 call :BAD "network tuning (netsh) failed"
netsh int tcp set global rss=enabled >nul 2>&1
netsh int tcp set global ecncapability=disabled >nul 2>&1
netsh int tcp set global timestamps=disabled >nul 2>&1
netsh interface teredo set state type=default >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\Psched" /v NonBestEffortLimit /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters" /v DisabledComponents /t REG_DWORD /d 32 /f >nul 2>&1
powershell -NoProfile -Command "Get-NetAdapter -Physical -EA SilentlyContinue | %%{ Disable-NetAdapterPowerManagement -Name $_.Name -EA SilentlyContinue; foreach($d in 'Energy Efficient Ethernet','Green Ethernet','Power Saving Mode','Advanced EEE','Gigabit Lite','Ultra Low Power Mode','Selective Suspend','Interrupt Moderation','Flow Control','Large Send Offload V2 (IPv4)','Large Send Offload V2 (IPv6)'){ Set-NetAdapterAdvancedProperty -Name $_.Name -DisplayName $d -DisplayValue 'Disabled' -EA SilentlyContinue } }; Get-NetAdapter -Physical -EA SilentlyContinue | ?{$_.Status -eq 'Up'} | %%{ Set-DnsClientServerAddress -InterfaceIndex $_.ifIndex -ServerAddresses ('1.1.1.1','1.0.0.1') -EA SilentlyContinue }" >nul 2>&1
ipconfig /flushdns >nul 2>&1

call :STEP "Light visuals, memory + disk tweaks, USB/NVMe no power saving"
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects" /v VisualFXSetting /t REG_DWORD /d 3 /f >nul 2>&1
reg add "HKCU\Control Panel\Desktop\WindowMetrics" /v MinAnimate /t REG_SZ /d 0 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v TaskbarAnimations /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize" /v EnableTransparency /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Control Panel\Desktop" /v FontSmoothing /t REG_SZ /d 2 /f >nul 2>&1
reg add "HKCU\Control Panel\Desktop" /v FontSmoothingType /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKCU\Control Panel\Desktop" /v FontSmoothingGamma /t REG_DWORD /d 1400 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\VideoSettings" /v EnableAutoEnhanceDuringPlayback /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\VideoSettings" /v AllowLowResolution /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v DisablePagingExecutive /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters" /v EnablePrefetcher /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters" /v EnableSuperfetch /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\FTH" /v Enabled /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Services\stornvme\Parameters\Device" /v IdlePowerMode /t REG_DWORD /d 0 /f >nul 2>&1
fsutil behavior set disablelastaccess 1 >nul 2>&1
fsutil behavior set disabledeletenotify 0 >nul 2>&1
fsutil behavior set disable8dot3 1 >nul 2>&1
fsutil behavior set memoryusage 2 >nul 2>&1
powershell -NoProfile -Command "if((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory -ge 15GB){Disable-MMAgent -MemoryCompression -EA SilentlyContinue}; Get-Volume -EA SilentlyContinue | ?{$_.DriveLetter -and $_.DriveType -eq 'Fixed'} | %%{ Optimize-Volume -DriveLetter $_.DriveLetter -ReTrim -EA SilentlyContinue }" >nul 2>&1
set "MSIMODE=SET"
if not "!SAFE!"=="1" call :PSRUN MSI
reg add "HKLM\SOFTWARE\Microsoft\Windows\Dwm" /v OverlayTestMode /t REG_DWORD /d 5 /f >nul 2>&1
if not exist "%BK%\rsv.done" DISM /Online /Set-ReservedStorageState /State:Disabled /NoRestart >nul 2>&1
echo 1>"%BK%\rsv.done" 2>nul

if "%SEC%"=="ON" (
    call :STEP "Security kept ON (option 2)"
) else (
    call :STEP "VBS / Memory Integrity / Mitigations / Defender OFF"
    reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard" /v EnableVirtualizationBasedSecurity /t REG_DWORD /d 0 /f >nul 2>&1
    reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity" /v Enabled /t REG_DWORD /d 0 /f >nul 2>&1
    reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v FeatureSettingsOverride /t REG_DWORD /d 3 /f >nul 2>&1
    reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v FeatureSettingsOverrideMask /t REG_DWORD /d 3 /f >nul 2>&1
    bcdedit /set hypervisorlaunchtype off >nul 2>&1
    bcdedit /set vsmlaunchtype off >nul 2>&1
    reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\DeviceGuard" /v EnableVirtualizationBasedSecurity /t REG_DWORD /d 0 /f >nul 2>&1
    reg add "HKLM\SYSTEM\CurrentControlSet\Control\Lsa" /v LsaCfgFlags /t REG_DWORD /d 0 /f >nul 2>&1
    reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\CredentialGuard" /v Enabled /t REG_DWORD /d 0 /f >nul 2>&1
    powershell -NoProfile -Command "Set-MpPreference -DisableRealtimeMonitoring $true -DisableBehaviorMonitoring $true -DisableIOAVProtection $true -DisableScriptScanning $true -MAPSReporting 0 -SubmitSamplesConsent 2 -EA SilentlyContinue" >nul 2>&1
    reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows Defender" /v DisableAntiSpyware /t REG_DWORD /d 1 /f >nul 2>&1
    reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection" /v DisableRealtimeMonitoring /t REG_DWORD /d 1 /f >nul 2>&1
    reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection" /v DisableBehaviorMonitoring /t REG_DWORD /d 1 /f >nul 2>&1
    rem SmartScreen / "Windows protected your PC" blue box / "Run anyway" / file security warnings OFF
    reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\System" /v EnableSmartScreen /t REG_DWORD /d 0 /f >nul 2>&1
    reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer" /v SmartScreenEnabled /t REG_SZ /d Off /f >nul 2>&1
    reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\SmartScreen" /v ConfigureAppInstallControlEnabled /t REG_DWORD /d 0 /f >nul 2>&1
    reg add "HKLM\SOFTWARE\Policies\Microsoft\Edge" /v SmartScreenEnabled /t REG_DWORD /d 0 /f >nul 2>&1
    reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\AppHost" /v EnableWebContentEvaluation /t REG_DWORD /d 0 /f >nul 2>&1
    reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Attachments" /v SaveZoneInformation /t REG_DWORD /d 1 /f >nul 2>&1
    reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Attachments" /v SaveZoneInformation /t REG_DWORD /d 1 /f >nul 2>&1
    reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Associations" /v LowRiskFileTypes /t REG_SZ /d ".exe;.bat;.cmd;.msi;.zip;.lnk;.ps1;.reg;.vbs;.js" /f >nul 2>&1
    reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\UX Configuration" /v Notification_Suppress /t REG_DWORD /d 1 /f >nul 2>&1
    reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Spynet" /v SpynetReporting /t REG_DWORD /d 0 /f >nul 2>&1
    reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Spynet" /v SubmitSamplesConsent /t REG_DWORD /d 2 /f >nul 2>&1
    powershell -NoProfile -Command "Set-MpPreference -PUAProtection 0 -CloudBlockLevel 0 -DisableBlockAtFirstSeen $true -DisableArchiveScanning $true -EnableNetworkProtection Disabled -EA SilentlyContinue" >nul 2>&1
    echo SECOFF> "%BK%\sec_off.flag"
)

call :STEP "Tuning for your CPU: !CPUNAME!"
if defined HIDDEN echo @@DO=Your CPU: !CPUNAME!   RAM: !RAMGB! GB
if "!CPUVEN!"=="AMD" (
    reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power" /v HiberbootEnabled /t REG_DWORD /d 0 /f >nul 2>&1
    sc config AMDRyzenMasterDriverV26 start= demand >nul 2>&1
)
if "!CPUVEN!"=="INTEL" (
    reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power" /v HiberbootEnabled /t REG_DWORD /d 0 /f >nul 2>&1
)
if "!ISX3D!"=="1" echo          X3D chip: kept V-Cache cores safe, no core-parking abuse

call :STEP "Storage + network tuned for your hardware (!DISK!)"
if defined HIDDEN echo @@DO=Your drive type: !DISK!
if "!DISK!"=="HDD" (
    sc config SysMain start= auto >nul 2>&1
    sc start SysMain >nul 2>&1
    reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters" /v EnablePrefetcher /t REG_DWORD /d 3 /f >nul 2>&1
    echo          HDD found: kept SysMain/Prefetch ON - they help hard drives load faster
) else (
    sc stop SysMain >nul 2>&1
    sc config SysMain start= disabled >nul 2>&1
    reg add "HKLM\SYSTEM\CurrentControlSet\Services\SysMain" /v Start /t REG_DWORD /d 4 /f >nul 2>&1
    reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters" /v EnablePrefetcher /t REG_DWORD /d 0 /f >nul 2>&1
    reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters" /v EnableSuperfetch /t REG_DWORD /d 0 /f >nul 2>&1
    echo          SSD/NVMe: SysMain + Prefetch OFF, TRIM ON
)
if "!KILLER!"=="1" echo          Killer network card: kept Ndu ON so Killer software keeps working

call :STEP "Mouse / keyboard / controller: lowest delay + checks your devices"
call :PSRUNBG INPUT

call :STEP "Setting monitor to its highest refresh rate"
call :PSRUNBG HZSET

call :STEP "Detecting your GPU and applying card tweaks"
if defined HIDDEN echo @@DO=Your graphics card: !GPUNAME!
call :GPUTWEAK

call :STEP "Extra cuts: more processes off, more FPS (safe)"
if not "!SAFE!"=="1" call :EXTRA

call :STEP "Scanning your games and tuning each one"
call :PSRUN GAMEOPT
call :STEP "Browsers: downloads go straight to your Downloads folder"
set "BRMODE=APPLY"
call :PSRUNBG BROWSERDL
call :STEP "Cleaning temp / cache / RAM"
del /f /q "%BK%\clean.ok" >nul 2>&1
start "" /b cmd /c call "%SELF%" CLEANBG HIDDEN

call :STEP "Freeing disk space (fast)"
if "%SEC%"=="OFF" call :PSRUN ONEDRIVE
call :PSRUN DISK
set "CW=0"
:CLWAIT
if not exist "%BK%\clean.ok" if !CW! lss 40 (set /a CW+=1 & ping -n 2 127.0.0.1 >nul & goto CLWAIT)
call :RAMPS
call :NAVFIXREG

:: final enforcement: SysMain must stay off on SSD/NVMe (nothing after this step can change it)
if not "!DISK!"=="HDD" (
    sc stop SysMain >nul 2>&1
    powershell -NoProfile -Command "try{Set-Service SysMain -StartupType Disabled -EA Stop}catch{}" >nul 2>&1
    reg add "HKLM\SYSTEM\CurrentControlSet\Services\SysMain" /v Start /t REG_DWORD /d 4 /f >nul 2>&1
    reg query "HKLM\SYSTEM\CurrentControlSet\Services\SysMain" /v Start 2>nul | find "0x4" >nul
    if errorlevel 1 call :BAD "SysMain could not be disabled - run the tweak as Administrator"
)

if defined HIDDEN (
    echo @@PCT=100
    echo @@ERRS=!ERRS!
    echo @@DONE=1
    exit /b
)
echo.
echo   ======================================================================
if "!ERRS!"=="0" (echo     !G!DONE.   100%%   -   no errors   -   Processes before: %PROC%) else (echo     !R!DONE.   100%%   -   !ERRS! problem^(s^):!ERRLIST!!G!   -   Processes before: %PROC%)
echo     Backup saved in: %BK%
if not "%SEC%"=="ON" echo     If Defender is still ON after restart: turn OFF "Tamper Protection"
if not "%SEC%"=="ON" echo     in Windows Security, then run option 1 once more.
echo   ======================================================================
echo.
echo     Restarting in 5 seconds to lock in everything.
echo     Press N to cancel.
choice /c YN /t 5 /d Y /n >nul 2>&1
if errorlevel 2 ( echo     Canceled - restart yourself later. & timeout /t 2 >nul & goto MENU )
shutdown /r /t 5 /c "Bu 7maid Zero - restarting" >nul 2>&1
goto MENU

:: ============================================================
:STEP
set /a STEP+=1
set /a PCT=STEP*100/TOTAL
set /a FILL=PCT/5
set "BAR="
for /l %%i in (1,1,!FILL!) do set "BAR=!BAR!#"
for /l %%i in (!FILL!,1,19) do set "BAR=!BAR!."
if defined HIDDEN (if !PCT! GTR 96 set "PCT=96")
if defined HIDDEN echo @@PCT=!PCT!
set "SMSG=%~1"
if defined SHOWPCT set "SMSG=%~1 - !PCT!%%"
if defined HIDDEN echo @@MSG=!SMSG!
if defined HIDDEN exit /b
echo !G!  [!STEP!/!TOTAL!] [!BAR!] !PCT!%%   %~1
exit /b

:PINFIX
for %%S in (NgcSvc NgcCtnrSvc) do (
    sc config %%S start= demand >nul 2>&1
    reg query "HKLM\SYSTEM\CurrentControlSet\Services\%%S" /v Start 2>nul | findstr /c:"0x4" >nul
    if not errorlevel 1 (
        reg add "HKLM\SYSTEM\CurrentControlSet\Services\%%S" /v Start /t REG_DWORD /d 3 /f >nul 2>&1
        reg query "HKLM\SYSTEM\CurrentControlSet\Services\%%S" /v Start 2>nul | findstr /c:"0x4" >nul
        if not errorlevel 1 call :BAD "%%S is still disabled - set it to Manual in services.msc so the PIN works"
    )
)
exit /b

:BAD
set /a ERRS+=1
set "ERRLIST=!ERRLIST! / %~1"
if defined HIDDEN echo @@ERR=%~1
echo          !R!X  %~1!G!
exit /b

:GPUTWEAK
set "GPU=OTHER"
echo !GPUNAME! | findstr /i /c:"NVIDIA" >nul && set "GPU=NVIDIA"
echo !GPUNAME! | findstr /i /c:"Radeon" >nul && set "GPU=AMD"
echo !GPUNAME! | findstr /i /c:"Intel" >nul && set "GPU=INTEL"
echo          found: !GPUNAME!  -^>  applying !GPU! profile
powershell -NoProfile -Command "Get-ChildItem 'HKLM:\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}' -EA SilentlyContinue | ?{$_.PSChildName -match '^[0-9]{4}$'} | %%{ $k=$_.PSPath; New-ItemProperty -Path $k -Name 'PowerMizerEnable' -Value 1 -PropertyType DWord -Force -EA SilentlyContinue; New-ItemProperty -Path $k -Name 'PowerMizerLevel' -Value 1 -PropertyType DWord -Force -EA SilentlyContinue; New-ItemProperty -Path $k -Name 'PowerMizerLevelAC' -Value 1 -PropertyType DWord -Force -EA SilentlyContinue; New-ItemProperty -Path $k -Name 'PerfLevelSrc' -Value 8738 -PropertyType DWord -Force -EA SilentlyContinue }" >nul 2>&1
if "!GPU!"=="NVIDIA" call :NVIDIA
if "!GPU!"=="AMD" echo          AMD colors: Adrenalin - Display - Saturation 110-115 (GPU PANEL button explains)
if "!GPU!"=="AMD" (
    powershell -NoProfile -Command "Get-ChildItem 'HKLM:\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}' -EA SilentlyContinue | ?{$_.PSChildName -match '^[0-9]{4}$'} | %%{ New-ItemProperty -Path $_.PSPath -Name 'EnableUlps' -Value 0 -PropertyType DWord -Force -EA SilentlyContinue; New-ItemProperty -Path $_.PSPath -Name 'EnableUlps_NA' -Value 0 -PropertyType DWord -Force -EA SilentlyContinue; New-ItemProperty -Path $_.PSPath -Name 'PP_ThermalAutoThrottlingEnable' -Value 0 -PropertyType DWord -Force -EA SilentlyContinue }" >nul 2>&1
)
exit /b

:EXTRA
:: more processes off (all restorable: RESTORE re-reads the services backup + re-enables tasks)
for %%S in (WebClient RemoteAccess Wecsvc workfolderssvc lltdsvc p2pimsvc p2psvc PNRPsvc PNRPAutoReg HomeGroupListener HomeGroupProvider SmsRouter NaturalAuthentication TroubleshootingSvc perceptionsimulation spectrum MsKeyboardFilter uhssvc shpamsvc NvTelemetryContainer AdobeARMservice AdobeUpdateService LMS jhi_service BcastDVRUserService GoogleChromeElevationService brave bravem BraveElevationService MozillaMaintenance WSAIFabricSvc webthreatdefsvc webthreatdefusersvc Sense McpManagementService) do (
    start "" /b sc stop %%S >nul 2>&1
    sc config %%S start= disabled >nul 2>&1
)
for %%S in (SSDPSRV upnphost FDResPub fdPHost defragsvc TieringEngineService swprv VSS FrameServer FrameServerMonitor DusmSvc DevQueryBroker BDESVC EntAppSvc ClickToRunSvc WinRM LanmanServer) do sc config %%S start= demand >nul 2>&1
powershell -NoProfile -Command "Get-ScheduledTask -TaskName 'NvTm*','NvDriverUpdateCheck*','NvNodeLauncher*','NvProfileUpdater*' -EA SilentlyContinue | Disable-ScheduledTask -EA SilentlyContinue | Out-Null" >nul 2>&1
netsh int tcp set heuristics disabled >nul 2>&1
netsh int tcp set global rsc=disabled >nul 2>&1
netsh int tcp set global nonsackrttresiliency=disabled >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v Start_AccountNotifications /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v ShowSyncProviderNotifications /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v ShowTaskViewButton /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\System" /v EnableCdp /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search" /v DisableWebSearch /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\Personalization" /v NoLockScreenCamera /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\GameDVR" /v AllowGameDVR /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\SettingSync" /v DisableSettingSync /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\SettingSync" /v DisableSettingSyncUserOverride /t REG_DWORD /d 1 /f >nul 2>&1
reg delete "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel" /v DistributeTimers /f >nul 2>&1
reg delete "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v DisablePageCombining /f >nul 2>&1
powershell -NoProfile -Command "Enable-MMAgent -PageCombining -EA SilentlyContinue" >nul 2>&1
powershell -NoProfile -Command "Disable-MMAgent -ApplicationPreLaunch -EA SilentlyContinue; Get-ScheduledTask -EA SilentlyContinue | ?{ $_.TaskPath -eq '\' -and $_.TaskName -match '^(GoogleUpdate|MicrosoftEdgeUpdate|Adobe|CCleaner|Opera|Brave|Dropbox|Mozilla)' } | Disable-ScheduledTask -EA SilentlyContinue | Out-Null" >nul 2>&1
if "%SEC%"=="OFF" sc config wscsvc start= disabled >nul 2>&1
for %%S in (fhsvc WFDSConMgrSvc wlpasvc sshd ssh-agent MSiSCSI PeerDistSvc SNMPTRAP embeddedmode AppMgmt UdkUserSvc) do (
    start "" /b sc stop %%S >nul 2>&1
    sc config %%S start= disabled >nul 2>&1
)
for %%S in (RmSvc QWAVE SCPolicySvc CertPropSvc EFS NcdAutoSetup svsvc vds RpcLocator wmiApSrv NetSetupSvc AppReadiness) do sc config %%S start= demand >nul 2>&1
if "%SEC%"=="OFF" (
    sc stop SgrmBroker >nul 2>&1
    sc config SgrmBroker start= disabled >nul 2>&1
)
:: tray process of Windows Security is not the protection itself - safe to remove in both modes
reg delete "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" /v SecurityHealth /f >nul 2>&1
taskkill /f /im SecurityHealthSystray.exe >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "Latency Sensitive" /t REG_SZ /d True /f >nul 2>&1
:: v17.17 extra tweaks - all reversible, none touch security
reg delete "HKCU\System\GameConfigStore" /v GameDVR_FSEBehaviorMode /f >nul 2>&1
reg delete "HKCU\System\GameConfigStore" /v GameDVR_HonorUserFSEBehaviorMode /f >nul 2>&1
reg delete "HKCU\System\GameConfigStore" /v GameDVR_DXGIHonorFSEWindowsCompatible /f >nul 2>&1
reg add "HKCU\Software\Microsoft\GameBar" /v AutoGameModeEnabled /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\GameBar" /v ShowStartupPanel /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization" /v DODownloadMode /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\Windows Error Reporting" /v Disabled /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search" /v AllowCortana /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Search" /v BingSearchEnabled /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Dsh" /v AllowNewsAndInterests /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Feeds" /v EnableFeeds /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Edge" /v StartupBoostEnabled /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Edge" /v BackgroundModeEnabled /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Google\Chrome" /v BackgroundModeEnabled /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v TaskbarDa /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v TaskbarMn /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power" /v HiberbootEnabled /t REG_DWORD /d 0 /f >nul 2>&1
fsutil behavior set disablelastaccess 1 >nul 2>&1
fsutil behavior set disable8dot3 1 >nul 2>&1
for %%S in (DoSvc BcastDVRUserService CDPUserSvc DusmSvc) do (start "" /b sc stop %%S >nul 2>&1 & sc config %%S start= demand >nul 2>&1)
for %%S in (AppReadiness FrameServer FrameServerMonitor GraphicsPerfSvc CscService SmsRouter DisplayEnhancementService DevicesFlowUserSvc NaturalAuthentication SensorDataService SensorService SensrSvc StiSvc ssh-agent WebClient TieringEngineService UevAgentService vmicguestinterface vmicheartbeat vmickvpexchange vmicrdv vmicshutdown vmictimesync vmicvmsession vmicvss HvHost lltdsvc upnphost WalletService PrintDeviceConfigurationService PrintScanBrokerService spectrum perceptionsimulation) do (
    start "" /b sc stop %%S >nul 2>&1
    sc config %%S start= disabled >nul 2>&1
)
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\System" /v EnableActivityFeed /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy" /v LetAppsRunInBackground /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent" /v DisableWindowsConsumerFeatures /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v ShowSyncProviderNotifications /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Control Panel\Desktop" /v MenuShowDelay /t REG_SZ /d 0 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Services\NetBT\Parameters" /v NodeType /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters" /v DefaultTTL /t REG_DWORD /d 64 /f >nul 2>&1
echo !GPUNAME! | findstr /i /c:"NVIDIA" >nul && reg add "HKLM\SYSTEM\CurrentControlSet\Services\nvlddmkm\Parameters" /v RmGpsPsEnablePerCpuCoreDpc /t REG_DWORD /d 1 /f >nul 2>&1
:: close junk that is running right now (not needed for games)
for %%P in (OneDrive.exe Teams.exe ms-teams.exe PhoneExperienceHost.exe YourPhone.exe CompatTelRunner.exe GameBarPresenceWriter.exe Widgets.exe WidgetService.exe MicrosoftEdgeUpdate.exe AdobeARM.exe NvTelemetryContainer.exe CrossDeviceService.exe CrossDeviceResume.exe GameBarFTServer.exe) do taskkill /f /im %%P >nul 2>&1
:: stop Windows pushing old GPU drivers over yours, no auto-restart surprise, no wake timers
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate" /v ExcludeWUDriversInQualityUpdate /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU" /v NoAutoRebootWithLoggedOnUsers /t REG_DWORD /d 1 /f >nul 2>&1
powercfg -setacvalueindex scheme_current sub_sleep bd3b718a-0680-4d9d-8ab2-e1d2b4ac806d 0 >nul 2>&1
:: PCIe link power saving off, wireless adapter + USB hub power saving off (AC only)
powercfg -setacvalueindex scheme_current 501a4d13-42af-4429-9fd1-a8218c268e20 ee12f906-d277-404b-b6da-e5fa1a576df5 0 >nul 2>&1
powercfg -setacvalueindex scheme_current 19cbb8fa-5279-450e-9fac-8a3d5fedd0c1 12bbebe6-58d6-4636-95bb-3217ef867c1a 0 >nul 2>&1
powercfg -setacvalueindex scheme_current 2a737441-1930-4402-8d77-b2bebba308a3 0853a681-27c8-4100-a2fd-82013e970683 0 >nul 2>&1
powercfg -setactive scheme_current >nul 2>&1
:: Windows tips / spotlight / suggested apps off, taskbar search box off (less shell work)
for %%V in (RotatingLockScreenEnabled RotatingLockScreenOverlayEnabled SoftLandingEnabled SubscribedContent-310093Enabled SubscribedContent-338387Enabled SubscribedContent-338388Enabled SubscribedContent-338389Enabled SubscribedContent-338393Enabled SubscribedContent-353694Enabled SubscribedContent-353696Enabled SubscribedContent-353698Enabled OemPreInstalledAppsEnabled PreInstalledAppsEnabled ContentDeliveryAllowed) do reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v %%V /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Search" /v SearchboxTaskbarMode /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v Start_TrackProgs /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v Start_IrisRecommendations /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Privacy" /v TailoredExperiencesWithDiagnosticDataEnabled /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\System" /v PublishUserActivities /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\System" /v UploadUserActivities /t REG_DWORD /d 0 /f >nul 2>&1
:: input + graphics response
reg add "HKCU\Control Panel\Desktop" /v LowLevelHooksTimeout /t REG_DWORD /d 1000 /f >nul 2>&1
reg add "HKCU\Control Panel\Desktop" /v WaitToKillAppTimeout /t REG_SZ /d 2000 /f >nul 2>&1
reg add "HKCU\Control Panel\Desktop" /v ForegroundLockTimeout /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKCU\Control Panel\Desktop" /v DragFullWindows /t REG_SZ /d 0 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Services\GpuEnergyDrv" /v Start /t REG_DWORD /d 4 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "Affinity" /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "Background Only" /t REG_SZ /d False /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games" /v "Clock Rate" /t REG_DWORD /d 10000 /f >nul 2>&1
:: security-kept mode: make Defender lighter instead of off (low priority, idle-only scans)
if "%SEC%"=="ON" powershell -NoProfile -Command "Set-MpPreference -ScanAvgCPULoadFactor 10 -EnableLowCpuPriority $true -ScanOnlyIfIdleEnabled $true -DisableCatchupFullScan $true -EA SilentlyContinue" >nul 2>&1
:: full mode: no Security tray process
if "%SEC%"=="OFF" reg delete "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" /v SecurityHealth /f >nul 2>&1
powershell -NoProfile -Command "$t=[IO.File]::ReadAllText('%SELF%'); $m='#RAM'+'KEEP#'; $i=$t.IndexOf($m)+$m.Length; $j=$t.IndexOf('#'+'END#',$i); [IO.File]::WriteAllText($env:ProgramData+'\Bu7maidZero\ramkeep.ps1',$t.Substring($i,$j-$i))" >nul 2>&1
schtasks /Create /TN "Bu7maid RAM Keeper" /TR "powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File \"%ProgramData%\Bu7maidZero\ramkeep.ps1\"" /SC MINUTE /MO 2 /RU SYSTEM /RL HIGHEST /F >nul 2>&1
if errorlevel 1 call :BAD "RAM keeper task could not be created"
schtasks /Create /TN "Bu7maid RAM Boot" /TR "powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File \"%ProgramData%\Bu7maidZero\ramkeep.ps1\"" /SC ONSTART /DELAY 0000:20 /RU SYSTEM /RL HIGHEST /F >nul 2>&1
set "BKFIX=1"
start "" /b powershell -NoProfile -ExecutionPolicy Bypass -File "%ProgramData%\Bu7maidZero\ramkeep.ps1" >nul 2>&1
set "BKFIX="
echo          RAM keeper on: trims background RAM 20 s after every start, then every 2 min whenever RAM use goes above 40 percent (never touches your game)
echo          extra services + tasks off, power saving off, tips/ads off
exit /b

:TASKS
for %%T in (
 "\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser"
 "\Microsoft\Windows\Application Experience\ProgramDataUpdater"
 "\Microsoft\Windows\Application Experience\PcaPatchDbTask"
 "\Microsoft\Windows\Application Experience\StartupAppTask"
 "\Microsoft\Windows\Autochk\Proxy"
 "\Microsoft\Windows\Customer Experience Improvement Program\Consolidator"
 "\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip"
 "\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector"
 "\Microsoft\Windows\DiskFootprint\Diagnostics"
 "\Microsoft\Windows\Feedback\Siuf\DmClient"
 "\Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload"
 "\Microsoft\Windows\Windows Error Reporting\QueueReporting"
 "\Microsoft\Windows\Maps\MapsUpdateTask"
 "\Microsoft\Windows\Maps\MapsToastTask"
 "\Microsoft\Windows\Power Efficiency Diagnostics\AnalyzeSystem"
 "\Microsoft\Windows\NetTrace\GatherNetworkInfo"
 "\Microsoft\Windows\Shell\FamilySafetyMonitor"
 "\Microsoft\Windows\Shell\FamilySafetyRefreshTask"
 "\Microsoft\Windows\WDI\ResolutionHost"
 "\Microsoft\Windows\Registry\RegIdleBackup"
 "\Microsoft\Windows\Speech\SpeechModelDownloadTask"
 "\Microsoft\Windows\Diagnosis\Scheduled"
 "\Microsoft\Windows\Flighting\FeatureConfig\ReconcileFeatures"
 "\Microsoft\Windows\Flighting\OneSettings\RefreshCache"
 "\Microsoft\Windows\DeviceDirectoryClient\RegisterDeviceAccountChange"
 "\Microsoft\Windows\Management\Provisioning\Logon"
) do schtasks /Change /TN %%T %~1 >nul 2>&1
exit /b
:PAUSE
if not defined HIDDEN pause
exit /b

:PSRUNBG
start "" /b powershell -NoProfile -ExecutionPolicy Bypass -Command "$t=[IO.File]::ReadAllText('%SELF%'); $m='#'+'%~1#'; $i=$t.IndexOf($m)+$m.Length; $j=$t.IndexOf('#'+'END#',$i); iex $t.Substring($i,$j-$i)" >nul 2>&1
exit /b

:PSRUN
powershell -NoProfile -ExecutionPolicy Bypass -Command "$t=[IO.File]::ReadAllText('%SELF%'); $m='#'+'%~1#'; $i=$t.IndexOf($m)+$m.Length; $j=$t.IndexOf('#'+'END#',$i); iex $t.Substring($i,$j-$i)"
if errorlevel 1 call :BAD "part %~1 reported an error"
exit /b

:NVIDIA
echo          NVIDIA panel: Low Latency ULTRA, max performance, fast textures...
set "NPIMODE=APPLY"
call :PSRUN NPI
set "COLORMODE=APPLY"
call :PSRUN COLOR
if not exist "%ProgramData%\Bu7maidZero" md "%ProgramData%\Bu7maidZero" >nul 2>&1
> "%ProgramData%\Bu7maidZero\colors.vbs" echo CreateObject("WScript.Shell").Run "cmd /c """"%SELF%"" COLOR""", 0, False
schtasks /Create /TN "Bu7maid Colors" /TR "wscript.exe //B //Nologo \"%ProgramData%\Bu7maidZero\colors.vbs\"" /SC ONLOGON /RL HIGHEST /F >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Services\nvlddmkm\FTS" /v EnableRID66610 /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Services\nvlddmkm\FTS" /v EnableRID61684 /t REG_DWORD /d 0 /f >nul 2>&1
for %%S in (NvTelemetryContainer) do sc config %%S start= demand >nul 2>&1
powershell -NoProfile -Command "if(Get-AppxPackage -AllUsers -Name NVIDIACorp.NVIDIAControlPanel -EA SilentlyContinue){exit 0}else{exit 1}" >nul 2>&1
if not errorlevel 1 exit /b
where winget >nul 2>&1
if errorlevel 1 exit /b
echo          NVIDIA Control Panel missing - installing it silently...
winget install -e --id 9NF8H0H7WMLT --accept-package-agreements --accept-source-agreements --silent >nul 2>&1
exit /b

:CLEAN
del /f /s /q "%TEMP%\*" >nul 2>&1
for /d %%D in ("%TEMP%\*") do rd /s /q "%%D" >nul 2>&1
del /f /s /q "%SystemRoot%\Temp\*" >nul 2>&1
del /f /s /q "%SystemRoot%\Prefetch\*" >nul 2>&1
del /f /s /q "%SystemRoot%\SoftwareDistribution\Download\*" >nul 2>&1
ipconfig /flushdns >nul 2>&1
exit /b

:RAMPS
call :PSRUN RAMPS
exit /b

:: ============================================================
:FIXWIN
cls
set "TOTAL=8"
set "STEP=0"
set "SHOWPCT=1"
echo.
echo   FIX WINDOWS - repairs system files and finds why the PC restarts / crashes
echo.
call :STEP "Finding out why your PC restarts or crashes"
del /f /q "%BK%\fix.txt" >nul 2>&1
call :PSRUN CRASHWHY
if exist "%BK%\fix.txt" for /f "usebackq delims=" %%L in ("%BK%\fix.txt") do (
    if defined HIDDEN (echo @@DO=%%L) else echo     %%L
)
call :STEP "Checking the Windows image (DISM)"
set "DR=0"
DISM /Online /Cleanup-Image /CheckHealth 2>&1 | findstr /i /c:"repairable" >nul && set "DR=1"
if "!DR!"=="1" (
    if defined HIDDEN echo @@DO=Damage found - repairing from Windows Update, please wait
    DISM /Online /Cleanup-Image /RestoreHealth >nul 2>&1
    if errorlevel 1 (if defined HIDDEN echo @@DO=Could not finish the repair - check your internet and run it again) else (if defined HIDDEN echo @@DO=Windows image repaired)
) else (
    if defined HIDDEN echo @@DO=Windows image is healthy
)
call :STEP "Repairing system files (SFC) - the longest part"
set /a SFCBASE=STEP*100/TOTAL
set /a SFCSPAN=100/TOTAL-1
powershell -NoProfile -ExecutionPolicy Bypass -Command "$t=[IO.File]::ReadAllText('%SELF%'); $m='#'+'SFCRUN#'; $i=$t.IndexOf($m)+$m.Length; $j=$t.IndexOf('#'+'END#',$i); iex $t.Substring($i,$j-$i)"
set "SR=!errorlevel!"
if defined HIDDEN (
    if "!SR!"=="10" echo @@DO=All system files are fine
    if "!SR!"=="11" echo @@DO=Damaged system files were found and repaired - restart the PC
    if "!SR!"=="12" echo @@DO=Some files could not be repaired - run it again after a restart
    if "!SR!"=="13" echo @@DO=System file check finished
)
call :STEP "Scanning your system drive for errors"
set "CR=0"
chkdsk %SystemDrive% /scan >"%BK%\chk.txt" 2>&1
if errorlevel 1 set "CR=1"
if "!CR!"=="1" (
    chkdsk %SystemDrive% /spotfix >nul 2>&1
    if defined HIDDEN echo @@DO=Drive errors found - they get fixed at the next restart
) else (
    if defined HIDDEN echo @@DO=No drive errors
)
call :STEP "Undoing old tweaks that can freeze or break the PC"
bcdedit /deletevalue useplatformclock >nul 2>&1
bcdedit /deletevalue useplatformtick >nul 2>&1
bcdedit /deletevalue tscsyncpolicy >nul 2>&1
bcdedit /deletevalue disabledynamictick >nul 2>&1
reg delete "HKLM\SYSTEM\CurrentControlSet\Services\kbdclass\Parameters" /v KeyboardDataQueueSize /f >nul 2>&1
reg delete "HKLM\SYSTEM\CurrentControlSet\Services\mouclass\Parameters" /v MouseDataQueueSize /f >nul 2>&1
reg delete "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel" /v GlobalTimerResolutionRequests /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v DisablePagingExecutive /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl" /v Win32PrioritySeparation /t REG_DWORD /d 2 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile" /v SystemResponsiveness /t REG_DWORD /d 20 /f >nul 2>&1
set "MSIMODE=RESTORE"
call :PSRUN MSI
set "MSIMODE=USBFIX"
call :PSRUN MSI
netsh int tcp set global autotuninglevel=normal >nul 2>&1
netsh int tcp set global rss=enabled >nul 2>&1
netsh int tcp set global ecncapability=default >nul 2>&1
netsh int tcp set global timestamps=default >nul 2>&1
netsh int tcp set heuristics enabled >nul 2>&1
netsh winsock reset >nul 2>&1
ipconfig /flushdns >nul 2>&1
if defined HIDDEN echo @@DO=Timer and priority settings back to Windows defaults
if defined HIDDEN echo @@DO=Hardware interrupt tweaks undone, keyboard and mouse queues back to normal
if defined HIDDEN echo @@DO=Network settings reset - fixes slow or broken internet after tweaks
call :STEP "Lowering RAM usage and keeping it low"
if not exist "%ProgramData%\Bu7maidZero" md "%ProgramData%\Bu7maidZero" >nul 2>&1
powershell -NoProfile -Command "$t=[IO.File]::ReadAllText('%SELF%'); $m='#RAM'+'KEEP#'; $i=$t.IndexOf($m)+$m.Length; $j=$t.IndexOf('#'+'END#',$i); [IO.File]::WriteAllText($env:ProgramData+'\Bu7maidZero\ramkeep.ps1',$t.Substring($i,$j-$i))" >nul 2>&1
schtasks /Create /TN "Bu7maid RAM Keeper" /TR "powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File \"%ProgramData%\Bu7maidZero\ramkeep.ps1\"" /SC MINUTE /MO 2 /RU SYSTEM /RL HIGHEST /F >nul 2>&1
schtasks /Create /TN "Bu7maid RAM Boot" /TR "powershell -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File \"%ProgramData%\Bu7maidZero\ramkeep.ps1\"" /SC ONSTART /DELAY 0000:20 /RU SYSTEM /RL HIGHEST /F >nul 2>&1
for /f %%M in ('powershell -NoProfile -Command "$t=(Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory; $a=(Get-CimInstance Win32_PerfFormattedData_PerfOS_Memory).AvailableBytes; [int](100-100*$a/$t)"') do set "RB=%%M"
set "BKFIX=1"
powershell -NoProfile -ExecutionPolicy Bypass -File "%ProgramData%\Bu7maidZero\ramkeep.ps1" >nul 2>&1
set "BKFIX="
call :RAMPS >nul 2>&1
for /f %%M in ('powershell -NoProfile -Command "$t=(Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory; $a=(Get-CimInstance Win32_PerfFormattedData_PerfOS_Memory).AvailableBytes; [int](100-100*$a/$t)"') do set "RA=%%M"
if exist "%ProgramData%\Bu7maidZero\ram_top.txt" for /f "usebackq delims=" %%T in ("%ProgramData%\Bu7maidZero\ram_top.txt") do if defined HIDDEN echo @@DO=%%T
if defined RB if defined RA if defined HIDDEN echo @@DO=RAM in use: !RB! percent before, !RA! percent now
set "RB=" & set "RA="
if defined HIDDEN echo @@DO=RAM keeper installed - checks every 2 minutes and keeps RAM low, never touches games
call :STEP "Scanning for viruses and removing them"
set "MP=%ProgramData%\Microsoft\Windows Defender\Platform"
set "MPCMD="
for /f "delims=" %%E in ('dir /b /ad /o-n "%MP%" 2^>nul') do if not defined MPCMD set "MPCMD=%MP%\%%E\MpCmdRun.exe"
set "VR=99"
if exist "!MPCMD!" (
    "!MPCMD!" -SignatureUpdate >nul 2>&1
    "!MPCMD!" -Scan -ScanType 1 >nul 2>&1
    set "VR=!errorlevel!"
)
if "!VR!"=="0" (if defined HIDDEN echo @@DO=No viruses found)
if "!VR!"=="2" (if defined HIDDEN echo @@DO=Threats were found and removed - run VIRUS SCAN in Tools for a deeper check)
if "!VR!"=="99" (if defined HIDDEN echo @@DO=Windows antivirus engine not available - scan skipped)
call :STEP "Repairing broken Windows parts"
set "MSIMODE=USBFIX"
call :PSRUN MSI
reg delete "HKLM\SYSTEM\CurrentControlSet\Services\kbdclass\Parameters" /v KeyboardDataQueueSize /f >nul 2>&1
reg delete "HKLM\SYSTEM\CurrentControlSet\Services\mouclass\Parameters" /v MouseDataQueueSize /f >nul 2>&1
powershell -NoProfile -Command "$w=(& winmgmt /verifyrepository 2>&1) -join ' '; if($w -match 'inconsistent'){ & winmgmt /salvagerepository | Out-Null; exit 20 }; foreach($n in 'RpcSs','DcomLaunch','EventLog','PlugPlay','Power','Winmgmt','ProfSvc','Themes','Audiosrv','cbdhsvc','TabletInputService','TextInputManagementService','BFE','mpssvc','CryptSvc','TrustedInstaller','Dhcp','Dnscache','NlaSvc','nsi','netprofm','LanmanWorkstation','AudioEndpointBuilder','hidserv','Schedule','StateRepository','Appinfo','ShellHWDetection','UserManager','gpsvc','DeviceInstall','DeviceAssociationService','TimeBrokerSvc','SENS','StorSvc','ClipSVC','Wcmsvc','BrokerInfrastructure','CoreMessaging','LSM','SystemEventsBroker','WlanSvc','lmhosts','FontCache','SysMain'){ $k='HKLM:\SYSTEM\CurrentControlSet\Services\'+$n; if(Test-Path $k){ $v=(Get-ItemProperty $k -Name Start -EA SilentlyContinue).Start; if($v -eq 4){ Set-ItemProperty $k -Name Start -Value 3 -EA SilentlyContinue; $n | Out-File -Append '%BK%\fixsvc.txt' } } }; exit 0" >nul 2>&1
if "!errorlevel!"=="20" (if defined HIDDEN echo @@DO=Windows WMI database was broken - repaired)
if exist "%BK%\fixsvc.txt" (if defined HIDDEN echo @@DO=Re-enabled Windows services that were disabled) & del /f /q "%BK%\fixsvc.txt" >nul 2>&1
if defined HIDDEN echo @@DO=Restart the PC once so everything is applied
if defined HIDDEN echo @@PCT=100
echo   Done.
if not defined HIDDEN timeout /t 2 >nul
goto MENU

:CLEANONLY
cls
echo.
if defined HIDDEN echo @@MSG=Cleaning temp and cache
if defined HIDDEN echo @@PCT=10
echo   Cleaning temp + RAM + disk space (few minutes)...
call :CLEAN
set "DEEP=1"
if defined HIDDEN echo @@MSG=Deep disk cleanup (can take a few minutes)
if defined HIDDEN echo @@PCT=35
call :PSRUN DISK
if defined HIDDEN echo @@MSG=Cleaning RAM
if defined HIDDEN echo @@PCT=85
call :RAMPS
if defined HIDDEN echo @@PCT=100
echo   Done.
if not defined HIDDEN timeout /t 2 >nul
goto MENU

:VBSFIX
cls
set "ERRS=0"
echo.
echo   Forcing VBS / Memory Integrity / hypervisor OFF (also when Windows locked it)...
if defined HIDDEN echo @@MSG=Writing every VBS-off setting again
if defined HIDDEN echo @@PCT=15
reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard" /v EnableVirtualizationBasedSecurity /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard" /v RequirePlatformSecurityFeatures /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity" /v Enabled /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity" /v WasEnabledBy /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SOFTWARE\Policies\Microsoft\Windows\DeviceGuard" /v EnableVirtualizationBasedSecurity /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Control\Lsa" /v LsaCfgFlags /t REG_DWORD /d 0 /f >nul 2>&1
bcdedit /set hypervisorlaunchtype off >nul 2>&1
bcdedit /set vsmlaunchtype off >nul 2>&1
if defined HIDDEN echo @@MSG=Preparing the one-time boot step that removes the Windows lock
if defined HIDDEN echo @@PCT=40
set "EFIDRV="
for %%L in (S T U V W X Y Z) do if not exist %%L:\ if not defined EFIDRV set "EFIDRV=%%L:"
if not defined EFIDRV call :BAD "no free drive letter for the boot partition" & goto VBSEND
mountvol !EFIDRV! /s >nul 2>&1
if not exist "!EFIDRV!\EFI\Microsoft\Boot" call :BAD "could not open the boot partition" & goto VBSEND
copy /y "%WINDIR%\System32\SecConfig.efi" "!EFIDRV!\EFI\Microsoft\Boot\SecConfig.efi" >nul 2>&1
if errorlevel 1 call :BAD "SecConfig.efi could not be copied" & mountvol !EFIDRV! /d >nul 2>&1 & goto VBSEND
if defined HIDDEN echo @@PCT=65
bcdedit /create {0cb3b571-2f2e-4343-a879-d86a476d7215} /d "DebugTool" /application osloader >nul 2>&1
bcdedit /set {0cb3b571-2f2e-4343-a879-d86a476d7215} path "\EFI\Microsoft\Boot\SecConfig.efi" >nul 2>&1
bcdedit /set {bootmgr} bootsequence {0cb3b571-2f2e-4343-a879-d86a476d7215} >nul 2>&1
bcdedit /set {0cb3b571-2f2e-4343-a879-d86a476d7215} loadoptions DISABLE-LSA-ISO,DISABLE-VBS >nul 2>&1
bcdedit /set {0cb3b571-2f2e-4343-a879-d86a476d7215} device partition=!EFIDRV! >nul 2>&1
if errorlevel 1 call :BAD "boot step could not be set"
mountvol !EFIDRV! /d >nul 2>&1
:VBSEND
if defined HIDDEN echo @@PCT=100
if defined HIDDEN echo @@ERRS=!ERRS!
if defined HIDDEN echo @@DONE=1
echo   Done. Restart: a Windows screen appears BEFORE the logo - read it and press the key it asks to confirm.
if not defined HIDDEN timeout /t 3 >nul
goto MENU

:NAVFIX
cls
echo.
if defined HIDDEN echo @@MSG=Fixing File Explorer left panel
if defined HIDDEN echo @@PCT=30
echo   Bringing back File Explorer left panel (Home, Gallery, Desktop, Downloads, Documents)...
call :NAVFIXREG
powershell -NoProfile -Command "$g='{374DE290-123F-4565-9164-39C4925E467B}'; $u='HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders'; $p=[Environment]::ExpandEnvironmentVariables([string](Get-ItemProperty $u).$g); if(-not $p -or -not (Test-Path $p) -or $p -match 'OneDrive'){ $d=Join-Path $env:USERPROFILE 'Downloads'; New-Item $d -ItemType Directory -Force | Out-Null; Set-ItemProperty $u -Name $g -Value $d -Type ExpandString; Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders' -Name $g -Value $d }" >nul 2>&1
if defined HIDDEN echo @@MSG=Pinning Desktop, Downloads, Documents, Pictures
if defined HIDDEN echo @@PCT=50
powershell -NoProfile -Command "$sh=New-Object -ComObject Shell.Application; $k=Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders'; foreach($g in 'Desktop','{374DE290-123F-4565-9164-39C4925E467B}','Personal','My Pictures','My Music','My Video'){ try{ $p=[Environment]::ExpandEnvironmentVariables([string]$k.$g); if($p -and (Test-Path $p)){ $it=$sh.Namespace((Split-Path $p)).ParseName((Split-Path $p -Leaf)); try{$it.InvokeVerb('unpinfromhome')}catch{}; Start-Sleep -Milliseconds 300; $it.InvokeVerb('pintohome') } }catch{} }" >nul 2>&1
if defined HIDDEN echo @@PCT=70
taskkill /f /im explorer.exe >nul 2>&1
start "" explorer.exe
if defined HIDDEN echo @@PCT=100
echo   Done - open a new File Explorer window.
if not defined HIDDEN timeout /t 2 >nul
goto MENU

:NAVFIXREG
reg add "HKCU\Software\Classes\CLSID\{f874310e-b6b7-47dc-bc84-b9e6b38f5903}" /v System.IsPinnedToNameSpaceTree /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKCU\Software\Classes\CLSID\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}" /v System.IsPinnedToNameSpaceTree /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer" /v ShowFrequent /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer" /v ShowRecent /t REG_DWORD /d 1 /f >nul 2>&1
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v NavPaneShowAllFolders /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\System" /v EnableActivityFeed /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\System" /v PublishUserActivities /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy" /v LetAppsRunInBackground /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\System" /v EnableCdp /f >nul 2>&1
exit /b

:GAMESONLY
cls
echo.
set "GAMESCAN=1"
if defined HIDDEN echo @@MSG=Scanning your games
if defined HIDDEN echo @@PCT=15
echo   Scanning your games and tuning each one for max FPS...
call :PSRUN GAMEOPT
if defined HIDDEN (
    echo @@PCT=100
    echo @@ERRS=!ERRS!
    echo @@DONE=1
    exit /b
)
call :PAUSE
goto MENU

:RAMCLEAN
cls
echo.
if defined HIDDEN echo @@MSG=Cleaning RAM
if defined HIDDEN echo @@PCT=40
echo   Cleaning RAM...
call :RAMPS
if defined HIDDEN echo @@PCT=100
echo.
call :PAUSE
goto MENU

:STATUS
cls
echo.
echo   ---------------- BU 7MAID ZERO - STATUS ----------------
echo.
call :PSRUN STATUS
echo.
call :PAUSE
goto MENU

:SCAN
cls
echo.
echo   Quick virus scan (built-in Microsoft engine) - about 1-3 minutes...
echo.
set "MP=%ProgramData%\Microsoft\Windows Defender\Platform"
set "MPCMD="
for /f "delims=" %%E in ('dir /b /ad /o-n "%MP%" 2^>nul') do if not defined MPCMD set "MPCMD=%MP%\%%E\MpCmdRun.exe"
if exist "%MPCMD%" (
    echo   Updating virus definitions...
    "%MPCMD%" -SignatureUpdate >nul 2>&1
    echo   Scanning common infection spots + removing threats...
    "%MPCMD%" -Scan -ScanType 1
) else (
    echo   Defender engine not found - running Windows removal tool instead.
)
echo.
echo   Running Microsoft Malicious Software Removal Tool (quick)...
start /wait mrt.exe /Q /F >nul 2>&1
echo.
echo   Scan done. Anything found was cleaned.
echo.
call :PAUSE
goto MENU

:TOOLS
cls
echo.
echo   ---------------- 1-CLICK GPU TEST ----------------
where winget >nul 2>&1
if errorlevel 1 (
    echo   winget missing - install "App Installer" from Microsoft Store first.
    call :PAUSE
    goto MENU
)
set "SPX=%ProgramFiles%\Unigine\Superposition Benchmark\Superposition.exe"
if not exist "!SPX!" (
    echo   Downloading Superposition benchmark from the official package - about 1.5 GB, first time only...
    winget install -e --id Unigine.SuperpositionBenchmark --source winget --silent --accept-package-agreements --accept-source-agreements
)
if not exist "!SPX!" for /f "delims=" %%X in ('powershell -NoProfile -Command "(Get-ChildItem @($env:ProgramFiles,[Environment]::GetFolderPath('ProgramFilesX86')) -Filter Superposition.exe -Recurse -Depth 3 -EA SilentlyContinue | Select -First 1).FullName"') do set "SPX=%%X"
if not exist "!SPX!" (
    echo   Could not install it. Get it from benchmark.unigine.com/superposition
    call :PAUSE
    goto MENU
)
echo.
echo   HOW TO TEST (2-3 minutes):
echo    1. Preset = 1080p High   (or 4K Optimized for strong cards)
echo    2. Press RUN - it plays a short scene and measures everything
echo    3. At the end you get: SCORE, FPS min / avg / max, and GPU max temp
echo    4. Take a photo - run again after the tweak and compare
echo.
echo    Max temp guide: under 80C great, 80-85C ok, over 87C = check fans / airflow
echo.
start "" "!SPX!"
call :PAUSE
goto MENU

:DELAYTEST
cls
echo.
echo   ---------------- DPC LATENCY CHECKER ----------------
for /f "delims=" %%D in ('powershell -NoProfile -Command "[Environment]::GetFolderPath('Desktop')"') do set "DESK=%%D"
if not defined DESK set "DESK=%USERPROFILE%\Desktop"
set "DPX=!DESK!\DPC Latency Checker.exe"
call :DPCGET
if exist "!DPX!" exit /b
if defined HIDDEN echo @@MSG=Downloading DPC Latency Checker
echo   Downloading DPC Latency Checker (Thesycon graph version)...
powershell -NoProfile -Command "[Net.ServicePointManager]::SecurityProtocol='Tls12'; $d=$env:TEMP+'\dpcl'; Remove-Item $d -Recurse -Force -EA SilentlyContinue; New-Item -ItemType Directory $d -Force|Out-Null; $u=@('https://web.archive.org/web/2020id_/http://www.thesycon.de/dpclat.zip','https://web.archive.org/web/2019id_/http://www.thesycon.de/dpclat.zip','https://web.archive.org/web/2020id_/http://www.thesycon.de/dpclat.exe','https://www.thesycon.de/static/dpclat/dpclat.zip','https://www.thesycon.de/dpclat.zip','http://www.thesycon.de/dpclat.zip','https://www.thesycon.de/dpclat.exe','http://www.thesycon.de/dpclat.exe','https://www.thesycon.de/ger/dpclat.zip','https://www.thesycon.de/eng/dpclat.zip'); foreach($x in $u){ try{ $f=$d+'\dl.bin'; Invoke-WebRequest -Uri $x -OutFile $f -UseBasicParsing -TimeoutSec 25 -EA Stop; $b=[IO.File]::ReadAllBytes($f); if($b.Length -lt 20000){continue}; if($b[0] -eq 80 -and $b[1] -eq 75){ Expand-Archive $f $d\x -Force; $e=Get-ChildItem $d\x -Recurse -Filter *.exe|Select -First 1; if($e){Copy-Item $e.FullName '!DPX!' -Force; exit 0} } elseif($b[0] -eq 77 -and $b[1] -eq 90){ Copy-Item $f '!DPX!' -Force; exit 0} }catch{} }; exit 2"
if not errorlevel 1 exit /b
echo   Thesycon server did not answer - trying the modern Wagnardsoft version (works on Win 10/11)...
powershell -NoProfile -Command "[Net.ServicePointManager]::SecurityProtocol='Tls12'; $f=($env:TEMP+'\dpc_dl.exe'); try{ Invoke-WebRequest -Uri 'https://download.wagnardsoft.com/dpclatency/dpclatency%%20v1.0.0.7.exe' -OutFile $f -UseBasicParsing -EA Stop }catch{ exit 2 }; $h=(Get-FileHash $f -Algorithm SHA256).Hash; if($h -eq '23DAFC8846E088FFA953895AA020EE75C3C3BF396777826EDA5C7E5A7C234EE5'){ Move-Item -Force $f '!DPX!'; exit 0 } else { Remove-Item $f -Force -EA SilentlyContinue; exit 3 }"
if errorlevel 3 call :BAD "file did not match the official checksum - not saved"
if errorlevel 2 if not errorlevel 3 (call :BAD "could not download - check your internet" & start "" "https://www.softpedia.com/get/System/System-Info/Thesycon-DPC-Latency-Checker.shtml")
exit /b

:PINGTEST
cls
echo.
echo   ---------------- PING TEST ----------------
powershell -NoProfile -Command "$r=@(); 1..20|%%{ try{ $x=(New-Object Net.NetworkInformation.Ping).Send('1.1.1.1',1000); if($x.Status -eq 'Success'){$r+=$x.RoundtripTime} }catch{}; Start-Sleep -Milliseconds 250 }; if($r.Count){ $m=($r|Measure-Object -Average -Minimum -Maximum); $j=0; for($i=1;$i -lt $r.Count;$i++){$j+=[math]::Abs($r[$i]-$r[$i-1])}; $j=[math]::Round($j/[math]::Max(1,$r.Count-1),1); Write-Host ('   avg {0} ms   min {1}   max {2}   jitter {3} ms   lost {4}/20' -f [math]::Round($m.Average,1),$m.Minimum,$m.Maximum,$j,(20-$r.Count)) } else { Write-Host '   no reply - check your internet' }"
call :PAUSE
goto MENU

:BIOSMAN
set "BMODE=MANUAL"
echo.
echo     Board brand:   [1] ASUS   [2] MSI   [3] GIGABYTE   [4] ASROCK   [5] Other
choice /c 12345 /n /m "    Choose: "
if errorlevel 5 (set "BVEN=OTHER") else if errorlevel 4 (set "BVEN=ASROCK") else if errorlevel 3 (set "BVEN=GIGABYTE") else if errorlevel 2 (set "BVEN=MSI") else set "BVEN=ASUS"
echo.
echo     CPU:   [1] AMD Ryzen   [2] AMD Ryzen X3D (7800X3D / 9800X3D...)   [3] Intel
choice /c 123 /n /m "    Choose: "
if errorlevel 3 (set "BCPU=INTEL") else if errorlevel 2 (set "BCPU=X3D") else set "BCPU=AMD"
exit /b

:GPUGUIDE
cls
echo.
echo   ---------------- GPU PANEL SETTINGS (by hand, 2 min) ----------------
echo.
echo   NVIDIA: option 1 (AUTO) already sets these. To check: Control Panel - Manage 3D settings:
echo     Power management mode ........ Prefer maximum performance
echo     Low Latency Mode ............. On (Ultra if game has no Reflex)
echo     Texture filtering - Quality .. High performance
echo     Vertical sync ................ Off  (G-Sync: On + cap FPS 3 below Hz)
echo     In games: NVIDIA Reflex = On + Boost
echo.
echo   AMD Adrenalin - Gaming - Graphics:
echo     Radeon Anti-Lag = On,  Chill = Off,  Boost = Off
echo     Texture Filtering Quality = Performance,  Wait for V-Refresh = Always Off
echo.
echo   COLORS (better picture, zero FPS cost - 30 seconds):
echo     NVIDIA Control Panel - Display - Change resolution:
echo       Use NVIDIA color settings, Output color depth = highest (10 bpc if shown)
echo       Output dynamic range = FULL   (Limited = washed-out grey blacks)
echo     Display - Adjust desktop color settings - Digital vibrance = 60-70%%
echo     AMD: Adrenalin - Display - Pixel Format = RGB 4:4:4 Full RGB, Saturation +10
echo.
echo   Windows: Settings - Display - Graphics - set each game to High performance
echo   Monitor: Advanced display - set refresh rate to MAX (144/165/240Hz)
echo   ---------------------------------------------------------------------
echo.
call :PAUSE
goto MENU

:BIOS
cls
echo.
echo   ---------------- BU 7MAID BIOS ADVISOR ----------------
echo.
echo     [1]  AUTO   - read THIS PC (board, CPU, RAM, BIOS age, ReBAR)
echo     [2]  MANUAL - I choose the board brand + CPU (for a friend's PC)
echo.
choice /c 12 /n /m "    Choose 1 or 2: "
set "BMODE=AUTO" & set "BVEN=" & set "BCPU="
if errorlevel 2 call :BIOSMAN
cls
echo.
echo   ---------------- BU 7MAID BIOS ADVISOR  [!BMODE!] ----------------
call :PSRUN BIOSADV
echo.
set "BB="
set /p "BB=    Restart straight into BIOS now? (Y/N): "
if /i "%BB%"=="Y" shutdown /r /fw /t 5 >nul 2>&1
if /i "%BB%"=="Y" echo     Restarting into BIOS in 5 seconds...
call :PAUSE
goto MENU

:: ============================================================
:RESTORE
cls
echo.
echo   RESTORING Windows defaults...
set "TOTAL=9"
set "STEP=0"
call :STEP "Power plan back to Windows default"
powercfg -setactive 381b4222-f694-41f0-9685-ff5bb260df2e >nul 2>&1
powercfg -delete 99999999-9999-9999-9999-999999999999 >nul 2>&1
powercfg -h on >nul 2>&1
reg delete "HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling" /v PowerThrottlingOff /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Control" /v SvcHostSplitThresholdInKB /t REG_DWORD /d 3670016 /f >nul 2>&1
call :STEP "Services back to original"
if exist "%BK%\services_original.csv" start "" /b powershell -NoProfile -Command "Import-Csv '%BK%\services_original.csv' | %%{ $m=switch($_.StartMode){'Auto'{'auto'}'Manual'{'demand'}'Disabled'{'disabled'}default{$null}}; if($m){ sc.exe config $_.Name start= $m | Out-Null } }" >nul 2>&1
call :STEP "Privacy + background settings back"
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection" /v AllowTelemetry /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy" /v LetAppsRunInBackground /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications" /v GlobalUserDisabled /t REG_DWORD /d 0 /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Dsh" /v AllowNewsAndInterests /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\AutoLogger-Diagtrack-Listener" /v Start /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Control\WMI\Autologger\SQMLogger" /v Start /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Services\Ndu" /v Start /t REG_DWORD /d 2 /f >nul 2>&1
start "" /b powershell -NoProfile -Command "$d=@{OneSyncSvc=2;CDPUserSvc=2;PimIndexMaintenanceSvc=3;UnistoreSvc=3;UserDataSvc=3;MessagingService=3;cbdhsvc=3;BcastDVRUserService=3}; foreach($k in $d.Keys){ $p='HKLM:\SYSTEM\CurrentControlSet\Services\'+$k; if(Test-Path $p){ Set-ItemProperty -Path $p -Name Start -Value $d[$k] -EA SilentlyContinue } }" >nul 2>&1
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Search" /v BingSearchEnabled /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Services\xbgm" /v Start /t REG_DWORD /d 3 /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\MicrosoftEdge\Main" /v AllowPrelaunch /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\MicrosoftEdge\TabPreloader" /v AllowTabPreloading /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Microsoft\Windows\Windows Error Reporting" /v Disabled /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\StorageSense" /f >nul 2>&1
reg delete "HKCU\Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\Bags\AllFolders\Shell" /v FolderType /f >nul 2>&1
for %%L in ("Microsoft-Windows-SleepStudy/Diagnostic" "Microsoft-Windows-Kernel-Processor-Power/Diagnostic" "Microsoft-Windows-UserModePowerService/Diagnostic") do start "" /min cmd /c "wevtutil sl %%~L /e:true >nul 2>&1"
rem sign-in safety: Windows Hello PIN / password services must stay available
call :PINFIX
sc config KeyIso start= demand >nul 2>&1
sc config VaultSvc start= demand >nul 2>&1
start "" /min cmd /c "sc start NgcSvc >nul 2>&1"
start "" /min cmd /c "sc start NgcCtnrSvc >nul 2>&1"
call :STEP "Scheduled tasks + RAM keeper removed"
call :TASKS /Enable
schtasks /Delete /TN "Bu7maid RAM Keeper" /F >nul 2>&1
schtasks /Delete /TN "Bu7maid RAM Boot" /F >nul 2>&1
reg delete "HKLM\SYSTEM\CurrentControlSet\Services\nvlddmkm\Parameters" /v RmGpsPsEnablePerCpuCoreDpc /f >nul 2>&1
reg delete "HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling" /v PowerThrottlingOff /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization" /v DODownloadMode /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Microsoft\Windows\Windows Error Reporting" /v Disabled /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search" /v AllowCortana /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Dsh" /v AllowNewsAndInterests /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Feeds" /v EnableFeeds /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Edge" /v StartupBoostEnabled /f >nul 2>&1
for %%S in (AppReadiness FrameServer FrameServerMonitor GraphicsPerfSvc CscService SmsRouter DisplayEnhancementService DevicesFlowUserSvc NaturalAuthentication SensorDataService SensorService SensrSvc StiSvc ssh-agent WebClient TieringEngineService UevAgentService vmicguestinterface vmicheartbeat vmickvpexchange vmicrdv vmicshutdown vmictimesync vmicvmsession vmicvss HvHost lltdsvc upnphost WalletService PrintDeviceConfigurationService PrintScanBrokerService) do sc config %%S start= demand >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\System" /v EnableActivityFeed /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\System" /v PublishUserActivities /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy" /v LetAppsRunInBackground /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent" /v DisableWindowsConsumerFeatures /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Edge" /v BackgroundModeEnabled /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Google\Chrome" /v BackgroundModeEnabled /f >nul 2>&1
fsutil behavior set disablelastaccess 2 >nul 2>&1
fsutil behavior set memoryusage 1 >nul 2>&1
del "%ProgramData%\Bu7maidZero\ramkeep.ps1" >nul 2>&1
reg delete "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel" /v DistributeTimers /f >nul 2>&1
reg delete "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v DisablePageCombining /f >nul 2>&1
start "" /b powershell -NoProfile -Command "Enable-MMAgent -ApplicationPreLaunch -PageCombining -EA SilentlyContinue; Get-ScheduledTask -EA SilentlyContinue | ?{ $_.TaskPath -eq '\' -and $_.TaskName -match '^(GoogleUpdate|MicrosoftEdgeUpdate|Adobe|CCleaner|Opera|Brave|Dropbox|Mozilla)' } | Enable-ScheduledTask -EA SilentlyContinue | Out-Null" >nul 2>&1
call :STEP "Network + memory defaults"
netsh int tcp set heuristics enabled >nul 2>&1
netsh int tcp set global rsc=enabled >nul 2>&1
netsh int tcp set global nonsackrttresiliency=enabled >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\System" /v EnableCdp /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search" /v DisableWebSearch /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\SettingSync" /f >nul 2>&1
start "" /b powershell -NoProfile -Command "Get-ScheduledTask -TaskName 'NvTm*','NvDriverUpdateCheck*','NvNodeLauncher*','NvProfileUpdater*' -EA SilentlyContinue | Enable-ScheduledTask -EA SilentlyContinue | Out-Null; Set-MpPreference -ScanAvgCPULoadFactor 50 -EnableLowCpuPriority $false -EA SilentlyContinue" >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" /v SecurityHealth /t REG_EXPAND_SZ /d "%%windir%%\system32\SecurityHealthSystray.exe" /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate" /v ExcludeWUDriversInQualityUpdate /f >nul 2>&1
reg add "HKCU\Control Panel\Desktop" /v DragFullWindows /t REG_SZ /d 1 /f >nul 2>&1
call :STEP "Drivers, NVIDIA panel, colors, startup apps"
set "MSIMODE=RESTORE"
call :PSRUNBG MSI
set "GBV=1"
call :PSRUNBG GBPW
call :PSRUNBG STARTBACK
call :PSRUNBG STARTON
set "COLORMODE=RESTORE"
call :PSRUNBG COLOR
set "BRMODE=RESTORE"
call :PSRUNBG BROWSERDL
schtasks /Delete /TN "Bu7maid Colors" /F >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\OneDrive" /v DisableFileSyncNGSC /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot" /v TurnOffWindowsCopilot /f >nul 2>&1
reg delete "HKCU\Software\Policies\Microsoft\Windows\WindowsCopilot" /v TurnOffWindowsCopilot /f >nul 2>&1
if exist "%USERPROFILE%\OneDrive_Files" (where winget >nul 2>&1 && start "" /min cmd /c "winget install -e --id Microsoft.OneDrive --source winget --silent --accept-package-agreements --accept-source-agreements >nul 2>&1")
set "NPIMODE=RESTORE"
call :PSRUNBG NPI
set "GAMEMODE=RESTORE"
call :PSRUNBG GAMEOPT
call :STEP "Timers, mouse, keyboard defaults"
reg delete "HKCU\System\GameConfigStore" /v GameDVR_FSEBehaviorMode /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl" /v Win32PrioritySeparation /t REG_DWORD /d 2 /f >nul 2>&1
reg delete "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel" /v GlobalTimerResolutionRequests /f >nul 2>&1
bcdedit /deletevalue disabledynamictick >nul 2>&1
bcdedit /deletevalue tscsyncpolicy >nul 2>&1
bcdedit /deletevalue useplatformtick >nul 2>&1
reg add "%MM%" /v SystemResponsiveness /t REG_DWORD /d 20 /f >nul 2>&1
reg add "%MM%" /v NetworkThrottlingIndex /t REG_DWORD /d 10 /f >nul 2>&1
reg add "HKCU\Control Panel\Mouse" /v MouseSpeed /t REG_SZ /d 1 /f >nul 2>&1
reg add "HKCU\Control Panel\Mouse" /v MouseThreshold1 /t REG_SZ /d 6 /f >nul 2>&1
reg add "HKCU\Control Panel\Mouse" /v MouseThreshold2 /t REG_SZ /d 10 /f >nul 2>&1
reg add "HKCU\Control Panel\Keyboard" /v KeyboardDelay /t REG_SZ /d 1 /f >nul 2>&1
reg add "HKCU\Control Panel\Desktop" /v MenuShowDelay /t REG_SZ /d 400 /f >nul 2>&1
start "" /b powershell -NoProfile -Command "Get-ChildItem 'HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces' | %%{ Remove-ItemProperty -Path $_.PSPath -Name TcpAckFrequency,TCPNoDelay,TcpDelAckTicks -EA SilentlyContinue }" >nul 2>&1
start "" /b powershell -NoProfile -Command "Get-NetAdapter -Physical -EA SilentlyContinue | %%{ foreach($d in 'Energy Efficient Ethernet','Green Ethernet','Power Saving Mode','Advanced EEE','Gigabit Lite','Ultra Low Power Mode','Selective Suspend','Interrupt Moderation','Flow Control','Large Send Offload V2 (IPv4)','Large Send Offload V2 (IPv6)'){ Reset-NetAdapterAdvancedProperty -Name $_.Name -DisplayName $d -EA SilentlyContinue } }" >nul 2>&1
netsh interface teredo set state default >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\Psched" /v NonBestEffortLimit /f >nul 2>&1
reg delete "HKLM\SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters" /v DisabledComponents /f >nul 2>&1
start "" /b powershell -NoProfile -Command "Get-NetAdapter -Physical -EA SilentlyContinue | %%{ Set-DnsClientServerAddress -InterfaceIndex $_.ifIndex -ResetServerAddresses -EA SilentlyContinue }" >nul 2>&1
call :STEP "Visual effects + disk defaults"
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects" /v VisualFXSetting /t REG_DWORD /d 0 /f >nul 2>&1
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\VideoSettings" /v EnableAutoEnhanceDuringPlayback /f >nul 2>&1
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\VideoSettings" /v AllowLowResolution /f >nul 2>&1
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize" /v EnableTransparency /t REG_DWORD /d 1 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v DisablePagingExecutive /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters" /v EnablePrefetcher /t REG_DWORD /d 3 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters" /v EnableSuperfetch /t REG_DWORD /d 3 /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Microsoft\FTH" /v Enabled /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Microsoft\Windows\Dwm" /v OverlayTestMode /f >nul 2>&1
start "" /b powershell -NoProfile -Command "Enable-MMAgent -MemoryCompression -EA SilentlyContinue" >nul 2>&1
start "" /min cmd /c "DISM /Online /Set-ReservedStorageState /State:Enabled /NoRestart >nul 2>&1"
del /q "%BK%\rsv.done" "%BK%\rp.done" >nul 2>&1
for %%G in (FortniteClient-Win64-Shipping.exe VALORANT-Win64-Shipping.exe cs2.exe r5apex.exe r5apex_dx12.exe cod.exe RainbowSix.exe RainbowSix_Vulkan.exe TslGame.exe GTA5.exe GTA5_Enhanced.exe RustClient.exe Overwatch.exe MarvelRivals-Win64-Shipping.exe "League of Legends.exe" javaw.exe RobloxPlayerBeta.exe FiveM_GTAProcess.exe) do reg delete "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\%%~G\PerfOptions" /f >nul 2>&1
call :STEP "Security features back ON"
reg delete "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v FeatureSettingsOverride /f >nul 2>&1
reg delete "HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management" /v FeatureSettingsOverrideMask /f >nul 2>&1
reg delete "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard" /v EnableVirtualizationBasedSecurity /f >nul 2>&1
bcdedit /set hypervisorlaunchtype auto >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows Defender" /v DisableAntiSpyware /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\System" /v EnableSmartScreen /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer" /v SmartScreenEnabled /t REG_SZ /d Warn /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\SmartScreen" /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Edge" /v SmartScreenEnabled /f >nul 2>&1
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\AppHost" /v EnableWebContentEvaluation /f >nul 2>&1
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Attachments" /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Attachments" /f >nul 2>&1
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Associations" /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\UX Configuration" /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Spynet" /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection" /f >nul 2>&1
start "" /b powershell -NoProfile -Command "Set-MpPreference -DisableRealtimeMonitoring $false -DisableBehaviorMonitoring $false -DisableIOAVProtection $false -DisableScriptScanning $false -MAPSReporting 2 -SubmitSamplesConsent 1 -EA SilentlyContinue" >nul 2>&1
set "W=0"
:RWAIT
set /a W+=1
set "BUSY=0"
for /f %%B in ('tasklist /fi "imagename eq powershell.exe" /nh ^| find /c "powershell.exe"') do set "BUSY=%%B"
if not "!BUSY!"=="0" if !W! lss 20 (ping -n 3 127.0.0.1 >nul & goto RWAIT)
del "%BK%\sec_off.flag" >nul 2>&1
if defined HIDDEN (
    echo @@PCT=100
    echo @@ERRS=!ERRS!
    echo @@DONE=1
    exit /b
)
echo.
echo   Restored. Removed apps reinstall from Microsoft Store.
echo   For a full rollback use System Restore point "Bu7maid Zero Before".
echo   Restart the PC.
echo.
call :PAUSE
goto MENU
exit /b

:: ===== PowerShell payloads (read by :PSRUN, never run by CMD) =====
#HEALTH#
$o=@(); $bad=0; $out=Join-Path $env:BK 'health.txt'
try{
  $since=(Get-Date).AddDays(-14)
  $kp=@(Get-WinEvent -FilterHashtable @{LogName='System';ProviderName='Microsoft-Windows-Kernel-Power';Id=41;StartTime=$since} -EA SilentlyContinue).Count
  $bc=@(Get-WinEvent -FilterHashtable @{LogName='System';ProviderName='Microsoft-Windows-WER-SystemErrorReporting';Id=1001;StartTime=$since} -EA SilentlyContinue).Count
  $wh=@(Get-WinEvent -FilterHashtable @{LogName='System';ProviderName='Microsoft-Windows-WHEA-Logger';StartTime=$since} -EA SilentlyContinue).Count
  $md=@(Get-ChildItem "$env:SystemRoot\Minidump" -Filter *.dmp -EA SilentlyContinue | ?{ $_.LastWriteTime -gt $since }).Count
  if(($kp+$bc) -ge 2 -or $wh -ge 1 -or $md -ge 2){ $bad=1; $o+="This PC crashed recently - $($kp+$bc) crashes, $wh hardware errors - risky tweaks skipped - safe ones only" } else { $o+='No recent crashes found - full boost allowed' }
}catch{}
try{
  foreach($n in 'RpcSs','DcomLaunch','EventLog','PlugPlay','Power','Winmgmt','ProfSvc','Themes','AudioSrv','Audiosrv','cbdhsvc','TabletInputService','TextInputManagementService'){
    $k="HKLM:\SYSTEM\CurrentControlSet\Services\$n"
    if(Test-Path $k){ $v=(Get-ItemProperty $k -Name Start -EA SilentlyContinue).Start; if($v -eq 4){ Set-ItemProperty $k -Name Start -Value 3 -EA SilentlyContinue; $o+="Repaired disabled Windows service: $n" } }
  }
}catch{}
try{
  $w=(& winmgmt /verifyrepository 2>&1) -join ' '
  if($w -match 'inconsistent'){ & winmgmt /salvagerepository | Out-Null; $o+='Repaired the Windows WMI database' }
}catch{}
try{
  $d=(& DISM /Online /Cleanup-Image /CheckHealth 2>&1) -join ' '
  if($d -match 'repairable'){ $bad=1; $o+='Windows system files are damaged - run DISM RestoreHealth then sfc scannow later, risky tweaks skipped' }
}catch{}
try{
  $pe=(Test-Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired') -or (Test-Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending')
  if($pe){ $o+='Windows has a pending restart - restart after the boost' }
  $c=Get-Volume -DriveLetter ($env:SystemDrive.Substring(0,1)) -EA SilentlyContinue
  if($c -and $c.Size -gt 0 -and ($c.SizeRemaining/$c.Size) -lt 0.1){ $o+='System drive is almost full - free some space' }
  $e=@(Get-PnpDevice -PresentOnly -EA SilentlyContinue | ?{ $_.Status -eq 'Error' }).Count
  if($e -gt 0){ $o+="$e devices with driver problems in Device Manager" }
}catch{}
if($bad){ $o=@('UNSTABLE')+$o }
[IO.File]::WriteAllLines($out,[string[]]$o)
#END#
#CRASHWHY#
$o=@(); $out=Join-Path $env:BK 'fix.txt'
$map=@{
 0x124='Hardware error (WHEA): CPU or RAM overclock/undervolt too aggressive, or a failing part - turn XMP/EXPO and any OC off to test';
 0x133='Driver stuck too long (DPC watchdog): update chipset, SSD and network drivers and the SSD firmware';
 0x116='Graphics driver crashed: reinstall the GPU driver clean and lower any GPU overclock';
 0x117='Graphics driver crashed: reinstall the GPU driver clean and lower any GPU overclock';
 0x119='Graphics driver crashed: reinstall the GPU driver clean and lower any GPU overclock';
 0x141='Graphics driver crashed: reinstall the GPU driver clean and lower any GPU overclock';
 0x1A='Memory problem: RAM unstable - turn XMP/EXPO off, or run a memory test';
 0x4E='Memory problem: RAM unstable or bad driver - turn XMP/EXPO off, or run a memory test';
 0x50='Memory problem: RAM unstable or bad driver - turn XMP/EXPO off, or run a memory test';
 0x3B='Driver or RAM fault - update drivers, test the RAM';
 0xA='Bad driver touched memory it should not - update or remove the latest driver';
 0xD1='Bad driver - update or remove the latest installed driver';
 0xC4='Bad driver - update or remove the latest installed driver';
 0xC9='Bad driver - update or remove the latest installed driver';
 0x9F='A device did not wake or sleep right - update chipset, USB and network drivers';
 0xEF='A core Windows process died - system files damaged, this repair fixes it';
 0x1E='Kernel exception - drivers or system files, this repair fixes files';
 0x7A='Disk read problem - check the SSD health, cable and drivers';
 0x7B='Windows cannot read the boot drive - storage driver or SSD problem';
 0xF4='A critical process was killed - usually the system drive is failing';
 0x154='Storage driver failure - update the SSD and chipset drivers';
 0x139='Kernel security check failed - bad driver or unstable RAM';
 0x101='CPU stopped responding: CPU overclock/undervolt or BIOS power settings - reset the BIOS to defaults';
 0xC2='Bad driver - update or remove the latest installed driver';
 0x19='Corrupt memory pool - drivers or RAM'
}
try{
  $since=(Get-Date).AddDays(-30)
  $bc=@(Get-WinEvent -FilterHashtable @{LogName='System';ProviderName='Microsoft-Windows-WER-SystemErrorReporting';Id=1001;StartTime=$since} -EA SilentlyContinue)
  $kp=@(Get-WinEvent -FilterHashtable @{LogName='System';ProviderName='Microsoft-Windows-Kernel-Power';Id=41;StartTime=$since} -EA SilentlyContinue)
  $codes=@{}
  foreach($e in $bc){ if($e.Message -match 'bugcheck was: (0x[0-9a-fA-F]+)'){ $c=[Convert]::ToInt32($matches[1],16); if($c -ne 0){ $codes[$c]++ } } }
  if($codes.Count -gt 0){
    $o+="Blue screens in the last 30 days: $($bc.Count)"
    foreach($c in ($codes.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 3)){
      $why= if($map.ContainsKey([int]$c.Key)){ $map[[int]$c.Key] } else { 'Update all drivers and test the RAM' }
      $o+=('Code 0x{0:X} x{1}: {2}' -f $c.Key,$c.Value,$why)
    }
  } elseif($kp.Count -gt 0){
    $o+="The PC lost power suddenly $($kp.Count) times with no blue screen"
    $o+='Cause is usually: power supply, overheating, a loose power cable, or an unstable overclock'
  } else { $o+='No crashes or sudden restarts found in the last 30 days' }
  $gp=@(Get-WinEvent -FilterHashtable @{LogName='System';ProviderName='Display';Id=4101;StartTime=$since} -EA SilentlyContinue).Count
  if($gp -gt 0){ $o+="The graphics driver stopped and recovered $gp times - lower GPU overclock or reinstall the driver" }
  $ap=@(Get-WinEvent -FilterHashtable @{LogName='Application';ProviderName='Application Error';Id=1000;StartTime=$since} -EA SilentlyContinue)
  if($ap.Count -gt 0){
    $top=$ap | %{ $_.Properties[0].Value } | Group-Object | Sort-Object Count -Descending | Select-Object -First 3
    $o+=('Apps that crashed most: ' + (($top | %{ "$($_.Name) x$($_.Count)" }) -join ', '))
  }
  $pd=@(Get-PhysicalDisk -EA SilentlyContinue | ?{ $_.HealthStatus -ne 'Healthy' })
  if($pd.Count -gt 0){ $o+="A drive reports it is not healthy: $($pd[0].FriendlyName) - back up your files" }
}catch{ $o+='Could not read the crash history' }
$o=$o | %{ ($_ -replace '[()&|<>"%^]','') }
[IO.File]::WriteAllLines($out,[string[]]$o)
#END#
#SFCRUN#
$o=Join-Path $env:BK 'sfc.txt'; Remove-Item $o -EA SilentlyContinue
$p=Start-Process -FilePath "$env:SystemRoot\System32\sfc.exe" -ArgumentList '/scannow' -RedirectStandardOutput $o -WindowStyle Hidden -PassThru
$base=[int]$env:SFCBASE; $span=[int]$env:SFCSPAN; $last=-1
while(-not $p.HasExited){
  Start-Sleep -Seconds 2
  try{
    $fs=[IO.File]::Open($o,'Open','Read','ReadWrite'); $sr=New-Object IO.StreamReader($fs,[Text.Encoding]::Unicode); $t=$sr.ReadToEnd(); $sr.Close(); $fs.Close()
    $m=[regex]::Matches($t,'(\d{1,3})%'); if($m.Count -gt 0){ $n=[int]$m[$m.Count-1].Groups[1].Value; if($n -ne $last -and $n -le 100){ $last=$n; Write-Host ('@@PCT=' + ($base + [int]($span*$n/100))) } }
  }catch{}
}
$t=[IO.File]::ReadAllText($o,[Text.Encoding]::Unicode)
if($t -match 'did not find any integrity'){ exit 10 } elseif($t -match 'successfully repaired'){ exit 11 } elseif($t -match 'unable to fix'){ exit 12 } else { exit 13 }
#END#

#MSI#
$bk=Join-Path $env:BK 'msi_backup.csv'
if($env:MSIMODE -eq 'RESTORE'){
  if(Test-Path $bk){ Import-Csv $bk | %{ $p=$_.Path; if(Test-Path $p){ if($_.Orig -eq 'NONE'){ Remove-ItemProperty -Path $p -Name MSISupported -EA SilentlyContinue } else { Set-ItemProperty -Path $p -Name MSISupported -Value ([int]$_.Orig) -EA SilentlyContinue } } } }
} else {
  $rows=@(); if(Test-Path $bk){ $rows=@(Import-Csv $bk) }
  # USB controllers must never stay on forced MSI (can kill keyboard/mouse): put them back
  $usbp=@(Get-PnpDevice -Class USB -EA SilentlyContinue | ?{ $_.InstanceId -like 'PCI\*' } | %{ 'HKLM:\SYSTEM\CurrentControlSet\Enum\'+$_.InstanceId+'\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties' })
  $keep=@()
  foreach($r in $rows){ if($usbp -contains $r.Path){ if(Test-Path $r.Path){ if($r.Orig -eq 'NONE'){ Remove-ItemProperty -Path $r.Path -Name MSISupported -EA SilentlyContinue } else { Set-ItemProperty -Path $r.Path -Name MSISupported -Value ([int]$r.Orig) -EA SilentlyContinue } } } else { $keep+=$r } }
  $rows=$keep
  if($env:MSIMODE -eq 'USBFIX'){ if($rows.Count -gt 0){ $rows | Export-Csv -NoTypeInformation -Path $bk }; exit 0 }
  Get-PnpDevice -PresentOnly -EA SilentlyContinue | ?{ $_.InstanceId -like 'PCI\*' -and ($_.Class -eq 'Display' -or $_.Class -eq 'Net') } | %{
    $p='HKLM:\SYSTEM\CurrentControlSet\Enum\'+$_.InstanceId+'\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties'
    if(-not ($rows | ?{ $_.Path -eq $p })){ $o='NONE'; if(Test-Path $p){ $v=(Get-ItemProperty $p -Name MSISupported -EA SilentlyContinue).MSISupported; if($v -ne $null){ $o=[string]$v } }; $rows+=[pscustomobject]@{Path=$p;Orig=$o} }
    New-Item -Path $p -Force -EA SilentlyContinue | Out-Null; New-ItemProperty -Path $p -Name MSISupported -Value 1 -PropertyType DWord -Force -EA SilentlyContinue | Out-Null }
  $rows | Export-Csv -NoTypeInformation -Path $bk
}
#END#

#RAMKEEP#
Add-Type -TypeDefinition @"
using System; using System.Runtime.InteropServices;
public class BKeep {
  [StructLayout(LayoutKind.Sequential, Pack=1)] struct TP { public int Count; public long Luid; public int Attr; }
  [DllImport("psapi.dll")] public static extern bool EmptyWorkingSet(IntPtr h);
  [DllImport("ntdll.dll")] static extern uint NtSetSystemInformation(int cls, ref int info, int len);
  [DllImport("advapi32.dll", SetLastError=true)] static extern bool OpenProcessToken(IntPtr h, uint acc, out IntPtr tok);
  [DllImport("advapi32.dll", SetLastError=true)] static extern bool LookupPrivilegeValue(string sys, string name, out long luid);
  [DllImport("advapi32.dll", SetLastError=true)] static extern bool AdjustTokenPrivileges(IntPtr tok, bool all, ref TP ns, int len, IntPtr prev, IntPtr ret);
  [DllImport("kernel32.dll")] static extern IntPtr GetCurrentProcess();
  static void Priv(string n){ IntPtr t; OpenProcessToken(GetCurrentProcess(), 0x28, out t); TP tp = new TP(); tp.Count=1; tp.Attr=2; LookupPrivilegeValue(null, n, out tp.Luid); AdjustTokenPrivileges(t, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero); }
  public static void Prep(){ Priv("SeProfileSingleProcessPrivilege"); Priv("SeIncreaseQuotaPrivilege"); }
  [StructLayout(LayoutKind.Sequential)] struct MS { public uint dwLength; public uint dwMemoryLoad; public ulong a,b,c,d,e,f,g; }
  [DllImport("kernel32.dll")] static extern bool GlobalMemoryStatusEx(ref MS m);
  public static int Load(){ MS m=new MS(); m.dwLength=64; GlobalMemoryStatusEx(ref m); return (int)m.dwMemoryLoad; }
  public static void Purge(){ int c=3; NtSetSystemInformation(80, ref c, 4); c=4; NtSetSystemInformation(80, ref c, 4); }
}
"@
[BKeep]::Prep()
$games='^(FortniteClient|VALORANT|cs2|r5apex|cod|RainbowSix|TslGame|GTA5|RustClient|Overwatch|MarvelRivals|League of Legends|RobloxPlayerBeta|FiveM|Forza|RocketLeague|javaw)'
$keep='^(System|Idle|Registry|MemCompression|smss|csrss|wininit|winlogon|services|lsass|dwm|audiodg|fontdrvhost|NVDisplay\.Container|powershell)$'
try{
  $ci=Get-CimInstance Win32_OperatingSystem; $tot=$ci.TotalVisibleMemorySize/1MB
  $procs=Get-Process -EA SilentlyContinue | Sort-Object WorkingSet64 -Descending
  $sum=($procs | Measure-Object WorkingSet64 -Sum).Sum/1GB
  $used=[BKeep]::Load()*$tot/100
  $lines=@(); $lines+=('Total in use now: {0:N1} GB of {1:N0} GB' -f $used,$tot)
  foreach($p in ($procs | Select-Object -First 6)){ $lines+=('{0}: {1:N1} GB' -f $p.Name,($p.WorkingSet64/1GB)) }
  $rest=$used-$sum; if($rest -gt 2){ $lines+=('Memory used by drivers or Windows itself, not by a program: {0:N1} GB' -f $rest) }
  [IO.File]::WriteAllLines((Join-Path $env:ProgramData 'Bu7maidZero\ram_top.txt'),[string[]]$lines)
}catch{}
$up=(Get-Date)-(Get-CimInstance Win32_OperatingSystem).LastBootUpTime
$passes=1; if($up.TotalMinutes -lt 6 -and -not $env:BKFIX){ $passes=5 }
for($n=0;$n -lt $passes;$n++){
  $load=[BKeep]::Load()
  if($load -ge 25){
    $gaming=@(Get-Process -EA SilentlyContinue | ?{ $_.Name -match $games }).Count -gt 0
    foreach($p in (Get-Process -EA SilentlyContinue)){
      try{
        if($p.Name -match $keep -or $p.Name -match $games){ continue }
        if($p.WorkingSet64 -lt 30MB){ continue }
        [void][BKeep]::EmptyWorkingSet($p.Handle)
      }catch{}
    }
    if(-not $gaming){ [BKeep]::Purge() }
  }
  if($n -lt $passes-1){ Start-Sleep -Seconds 40 }
}
#END#

#HZSET#
Add-Type -TypeDefinition @"
using System; using System.Runtime.InteropServices;
public class BDisp {
  [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)] public struct DEVMODE {
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)] public string dmDeviceName;
    public short a,b; public int dmSize; public short c,d;
    public int e; public short f,g,h,i,j,k,l,m,n;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)] public string dmFormName;
    public short o,p; public int dmBitsPerPel,dmPelsWidth,dmPelsHeight,dmDisplayFlags,dmDisplayFrequency; }
  [DllImport("user32.dll")] public static extern bool EnumDisplaySettings(string dev,int mode,ref DEVMODE dm);
  [DllImport("user32.dll")] public static extern int ChangeDisplaySettings(ref DEVMODE dm,int flags);
  public static int Max(){
    DEVMODE cur=new DEVMODE(); cur.dmSize=(short)Marshal.SizeOf(typeof(DEVMODE));
    EnumDisplaySettings(null,-1,ref cur);
    int w=cur.dmPelsWidth,h=cur.dmPelsHeight,best=cur.dmDisplayFrequency,i=0;
    DEVMODE d=new DEVMODE(); d.dmSize=(short)Marshal.SizeOf(typeof(DEVMODE));
    while(EnumDisplaySettings(null,i,ref d)){ if(d.dmPelsWidth==w && d.dmPelsHeight==h && d.dmDisplayFrequency>best) best=d.dmDisplayFrequency; i++; }
    if(best>cur.dmDisplayFrequency){ cur.dmDisplayFrequency=best; cur.dmDisplayFlags=0; ChangeDisplaySettings(ref cur,1); }
    return best; }
}
"@
$hz=[BDisp]::Max()
Write-Host ("   Monitor set to highest: {0} Hz" -f $hz) -ForegroundColor Green
#END#

#RAMPS#
$os=Get-CimInstance Win32_OperatingSystem; $before=[math]::Round($os.FreePhysicalMemory/1024)
Add-Type -TypeDefinition @"
using System; using System.Runtime.InteropServices;
public class BMem {
  [StructLayout(LayoutKind.Sequential, Pack=1)] struct TP { public int Count; public long Luid; public int Attr; }
  [DllImport("ntdll.dll")] static extern uint NtSetSystemInformation(int cls, ref int info, int len);
  [DllImport("advapi32.dll", SetLastError=true)] static extern bool OpenProcessToken(IntPtr h, uint acc, out IntPtr tok);
  [DllImport("advapi32.dll", SetLastError=true)] static extern bool LookupPrivilegeValue(string sys, string name, out long luid);
  [DllImport("advapi32.dll", SetLastError=true)] static extern bool AdjustTokenPrivileges(IntPtr tok, bool all, ref TP ns, int len, IntPtr prev, IntPtr ret);
  [DllImport("kernel32.dll")] static extern IntPtr GetCurrentProcess();
  static void Priv(string n){ IntPtr t; OpenProcessToken(GetCurrentProcess(), 0x28, out t); TP tp = new TP(); tp.Count=1; tp.Attr=2; LookupPrivilegeValue(null, n, out tp.Luid); AdjustTokenPrivileges(t, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero); }
  public static void Clean(){ Priv("SeProfileSingleProcessPrivilege"); Priv("SeIncreaseQuotaPrivilege");
    int c=2; NtSetSystemInformation(80, ref c, 4); c=3; NtSetSystemInformation(80, ref c, 4); c=5; NtSetSystemInformation(80, ref c, 4); c=4; NtSetSystemInformation(80, ref c, 4); }
}
"@
[BMem]::Clean(); Start-Sleep -Milliseconds 400
$os=Get-CimInstance Win32_OperatingSystem; $after=[math]::Round($os.FreePhysicalMemory/1024)
Write-Host ("   RAM free: {0} MB  ->  {1} MB   (+{2} MB)" -f $before,$after,($after-$before)) -ForegroundColor Green
#END#

#GBPW#
$v=[int]$env:GBV
Add-Type -TypeDefinition @"
using System; using System.Runtime.InteropServices;
public class BPriv {
  [StructLayout(LayoutKind.Sequential, Pack=1)] struct TP { public int Count; public long Luid; public int Attr; }
  [DllImport("advapi32.dll", SetLastError=true)] static extern bool OpenProcessToken(IntPtr h, uint acc, out IntPtr tok);
  [DllImport("advapi32.dll", SetLastError=true)] static extern bool LookupPrivilegeValue(string sys, string name, out long luid);
  [DllImport("advapi32.dll", SetLastError=true)] static extern bool AdjustTokenPrivileges(IntPtr tok, bool all, ref TP ns, int len, IntPtr prev, IntPtr ret);
  [DllImport("kernel32.dll")] static extern IntPtr GetCurrentProcess();
  public static void Enable(string n){ IntPtr t; OpenProcessToken(GetCurrentProcess(), 0x28, out t); TP tp = new TP(); tp.Count=1; tp.Attr=2; LookupPrivilegeValue(null, n, out tp.Luid); AdjustTokenPrivileges(t, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero); }
}
"@
try {
  [BPriv]::Enable('SeTakeOwnershipPrivilege'); [BPriv]::Enable('SeRestorePrivilege')
  $k='SOFTWARE\Microsoft\WindowsRuntime\ActivatableClassId\Windows.Gaming.GameBar.PresenceServer.Internal.PresenceWriter'
  $adm=New-Object System.Security.Principal.SecurityIdentifier('S-1-5-32-544')
  $rk=[Microsoft.Win32.Registry]::LocalMachine.OpenSubKey($k,[Microsoft.Win32.RegistryKeyPermissionCheck]::ReadWriteSubTree,[System.Security.AccessControl.RegistryRights]::TakeOwnership)
  if($rk){ $acl=$rk.GetAccessControl([System.Security.AccessControl.AccessControlSections]::None); $acl.SetOwner($adm); $rk.SetAccessControl($acl); $rk.Close()
    $rk=[Microsoft.Win32.Registry]::LocalMachine.OpenSubKey($k,[Microsoft.Win32.RegistryKeyPermissionCheck]::ReadWriteSubTree,[System.Security.AccessControl.RegistryRights]::ChangePermissions)
    $acl=$rk.GetAccessControl(); $acl.SetAccessRule((New-Object System.Security.AccessControl.RegistryAccessRule($adm,'FullControl','ContainerInherit','None','Allow'))); $rk.SetAccessControl($acl); $rk.Close()
    Set-ItemProperty -Path "HKLM:\$k" -Name ActivationType -Value $v -Type DWord }
} catch {}
#END#

#STATUS#
function ShowL($name,$ok){ if($ok){$m='[ON ]';$c='Green'}else{$m='[OFF]';$c='Red'}; Write-Host ("   {0} {1}" -f $m,$name) -ForegroundColor $c }
function GetReg($p,$n){ try{ (Get-ItemProperty -Path $p -Name $n -EA Stop).$n }catch{ $null } }
$os=Get-CimInstance Win32_OperatingSystem; $cs=Get-CimInstance Win32_ComputerSystem
$ramGB=[math]::Round($cs.TotalPhysicalMemory/1GB); $usedGB=[math]::Round(($os.TotalVisibleMemorySize-$os.FreePhysicalMemory)/1MB,1)
Write-Host ("   Processes: {0}    RAM used: {1} / {2} GB" -f (Get-Process).Count,$usedGB,$ramGB) -ForegroundColor Green
Write-Host ""
ShowL 'Bu 7maid power plan active' (((powercfg /getactivescheme) -join '') -match 'Bu 7maid')
ShowL 'svchost merged (fewer processes)' ((GetReg 'HKLM:\SYSTEM\CurrentControlSet\Control' 'SvcHostSplitThresholdInKB') -gt 3670016)
try{ $dg=Get-CimInstance -Namespace root\Microsoft\Windows\DeviceGuard -ClassName Win32_DeviceGuard -EA Stop;ShowL 'VBS / Memory Integrity OFF (FPS)' ($dg.VirtualizationBasedSecurityStatus -ne 2) }catch{}
try{ShowL 'Defender realtime OFF (FPS)' (-not (Get-MpComputerStatus -EA Stop).RealTimeProtectionEnabled) }catch{ShowL 'Defender realtime OFF (FPS)' $true }
ShowL 'Game DVR OFF' ((GetReg 'HKCU:\System\GameConfigStore' 'GameDVR_Enabled') -eq 0)
ShowL 'Game Bar Presence Writer OFF' ((GetReg 'HKLM:\SOFTWARE\Microsoft\WindowsRuntime\ActivatableClassId\Windows.Gaming.GameBar.PresenceServer.Internal.PresenceWriter' 'ActivationType') -eq 0)
ShowL 'Hardware GPU scheduling ON' ((GetReg 'HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers' 'HwSchMode') -eq 2)
ShowL 'MPO OFF (no flicker/stutter)' ((GetReg 'HKLM:\SOFTWARE\Microsoft\Windows\Dwm' 'OverlayTestMode') -eq 5)
ShowL 'Foreground priority 0x26' ((GetReg 'HKLM:\SYSTEM\CurrentControlSet\Control\PriorityControl' 'Win32PrioritySeparation') -eq 38)
ShowL 'Network throttling OFF' ((GetReg 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile' 'NetworkThrottlingIndex') -eq 4294967295)
try{ShowL 'Memory compression OFF' (-not (Get-MMAgent -EA Stop).MemoryCompression) }catch{}
$sm=Get-Service SysMain -EA SilentlyContinue;ShowL 'SysMain OFF' (-not $sm -or $sm.StartType -eq 'Disabled')
ShowL 'Telemetry OFF' ((GetReg 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\DataCollection' 'AllowTelemetry') -eq 0)
$m=Get-CimInstance Win32_PhysicalMemory | Select-Object -First 1
if($m){ Write-Host ""; Write-Host ("   RAM speed now: {0} MT/s (rated {1}) - if lower, turn XMP/EXPO ON in BIOS" -f $m.ConfiguredClockSpeed,$m.Speed) -ForegroundColor Yellow }
if(@(Get-CimInstance Win32_Battery -EA SilentlyContinue).Count -gt 0){ Write-Host "   Laptop: plug in charger when gaming for full power." -ForegroundColor Yellow }
#END#

#BIOSADV#
function SecH($t){ Write-Host ""; Write-Host ("   == " + $t + " ==") -ForegroundColor Green }
function Ln($t,$c='Gray'){ Write-Host ("   " + $t) -ForegroundColor $c }
$bb=Get-CimInstance Win32_BaseBoard; $bi=Get-CimInstance Win32_BIOS; $cpu=(Get-CimInstance Win32_Processor|Select -First 1).Name.Trim()
$mem=Get-CimInstance Win32_PhysicalMemory|Select -First 1; $lap=@(Get-CimInstance Win32_Battery -EA SilentlyContinue).Count -gt 0
$mf="$($bb.Manufacturer)"; $model="$($bb.Product)"
$v='OTHER'; if($mf -match 'ASUS'){$v='ASUS'}elseif($mf -match 'Micro-Star|MSI'){$v='MSI'}elseif($mf -match 'Gigabyte'){$v='GIGABYTE'}elseif($mf -match 'ASRock'){$v='ASROCK'}
$amd=$cpu -match 'AMD'; $x3d=$cpu -match 'X3D'; $intel=$cpu -match 'Intel'
$bdate=$null; try{ $bdate=[datetime]$bi.ReleaseDate }catch{}
$manual=($env:BMODE -eq 'MANUAL')
if($manual){
  $v=$env:BVEN; $amd=($env:BCPU -ne 'INTEL'); $x3d=($env:BCPU -eq 'X3D'); $intel=($env:BCPU -eq 'INTEL'); $lap=$false
  $mf=$v; $model='(your board)'; $cpu=$(if($intel){'Intel Core'}elseif($x3d){'AMD Ryzen X3D'}else{'AMD Ryzen'})
}
if($manual){
SecH 'YOUR CHOICE'
Ln ("Board : {0}" -f $v) 'White'
Ln ("CPU   : {0}" -f $cpu) 'White'
} else {
SecH 'YOUR PC (auto-detected)'
Ln ("Board : {0} {1}" -f $mf,$model) 'White'
Ln ("BIOS  : {0}   date: {1}" -f $bi.SMBIOSBIOSVersion, $(if($bdate){$bdate.ToString('yyyy-MM-dd')}else{'?'})) 'White'
Ln ("CPU   : {0}" -f $cpu) 'White'
Ln ("RAM   : running {0} MT/s  (rated {1})" -f $mem.ConfiguredClockSpeed,$mem.Speed) 'White'
}
if($lap){ SecH 'LAPTOP'; Ln 'Laptop BIOS is locked by the maker - most options below will not exist.' 'Yellow'; Ln 'Only do: update BIOS from the maker website, and set power/fan mode to Performance in its app.' 'Yellow'; return }
$enter=switch($v){'ASUS'{'DEL (then F7 = Advanced Mode)'}'MSI'{'DEL (then F7 = Advanced Mode)'}'GIGABYTE'{'DEL (then F2 = Advanced Mode)'}'ASROCK'{'F2 or DEL'}default{'DEL or F2'}}
SecH 'HOW TO ENTER'
Ln ("Restart and press {0}  - or answer Y below to jump straight in." -f $enter)
$n=1
# 1 BIOS update
if($manual -or ($bdate -and $bdate -lt (Get-Date).AddMonths(-12))){
  SecH ("{0}. UPDATE BIOS  {1}" -f $n,$(if($manual){"(do this first)"}else{"(yours is over 1 year old)"})); $n++
  Ln ("Download newest BIOS for {0} from {1} support site, put it on a USB," -f $model,$mf)
  $fl=switch($v){'ASUS'{'Tool > ASUS EZ Flash 3'}'MSI'{'M-FLASH (bottom-left / main page)'}'GIGABYTE'{'Q-Flash (F8)'}'ASROCK'{'Tool > Instant Flash'}default{'the flash tool in BIOS'}}
  Ln ("then flash it from: {0}.  Do this FIRST - settings reset after." -f $fl)
  if($intel -and ($manual -or $cpu -match 'i[579]-1[34]\d{3}')){ Ln 'Intel 13th/14th gen: newest BIOS has the stability microcode fix - very important.' 'Yellow' }
}
# 2 XMP/EXPO
SecH ("{0}. RAM PROFILE  XMP / EXPO" -f $n); $n++
if(-not $manual -and $mem.ConfiguredClockSpeed -ge $mem.Speed -and $mem.Speed -gt 3200){ Ln ("Already ON - running {0} MT/s. Nothing to do." -f $mem.ConfiguredClockSpeed) 'Green' }
else{
  if($manual){ Ln 'Turn the RAM profile ON - RAM runs slow without it (big FPS gain).' 'Yellow' } else { Ln ("RAM is running {0} but is rated {1} - turn the profile ON (big FPS gain)." -f $mem.ConfiguredClockSpeed,$mem.Speed) 'Yellow' }
  $p=switch($v){'ASUS'{'Ai Tweaker > Ai Overclock Tuner > XMP I  (AMD: EXPO I)'}'MSI'{'OC > Extreme Memory Profile / A-XMP / EXPO  (also a button on the main page)'}'GIGABYTE'{'Tweaker > Extreme Memory Profile (X.M.P.) / EXPO > Profile1'}'ASROCK'{'OC Tweaker > DRAM Configuration > Load XMP / EXPO Setting'}default{'look for XMP / EXPO / DOCP in the memory or OC menu'}}
  Ln ("Where: {0}" -f $p)
}
# 3 ReBAR
SecH ("{0}. RESIZABLE BAR  (+FPS in many games)" -f $n); $n++
$rebar=$null
$smi="$env:ProgramFiles\NVIDIA Corporation\NVSMI\nvidia-smi.exe"; if(-not (Test-Path $smi)){ $smi=(Get-Command nvidia-smi.exe -EA SilentlyContinue).Source }
if($smi -and -not $manual){ try{ $q=& $smi -q -d MEMORY 2>$null | Out-String; if($q -match 'BAR1 Memory Usage\s+Total\s+:\s+(\d+)\s*MiB'){ $rebar=([int]$matches[1] -gt 256) } }catch{} }
if($rebar -eq $true){ Ln 'Already ON (checked from your NVIDIA card).' 'Green' }
else{
  if($rebar -eq $false){ Ln 'OFF on your card - turn it ON.' 'Yellow' } else { Ln 'Could not read it - check it is ON:' 'Yellow' }
  $p=switch($v){'ASUS'{'Advanced > PCI Subsystem Settings > Above 4G Decoding = Enabled, Re-Size BAR Support = Enabled'}'MSI'{'Settings > Advanced > PCI Subsystem Settings > Above 4G memory = Enabled, Re-Size BAR Support = Enabled'}'GIGABYTE'{'Settings > IO Ports > Above 4G Decoding = Enabled, Re-Size BAR Support = Auto/Enabled'}'ASROCK'{'Advanced > Chipset Configuration > Above 4G Decoding = Enabled, Re-Size BAR Support = Enabled'}default{'PCI / Chipset menu > Above 4G Decoding + Re-Size BAR = Enabled'}}
  Ln ("Where: {0}" -f $p)
  Ln 'Needs CSM = Disabled (Boot menu). Windows must be UEFI/GPT (normal on Win11).' 'DarkGray'
}
# 4 CPU tuning
if($amd){
  SecH ("{0}. AMD  PBO + CURVE OPTIMIZER  (more boost, cooler)" -f $n); $n++
  $p=switch($v){'ASUS'{'Ai Tweaker > Precision Boost Overdrive > Curve Optimizer'}'MSI'{'OC > Advanced CPU Configuration > AMD Overclocking > Precision Boost Overdrive > Curve Optimizer'}'GIGABYTE'{'Tweaker > Advanced CPU Settings > Precision Boost Overdrive > Curve Optimizer'}'ASROCK'{'Advanced > AMD Overclocking > Precision Boost Overdrive > Curve Optimizer'}default{'AMD Overclocking > Precision Boost Overdrive > Curve Optimizer'}}
  Ln ("Where: {0}" -f $p)
  Ln 'Set: PBO = Enabled (or Advanced), Curve Optimizer = All Cores, Sign = Negative, Magnitude = 15'
  Ln 'If you get a crash / blue screen, lower to 10, then 5. Never go past 30.' 'Yellow'
  if($x3d){ Ln 'X3D chip: keep C-States + Global C-State Control on AUTO (do NOT disable).' 'Yellow'; Ln 'Leave PBO limits on Auto - X3D is temperature-sensitive.' 'Yellow' }
  else { Ln 'Optional: Global C-State Control = Disabled for a bit less delay (more heat).' 'DarkGray' }
}
elseif($intel){
  SecH ("{0}. INTEL  POWER PROFILE" -f $n); $n++
  $p=switch($v){'ASUS'{'Ai Tweaker > Intel Default Settings / Performance profile'}'MSI'{'OC > Intel Default Settings (newer BIOS) or CPU Lite Load = Intel Default'}'GIGABYTE'{'Tweaker > Intel Default Settings / Performance'}'ASROCK'{'OC Tweaker > CPU Configuration > Intel Default / Performance'}default{'OC menu > Intel Default / Performance profile'}}
  Ln ("Where: {0}" -f $p)
  Ln 'Use the Intel "Performance" profile - fast AND safe for the CPU long-term.'
  Ln 'Turn OFF any "unlimited power" / MCE (Multi-Core Enhancement) on 13th/14th gen.' 'Yellow'
}
# 5 small safe ones
SecH ("{0}. SMALL EXTRAS" -f $n); $n++
Ln 'Boot > Fast Boot = Enabled (faster startup). To enter BIOS later use option 5 > Y.'
Ln 'Fan / Q-Fan: set CPU fan curve to Performance or Standard (cooler = higher boost)'
Ln 'SVM / VT-x (virtualization): leave as is - harmless, Windows hypervisor is already off.'
Ln 'Leave Secure Boot + TPM ON - Valorant, Battlefield 6 and new CoD need them to launch.' 'Yellow'
SecH 'AFTER'
Ln 'Save & Exit (F10). If the PC does not boot: clear CMOS or it auto-recovers after 3 tries.' 'White'
Ln 'Then run option 8 here - RAM speed line shows if XMP/EXPO worked.' 'White'
#END#

#STARTOFF#
$pat='Steam|Epic|Battle\.net|Rockstar|EADM|EA Desktop|EA app|Origin|Ubisoft|UplayLauncher|GalaxyClient|GOG|Teams|MicrosoftEdgeAutoLaunch|CCleaner|Adobe|AcroTray|OperaAssistant|Opera Browser Assistant|Skype|Overwolf|Razer Synapse Update|LGHUB Updater'
$bk="$env:SystemDrive\Bu7maid_Backup\startup_off.txt"; $done=@()
$runs=@(@('HKCU:\Software\Microsoft\Windows\CurrentVersion\Run','HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run'),
        @('HKLM:\Software\Microsoft\Windows\CurrentVersion\Run','HKLM:\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run'),
        @('HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run','HKLM:\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run32'))
foreach($r in $runs){
  $k=Get-Item -Path $r[0] -EA SilentlyContinue; if(-not $k){continue}
  if(-not (Test-Path $r[1])){ New-Item -Path $r[1] -Force | Out-Null }
  foreach($n in $k.GetValueNames()){ if($n -match $pat){
    Set-ItemProperty -Path $r[1] -Name $n -Value ([byte[]](3,0,0,0,0,0,0,0,0,0,0,0)) -Type Binary -EA SilentlyContinue
    $done+=("{0}|{1}" -f $r[1],$n) } }
}
if($done.Count){ $done | Set-Content -Path $bk -Encoding UTF8; Write-Host ("          launchers kept from auto-starting: {0}" -f $done.Count) -ForegroundColor DarkGray }
#END#

#STARTON#
$bk="$env:SystemDrive\Bu7maid_Backup\startup_off.txt"
if(Test-Path $bk){ foreach($l in Get-Content $bk){ $a=$l.Split('|',2); Set-ItemProperty -Path $a[0] -Name $a[1] -Value ([byte[]](2,0,0,0,0,0,0,0,0,0,0,0)) -Type Binary -EA SilentlyContinue }; Remove-Item $bk -EA SilentlyContinue }
#END#

#NPI#
$dir="$env:SystemDrive\Bu7maid_Backup\npi"; $exe=Join-Path $dir 'nvidiaProfileInspector.exe'
if(-not (Test-Path $exe)){ $f=Get-ChildItem $dir -Filter nvidiaProfileInspector.exe -Recurse -EA SilentlyContinue | Select -First 1; if($f){$exe=$f.FullName} }
if(-not (Test-Path $exe)){
  if($env:NPIMODE -eq 'RESTORE'){ return }
  New-Item $dir -ItemType Directory -Force | Out-Null
  try{ [Net.ServicePointManager]::SecurityProtocol='Tls12'; $z=Join-Path $env:TEMP 'npi.zip'
       Invoke-WebRequest 'https://github.com/Orbmu2k/nvidiaProfileInspector/releases/latest/download/nvidiaProfileInspector.zip' -OutFile $z -UseBasicParsing
       Expand-Archive $z -DestinationPath $dir -Force }catch{}
  $f=Get-ChildItem $dir -Filter nvidiaProfileInspector.exe -Recurse -EA SilentlyContinue | Select -First 1; if($f){$exe=$f.FullName}
}
if(-not (Test-Path $exe)){ Write-Host '          Profile Inspector download failed - set them by hand (option 7)' -ForegroundColor Red; return }
$lap=@(Get-CimInstance Win32_Battery -EA SilentlyContinue).Count -gt 0
$names=@{274197361='Power management mode';390467='Ultra Low Latency - CPL State';277041152='Ultra Low Latency - Enabled';13510289='Texture filtering - Quality';549528094='Threaded optimization'}
if($env:NPIMODE -eq 'RESTORE'){ $set=@(@(274197361,5),@(390467,0),@(277041152,0),@(13510289,0),@(549528094,0)) }
else{ $set=@(@(390467,1),@(277041152,0),@(13510289,20),@(549528094,1)); if(-not $lap){ $set+=,@(274197361,1) } }
$x='<?xml version="1.0" encoding="utf-16"?>'+"`r`n<ArrayOfProfile>`r`n  <Profile>`r`n    <ProfileName>Base Profile</ProfileName>`r`n    <Executeables />`r`n    <Settings>`r`n"
foreach($e in $set){ $id=[uint32]$e[0]; $val=[uint32]$e[1]; $nm=$names[[int]$e[0]]
  $x+="      <ProfileSetting>`r`n        <SettingNameInfo>$nm</SettingNameInfo>`r`n        <SettingID>$id</SettingID>`r`n        <SettingValue>$val</SettingValue>`r`n        <ValueType>Dword</ValueType>`r`n      </ProfileSetting>`r`n" }
$x+="    </Settings>`r`n  </Profile>`r`n</ArrayOfProfile>`r`n"
$nip=Join-Path $dir 'Bu7maid.nip'; [IO.File]::WriteAllText($nip,$x,[Text.Encoding]::Unicode)
try{ $pr=Start-Process -FilePath $exe -ArgumentList @('-silentImport',('"'+$nip+'"')) -PassThru -WindowStyle Hidden
     if(-not $pr.WaitForExit(30000)){ $pr.Kill() } }catch{}
if($env:NPIMODE -eq 'RESTORE'){ Write-Host '          NVIDIA panel back to default' -ForegroundColor DarkGray }
else{ Write-Host ('          NVIDIA panel set: Low Latency ON (not Ultra - Ultra costs FPS), Textures High performance, Threaded opt ON' + $(if($lap){' (laptop: power stays Adaptive)'}else{', Power = Max performance'})) -ForegroundColor DarkGray }
#END#

#GAMEOPT#
$ErrorActionPreference='SilentlyContinue'
$bk=Join-Path $env:BK 'games'; New-Item $bk -ItemType Directory -Force | Out-Null
$man=Join-Path $bk 'manifest.csv'
$gp='HKCU:\Software\Microsoft\DirectX\UserGpuPreferences'
$script:errs=0
function Say($t,$c='Gray'){ Write-Host ('          '+$t) -ForegroundColor $c }
function Err($t){ $script:errs++; Write-Host ('          '+$t) -ForegroundColor Red; if($env:HIDDEN){ Write-Host ('@@ERR='+$t) } }
# ---------- Game Mode: freeze background apps while a game runs (no resident process) ----------
$gmDir=Join-Path $env:ProgramData 'Bu7maidZero'; New-Item $gmDir -ItemType Directory -Force | Out-Null
$gm=Join-Path $gmDir 'gamemode.ps1'; $mk=Join-Path $gmDir 'gm_cod.txt'
$ns='root\subscription'
function RemoveSubs{
  Get-CimInstance -Namespace $ns -ClassName __FilterToConsumerBinding | ?{ $_.Filter -match 'Bu7maid' } | Remove-CimInstance
  Get-CimInstance -Namespace $ns -ClassName __EventFilter | ?{ $_.Name -like 'Bu7maid*' } | Remove-CimInstance
  Get-CimInstance -Namespace $ns -ClassName CommandLineEventConsumer | ?{ $_.Name -like 'Bu7maid*' } | Remove-CimInstance
}
if($env:GAMEMODE -eq 'RESTORE'){
  if(Test-Path $man){
    foreach($r in @(Import-Csv $man)){
      try{
        if($r.Type -eq 'FILE' -and (Test-Path $r.Backup)){ Copy-Item $r.Backup $r.Path -Force }
        elseif($r.Type -eq 'GPU'){ Remove-ItemProperty -Path $gp -Name $r.Path -EA SilentlyContinue }
      }catch{}
    }
    Remove-Item $man -Force -EA SilentlyContinue
    Say 'games: settings back to how they were' 'DarkGray'
  }
  try{ RemoveSubs; Remove-Item $gm -Force -EA SilentlyContinue }catch{}
  return
}
$codOk=(Test-Path (Join-Path $env:ProgramData 'Bu7maidZero\cod_ok.flag'))
$rows=@(); if(Test-Path $man){ $rows=@(Import-Csv $man) }
$new=@()
# ---------- Game Mode installer (runs LAST, in its own runspace with a 30 s time limit) ----------
$gmJob={ param($gm,$ns,$codOk)
$ErrorActionPreference='Stop'
function RemoveSubs{
  Get-CimInstance -Namespace $ns -ClassName __FilterToConsumerBinding | ?{ $_.Filter -match 'Bu7maid' } | Remove-CimInstance
  Get-CimInstance -Namespace $ns -ClassName __EventFilter | ?{ $_.Name -like 'Bu7maid*' } | Remove-CimInstance
  Get-CimInstance -Namespace $ns -ClassName CommandLineEventConsumer | ?{ $_.Name -like 'Bu7maid*' } | Remove-CimInstance
}
try{
  $pre=@('FortniteClient','VALORANT','cs2','r5apex','RainbowSix','TslGame','GTA5','FiveM','ForzaHorizon','RocketLeague','Overwatch','Marvel','RustClient','League of Legends','RobloxPlayerBeta')
  if($codOk){ $pre+=@('cod','BlackOps','ModernWarfare','Vanguard') }
  $rx='^('+(($pre | %{ [regex]::Escape($_) }) -join '|')+')'
  $script=@'
param($mode)
Add-Type -TypeDefinition 'using System;using System.Runtime.InteropServices;public class BGM{[DllImport("ntdll.dll")]public static extern int NtSuspendProcess(IntPtr h);[DllImport("ntdll.dll")]public static extern int NtResumeProcess(IntPtr h);}'
$junk='wallpaper32','wallpaper64','webwallpaper32','ui32','Lively','LivelyWpf','OneDrive','Teams','ms-teams','Widgets','PhoneExperienceHost','YourPhone','Dropbox','GoogleDriveFS','EpicWebHelper','AdobeARM','CCXProcess','Creative Cloud','AdobeIPCBroker','MicrosoftEdgeUpdate','CompatTelRunner'
$st=Join-Path $env:ProgramData 'Bu7maidZero\gm_suspended.txt'
if($mode -eq 'start'){
  $ids=@()
  foreach($n in $junk){ foreach($p in @(Get-Process -Name $n -EA SilentlyContinue)){ try{ [void][BGM]::NtSuspendProcess($p.Handle); $ids+=$p.Id }catch{} } }
  if($ids.Count){ $old=@(); if(Test-Path $st){ $old=@(Get-Content $st) }; ($old+$ids) | Select-Object -Unique | Set-Content $st }
}else{
  if(@(Get-Process -EA SilentlyContinue | ?{ $_.Name -match '__RX__' }).Count -gt 0){ exit }
  if(Test-Path $st){ foreach($i in @(Get-Content $st)){ try{ $p=Get-Process -Id ([int]$i) -EA Stop; [void][BGM]::NtResumeProcess($p.Handle) }catch{} }; Remove-Item $st -Force -EA SilentlyContinue }
}
'@
  $script=$script.Replace('__RX__',$rx.Replace("'","''"))
  [IO.File]::WriteAllText($gm,$script)
  RemoveSubs
  $cond=(($pre | %{ "TargetInstance.Name LIKE '"+$_+"%'" }) -join ' OR ')
  $q1="SELECT * FROM __InstanceCreationEvent WITHIN 3 WHERE TargetInstance ISA 'Win32_Process' AND ("+$cond+")"
  $q2="SELECT * FROM __InstanceDeletionEvent WITHIN 5 WHERE TargetInstance ISA 'Win32_Process' AND ("+$cond+")"
  foreach($s in @(@('Bu7maidGameStart',$q1,'start'),@('Bu7maidGameStop',$q2,'stop'))){
    $f=New-CimInstance -Namespace $ns -ClassName __EventFilter -Property @{Name=$s[0];EventNamespace='root\cimv2';QueryLanguage='WQL';Query=$s[1]} -ErrorAction Stop
    $c=New-CimInstance -Namespace $ns -ClassName CommandLineEventConsumer -Property @{Name=$s[0];CommandLineTemplate=('powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File "'+$gm+'" '+$s[2])} -ErrorAction Stop
    New-CimInstance -Namespace $ns -ClassName __FilterToConsumerBinding -Property @{Filter=[ref]$f;Consumer=[ref]$c} -ErrorAction Stop | Out-Null
  }
  Write-Output 'Game Mode ON: wallpaper engine / OneDrive / updaters freeze while a game runs, wake up after'
}catch{ Write-Output ('ERR:Game Mode could not be installed: '+$_.Exception.Message) }
}
function RunGM{
  try{ if((Test-Path $gm) -and (Test-Path $mk) -and ((Get-Content $mk -Raw).Trim() -eq "$codOk") -and @(Get-CimInstance -Namespace $ns -ClassName __EventFilter -EA Stop | ?{ $_.Name -eq 'Bu7maidGameStart' }).Count -gt 0){ Say 'Game Mode already ON (nothing to redo)' 'Green'; return } }catch{}
  try{
    $rsp=[runspacefactory]::CreateRunspace(); $rsp.Open(); $ps=[powershell]::Create(); $ps.Runspace=$rsp
    [void]$ps.AddScript($gmJob.ToString()).AddArgument($gm).AddArgument($ns).AddArgument($codOk)
    $h=$ps.BeginInvoke()
    if($h.AsyncWaitHandle.WaitOne(30000)){
      foreach($l in @($ps.EndInvoke($h))){ if("$l" -like 'ERR:*'){ Err ("$l".Substring(4)) } else { Say "$l" 'Green'; if("$l" -like 'Game Mode ON*'){ try{ Set-Content -LiteralPath $mk -Value "$codOk" }catch{} } } }
    }else{ Say 'Game Mode: Windows took too long - skipped (games are already tuned)' 'Yellow'; try{ $ps.Stop() }catch{} }
  }catch{ Err ('Game Mode: '+$_.Exception.Message) }
}
Say 'looking for your game folders...' 'DarkGray'
# ---------- find install folders ----------
$roots=@()
$steam=(Get-ItemProperty 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam').InstallPath
if($steam){
  $roots+=(Join-Path $steam 'steamapps\common')
  $vdf=Join-Path $steam 'steamapps\libraryfolders.vdf'
  if(Test-Path $vdf){ foreach($m in [regex]::Matches((Get-Content $vdf -Raw),'"path"\s+"([^"]+)"')){ $roots+=(Join-Path ($m.Groups[1].Value -replace '\\\\','\') 'steamapps\common') } }
}
foreach($f in @(Get-ChildItem "$env:ProgramData\Epic\EpicGamesLauncher\Data\Manifests\*.item")){ try{ $j=Get-Content $f.FullName -Raw | ConvertFrom-Json; if($j.InstallLocation){ $roots+=$j.InstallLocation } }catch{} }
$roots+=(Join-Path $env:LOCALAPPDATA 'FiveM')
foreach($d in @([IO.DriveInfo]::GetDrives() | ?{ $_.DriveType -eq 'Fixed' -and $_.IsReady } | %{ $_.RootDirectory.FullName })){
  foreach($n in 'Games','Program Files\Epic Games','Program Files (x86)\Epic Games','Program Files\Riot Games','Riot Games','Program Files (x86)\Call of Duty','Program Files\Call of Duty','Program Files\Rockstar Games','XboxGames','Program Files\EA Games','Program Files (x86)\Ubisoft','Program Files (x86)\Battle.net','Program Files (x86)\Overwatch'){
    $p=Join-Path $d $n; if(Test-Path $p){ $roots+=$p }
  }
}
$roots=@($roots | ?{ $_ -and (Test-Path $_) } | Select-Object -Unique)
# ---------- known games ----------
$games=@(
 @{N='Fortnite';X=@('FortniteClient-Win64-Shipping.exe')},
 @{N='VALORANT';X=@('VALORANT-Win64-Shipping.exe')},
 @{N='Counter-Strike 2';X=@('cs2.exe')},
 @{N='Apex Legends';X=@('r5apex.exe','r5apex_dx12.exe')},
 @{N='Call of Duty';COD=1;X=@('cod.exe','BlackOps6.exe','ModernWarfare.exe','cod23-cod.exe','BlackOpsColdWar.exe','Vanguard.exe','BlackOps7.exe')},
 @{N='Rainbow Six Siege';X=@('RainbowSix.exe','RainbowSix_Vulkan.exe')},
 @{N='PUBG';X=@('TslGame.exe')},
 @{N='GTA V';X=@('GTA5.exe','GTA5_Enhanced.exe')},
 @{N='FiveM';X=@('FiveM.exe','FiveM_*_GTAProcess.exe','FiveM_GTAProcess.exe')},
 @{N='Forza Horizon';X=@('ForzaHorizon5.exe','ForzaHorizon4.exe','ForzaHorizon6.exe')},
 @{N='Rocket League';X=@('RocketLeague.exe')},
 @{N='Overwatch 2';X=@('Overwatch.exe')},
 @{N='Marvel Rivals';X=@('Marvel-Win64-Shipping.exe','MarvelRivals-Win64-Shipping.exe')},
 @{N='Rust';X=@('RustClient.exe')},
 @{N='League of Legends';X=@('League of Legends.exe')},
 @{N='Roblox';X=@('RobloxPlayerBeta.exe')}
)
function Backup($file){
  $key=($file -replace '[\\/:*?"<>| ]','_'); $b=Join-Path $bk ($key+'.bak')
  if(-not (Test-Path $b)){ Copy-Item $file $b -Force }
  if(-not (@($rows)+@($new) | ?{ $_.Path -eq $file })){ $script:new+=[pscustomobject]@{Type='FILE';Path=$file;Backup=$b} }
}
function EditFile($file,$rules){
  if(-not (Test-Path $file)){ return 0 }
  $t=[IO.File]::ReadAllText($file); $o=$t; $c=0
  foreach($k in $rules.Keys){ $r=[regex]::new($k,'Multiline'); if($r.IsMatch($t)){ $t=$r.Replace($t,$rules[$k]); $c++; $script:hit+=$k } }
  if($t -ne $o){ Backup $file; [IO.File]::WriteAllText($file,$t); return $c }
  return 0
}
function SetGpu($exe){
  New-Item $gp -Force | Out-Null
  $had=(Get-ItemProperty $gp -Name $exe -EA SilentlyContinue)
  Set-ItemProperty -Path $gp -Name $exe -Value 'GpuPreference=2;'
  if(-not $had -and -not (@($rows)+@($new) | ?{ $_.Path -eq $exe })){ $script:new+=[pscustomobject]@{Type='GPU';Path=$exe;Backup=''} }
}
function SetPrio($leaf){
  $k='HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\'+$leaf+'\PerfOptions'
  New-Item $k -Force | Out-Null
  Set-ItemProperty $k -Name CpuPriorityClass -Value 3 -Type DWord
  Set-ItemProperty $k -Name IoPriority -Value 3 -Type DWord
}
$done=@(); $found=0; $script:hit=@()
$ue=@{ '^bUseVSync=.*$'='bUseVSync=False'; '^FrameRateLimit=.*$'='FrameRateLimit=0.000000'; '^sg\.ShadowQuality=.*$'='sg.ShadowQuality=0'; '^sg\.PostProcessQuality=.*$'='sg.PostProcessQuality=0'; '^sg\.EffectsQuality=.*$'='sg.EffectsQuality=0'; '^sg\.FoliageQuality=.*$'='sg.FoliageQuality=0'; '^sg\.ShadingQuality=.*$'='sg.ShadingQuality=0'; '^sg\.GlobalIlluminationQuality=.*$'='sg.GlobalIlluminationQuality=0'; '^sg\.ReflectionQuality=.*$'='sg.ReflectionQuality=0'; '^sg\.AntiAliasingQuality=.*$'='sg.AntiAliasingQuality=0'; '^bMotionBlur=.*$'='bMotionBlur=False'; '^sg\.ResolutionQuality=.*$'='sg.ResolutionQuality=50.000000'; '^(bLowLatencyMode|bEnableReflex|bNvidiaReflex|NvidiaReflex|bReflex)=.*$'='$1=True'; '^(bUseDLSS|bDLSSEnabled|DLSSEnabled|bEnableDLSS)=.*$'='$1=True' }
$allPat=@($games | %{ $_.X } | Select-Object -Unique)
$allPat=@($games | %{ $_.X } | Select-Object -Unique)
$wild=@($allPat | ?{ $_ -match "[*?]" }); $exact=@($allPat | ?{ $_ -notmatch "[*?]" })
$skip=@("Content","Paks","Engine","Plugins","Movies","Redist","_CommonRedist","Localization","ShaderCache","logs","Logs","crashes","mods","Mods","Screenshots","Saved","DirectX","vcredist","Support","Launcher","cache","Cache","data","Data","citizen","plugins","db","Binaries_Old")
if(-not ("GW" -as [type])){ Add-Type -TypeDefinition @'
using System; using System.Collections.Generic; using System.IO; using System.Text.RegularExpressions;
public static class GW {
  public static List<string> Hits = new List<string>();
  static HashSet<string> exact; static List<Regex> wild; static HashSet<string> skip;
  public static void Init(string[] e, string[] w, string[] s) {
    exact = new HashSet<string>(e, StringComparer.OrdinalIgnoreCase);
    wild = new List<Regex>(); foreach (string x in w) wild.Add(new Regex("^" + Regex.Escape(x).Replace("\\*", ".*").Replace("\\?", ".") + "$", RegexOptions.IgnoreCase));
    skip = new HashSet<string>(s, StringComparer.OrdinalIgnoreCase); Hits.Clear();
  }
  public static void Walk(string dir, int depth) {
    try {
      foreach (string f in Directory.EnumerateFiles(dir, "*.exe")) {
        string n = Path.GetFileName(f); bool ok = exact.Contains(n);
        if (!ok) foreach (Regex r in wild) if (r.IsMatch(n)) { ok = true; break; }
        if (ok) Hits.Add(f);
      }
      if (depth > 0) foreach (string d in Directory.EnumerateDirectories(dir)) {
        if (skip.Contains(Path.GetFileName(d))) continue;
        try { if ((File.GetAttributes(d) & FileAttributes.ReparsePoint) != 0) continue; } catch { continue; }
        Walk(d, depth - 1);
      }
    } catch { }
  }
}
'@ }
[GW]::Init([string[]]$exact,[string[]]$wild,[string[]]$skip)
Say ("scanning "+$roots.Count+" game folders (fast)...") "DarkGray"
$cf=Join-Path $bk 'hits.cache'; $useC=$false
if($env:GAMESCAN -ne '1' -and (Test-Path $cf)){ if(((Get-Date)-(Get-Item $cf).LastWriteTime).TotalHours -lt 6){ $cl=@(Get-Content $cf); if($cl.Count -gt 0 -and @($cl | ?{ -not (Test-Path $_) }).Count -eq 0){ $hitsAll=$cl; $useC=$true; Say 'games: using the list from the last scan (fast)' 'DarkGray' } } }
if(-not $useC){
  foreach($r in $roots){ $dep=4; if($r -like "*\FiveM*"){ $dep=2 }; [GW]::Walk($r,$dep) }
  $hitsAll=@([GW]::Hits | Select-Object -Unique)
  try{ $hitsAll | Set-Content -LiteralPath $cf }catch{}
}
foreach($g in $games){
  $hits=@(); foreach($pat in $g.X){ $hits+=@($hitsAll | ?{ [IO.Path]::GetFileName($_) -like $pat }) }
  $hits=@($hits | Select-Object -Unique)
  if($hits.Count -eq 0){ continue }
  $found++
  Say ('SCAN: '+$g.N+' - FOUND') 'Cyan'
  if($g.COD -and -not $codOk){ Say ('   '+$g.N+': skipped (you chose not to touch it)') 'Yellow'; if($env:HIDDEN){ Write-Host ('@@GAME='+$g.N+'|skipped|you chose not to touch it') }; continue }
  try{
    Say ('TUNING: '+$g.N+' ...') 'Yellow'
    foreach($h in $hits){ SetGpu $h; SetPrio ([IO.Path]::GetFileName($h)) }
    Say ('   '+$g.N+': GPU = high performance, priority HIGH, I/O priority HIGH') 'Green'
    $what=@('all tuned')
    $n=0; $script:hit=@()
    if($g.N -eq 'Fortnite'){
      $f=Join-Path $env:LOCALAPPDATA 'FortniteGame\Saved\Config\WindowsClient\GameUserSettings.ini'
      $n=EditFile $f $ue
      # DirectX 11 instead of DirectX 12 (DX12 costs FPS in Fortnite) - section is created if the game never wrote it
      if(Test-Path $f){
        $n+=EditFile $f @{ '^PreferredRHI=.*$'='PreferredRHI=dx11'; '^PreferredFeatureLevel=.*$'='PreferredFeatureLevel=sm5' }
        $tx=[IO.File]::ReadAllText($f)
        if($tx -notmatch '(?m)^PreferredRHI='){ Backup $f; $tx=$tx.TrimEnd()+"`r`n`r`n[D3DRHIPreference]`r`nPreferredRHI=dx11`r`nPreferredFeatureLevel=sm5`r`n"; [IO.File]::WriteAllText($f,$tx); $n++ }
        Say '   Fortnite: rendering mode set to DirectX 11 (more FPS than DX12) - close Fortnite before pressing BOOST' 'Green'
      }
    }
    elseif($g.N -eq 'GTA V' -or $g.N -eq 'FiveM'){
      foreach($f in @((Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'Rockstar Games\GTA V\settings.xml'),(Join-Path $env:APPDATA 'CitizenFX\gta5_settings.xml'))){
        $n+=EditFile $f @{ '<VSync value="[^"]*"'='<VSync value="0"'; '<MSAA value="[^"]*"'='<MSAA value="0"'; '<ReflectionMSAA value="[^"]*"'='<ReflectionMSAA value="0"'; '<ShadowQuality value="[^"]*"'='<ShadowQuality value="0"'; '<ReflectionQuality value="[^"]*"'='<ReflectionQuality value="0"'; '<PostFX value="[^"]*"'='<PostFX value="0"'; '<FXAA_Enabled value="[^"]*"'='<FXAA_Enabled value="false"'; '<Tessellation value="[^"]*"'='<Tessellation value="0"'; '<Shadow_SoftShadows value="[^"]*"'='<Shadow_SoftShadows value="0"' }
      }
    }
    elseif($g.N -eq 'Counter-Strike 2' -and $steam){
      foreach($f in @(Get-ChildItem (Join-Path $steam 'userdata\*\730\local\cfg\cs2_video.txt') -EA SilentlyContinue)){
        $n+=EditFile $f.FullName @{ '("setting\.mat_vsync"\s+)"\d+"'='$1"0"'; '("setting\.r_low_latency"\s+)"\d+"'='$1"1"'; '("setting\.msaa_samples"\s+)"\d+"'='$1"0"'; '("setting\.shaderquality"\s+)"\d+"'='$1"0"' }
      }
    }
    elseif($g.N -eq 'Apex Legends'){
      $f=Join-Path $env:USERPROFILE 'Saved Games\Respawn\Apex\local\videoconfig.txt'
      $n=EditFile $f @{ '("setting\.mat_vsync"\s+)"\d+"'='$1"0"' }
    }
    elseif($g.N -eq 'VALORANT'){
      foreach($f in @(Get-ChildItem (Join-Path $env:LOCALAPPDATA 'VALORANT\Saved\Config\*\Windows\GameUserSettings.ini') -EA SilentlyContinue)){
        $n+=EditFile $f.FullName $ue
      }
    }
    elseif($g.N -eq 'Marvel Rivals'){
      $f=Join-Path $env:LOCALAPPDATA 'Marvel\Saved\Config\Windows\GameUserSettings.ini'
      $n=EditFile $f $ue
    }
    elseif($g.N -eq 'Overwatch 2'){
      $f=Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'Overwatch\Settings\Settings_v0.ini'
      $n=EditFile $f @{ '^VerticalSync\s*=.*$'='VerticalSync = "0"' }
    }
    elseif($g.N -eq 'Rainbow Six Siege'){
      foreach($f in @(Get-ChildItem (Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'My Games\Rainbow Six - Siege\*\GameSettings.ini') -EA SilentlyContinue)){
        $n+=EditFile $f.FullName @{ '^VSync=.*$'='VSync=0' }
      }
    }
    elseif($g.N -eq 'Rocket League'){
      $f=Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'My Games\Rocket League\TAGame\Config\TASystemSettings.ini'
      $n=EditFile $f @{ '^(b?VSync)=True$'='$1=False' }
    }
    if($n -gt 0){ Say ('   '+$g.N+': lowest graphics + max FPS set ('+$n+' settings: VSync off, no FPS cap, shadows/effects/post-fx low)') 'Green' }
    else{ Say ('   '+$g.N+': no settings file to edit yet (open the game once, then run BOOST again)') 'DarkGray' }
    $hasDlss=(@($script:hit | ?{ $_ -match 'DLSS' }).Count -gt 0)
    if($hasDlss){ Say ('DLSS: '+$g.N+'  -  ON') 'Cyan' }
    elseif($g.N -in @('Fortnite','Marvel Rivals','Call of Duty','Forza Horizon','Rainbow Six Siege')){ Say ('   '+$g.N+': supports DLSS - set it to Ultra Performance in the game video menu') 'DarkGray' }
    if(@($script:hit | ?{ $_ -match 'Reflex' }).Count -gt 0){ Say ('   '+$g.N+': NVIDIA Reflex ON (lowest input delay)') 'Green' }
    if(@($script:hit | ?{ $_ -match 'Resolution' }).Count -gt 0){ Say ('   '+$g.N+': render scale 50% (performance first)') 'Green' }
    $det=@('GPU high performance','CPU + disk priority High')
    if($n -gt 0){ $det+=('game settings edited ('+$n+'): VSync off, low graphics') } else { $det+='no settings file yet - open the game once' }
    if($g.N -eq 'Fortnite' -and $n -gt 0){ $det+='DirectX 11' }
    if($hasDlss){ $det+='DLSS ON' }
    if(@($script:hit | ?{ $_ -match 'Reflex' }).Count -gt 0){ $det+='Reflex ON' }
    if($env:HIDDEN){ Write-Host ('@@GAME='+$g.N+'|'+$(if($n -gt 0){'tuned'}else{'nofile'})+'|'+($det -join ', ')) }
    Say ('DONE: '+$g.N+' tuned') 'Green'
    $done+=$g.N
  }catch{ Err ($g.N+' could not be tuned: '+$_.Exception.Message) }
}
if($new.Count){ ($rows+$new) | Export-Csv -NoTypeInformation -Path $man }
Say 'installing Game Mode (background apps freeze while you play)...' 'DarkGray'
RunGM
if($found -eq 0){ Say 'no supported games found on this PC' 'DarkGray' }
else{ Say ('games tuned: '+$done.Count+' of '+$found+'  (DLSS / graphics presets are set inside each game)') 'Green' }
if($script:errs -gt 0){ exit 1 }
#END#

#HWSVC#
$off=@(); function Dis($n){ $sv=Get-Service -Name $n -EA SilentlyContinue; if($sv -and $sv.StartType -ne 'Disabled'){ Stop-Service $n -Force -EA SilentlyContinue; Set-Service $n -StartupType Disabled -EA SilentlyContinue; $script:off+=$n } }
$virt='PDF|XPS|OneNote|Fax|AnyDesk|Send To'
$realPrinters=@(Get-CimInstance Win32_Printer -EA SilentlyContinue | ?{ $_.Name -notmatch $virt })
if($realPrinters.Count -eq 0){ Dis 'Spooler'; Dis 'PrintNotify' }
if(@(Get-PnpDevice -Class Bluetooth -PresentOnly -EA SilentlyContinue | ?{ $_.FriendlyName -notmatch 'Enumerator|RFCOMM|LE Generic' }).Count -eq 0){ Dis 'bthserv'; Dis 'BTAGService'; Dis 'BthAvctpSvc' }
if(@(Get-NetAdapter -EA SilentlyContinue | ?{ $_.PhysicalMediaType -match '802\.11|Native 802' -or $_.InterfaceDescription -match 'Wi-?Fi|Wireless|WLAN' }).Count -eq 0){ Dis 'WlanSvc'; Dis 'wcncsvc' }
if(@(Get-PnpDevice -Class Biometric -PresentOnly -EA SilentlyContinue).Count -eq 0){ Dis 'WbioSrvc' }
foreach($s in 'RemoteAccess','SessionEnv','TermService','UmRdpService','vmicguestinterface','vmicheartbeat','vmickvpexchange','vmicrdv','vmicshutdown','vmictimesync','vmicvmsession','vmicvss','lmhosts','NetTcpPortSharing','WebClient','seclogon','wcncsvc'){ Dis $s }
if($off.Count){ Write-Host ("          not needed on this PC -> turned off: {0}" -f ($off -join ', ')) -ForegroundColor DarkGray }
#END#

#DISK#
$dl=$env:SystemDrive.TrimEnd(':'); $free0=(Get-PSDrive $dl).Free
$vc='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches'
$cats='Active Setup Temp Folders','Delivery Optimization Files','Device Driver Packages','Downloaded Program Files','Internet Cache Files','Old ChkDsk Files','Previous Installations','Setup Log Files','System error memory dump files','System error minidump files','Temporary Files','Temporary Setup Files','Thumbnail Cache','Update Cleanup','Upgrade Discarded Files','Windows Error Reporting Files','Windows ESD installation files','Windows Upgrade Log Files','Windows Defender','Feedback Hub Archive log files','Language Pack'
foreach($c in $cats){ $k=Join-Path $vc $c; if(Test-Path $k){ Set-ItemProperty -Path $k -Name StateFlags0077 -Value 2 -Type DWord -EA SilentlyContinue } }
if($env:DEEP -eq '1'){ try{ $p=Start-Process cleanmgr.exe -ArgumentList '/sagerun:77' -PassThru -WindowStyle Hidden; if(-not $p.WaitForExit(240000)){ $p.Kill() } }catch{} }
try{ Delete-DeliveryOptimizationCache -Force -EA SilentlyContinue }catch{}
$junk=@("$env:SystemRoot\Minidump","$env:SystemRoot\LiveKernelReports","$env:LocalAppData\CrashDumps","$env:ProgramData\Microsoft\Windows\WER\ReportArchive","$env:ProgramData\Microsoft\Windows\WER\ReportQueue","$env:SystemDrive\NVIDIA","$env:SystemDrive\AMD","$env:ProgramData\NVIDIA Corporation\Downloader","$env:LocalAppData\NVIDIA Corporation\NV_Cache_old","$env:SystemRoot\Downloaded Program Files")
foreach($j in $junk){ if(Test-Path $j){ Get-ChildItem $j -Force -EA SilentlyContinue | Remove-Item -Recurse -Force -EA SilentlyContinue } }
Remove-Item "$env:SystemRoot\MEMORY.DMP" -Force -EA SilentlyContinue
Get-ChildItem "$env:SystemRoot\Logs\CBS" -Filter *.log -EA SilentlyContinue | ?{ $_.LastWriteTime -lt (Get-Date).AddDays(-1) } | Remove-Item -Force -EA SilentlyContinue
Get-ChildItem "$env:SystemRoot\Logs\CBS" -Filter *.cab -EA SilentlyContinue | Remove-Item -Force -EA SilentlyContinue
if($env:DEEP -eq '1'){ foreach($cap in 'MathRecognizer~~~~0.0.1.0','App.StepsRecorder~~~~0.0.1.0','Microsoft.Windows.WordPad~~~~0.0.1.0','Media.WindowsMediaPlayer~~~~0.0.12.0','Browser.InternetExplorer~~~~0.0.11.0'){ try{ Remove-WindowsCapability -Online -Name $cap -EA SilentlyContinue | Out-Null }catch{} } }
if($env:DEEP -eq '1'){ Write-Host '          removing old Windows update files (can take a few minutes)...' -ForegroundColor DarkGray; Start-Process dism.exe -ArgumentList '/Online','/Cleanup-Image','/StartComponentCleanup','/ResetBase','/Quiet' -Wait -WindowStyle Hidden }
$free1=(Get-PSDrive $dl).Free
Write-Host ("          disk space freed: {0:N1} GB   (free now: {1:N0} GB)" -f (($free1-$free0)/1GB),($free1/1GB)) -ForegroundColor Green
#END#

#ONEDRIVE#
$od=$env:OneDrive; if(-not $od){ $od=$env:OneDriveConsumer }
$exes=@("$env:LocalAppData\Microsoft\OneDrive\OneDrive.exe","$env:ProgramFiles\Microsoft OneDrive\OneDrive.exe","${env:ProgramFiles(x86)}\Microsoft OneDrive\OneDrive.exe")
$inst=@($exes | ?{ Test-Path $_ }).Count -gt 0
if(-not $inst -and -not (Get-Process -Name OneDrive -EA SilentlyContinue)){ Write-Host '          OneDrive not on this PC - skipped' -ForegroundColor DarkGray; return }
$ok=$true; $moved=@()
if($od -and (Test-Path $od)){
  Write-Host '          copying your OneDrive files to this PC first (online-only files get downloaded)...' -ForegroundColor DarkGray
  $usf='HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders'
  $map=[ordered]@{ 'Desktop'='Desktop'; 'Personal'='Documents'; 'My Pictures'='Pictures'; 'My Music'='Music'; 'My Video'='Videos'; '{F42EE2D3-909F-4907-8871-4C22FC0BF756}'='Documents'; '{0DDD015D-B06C-45D5-8C4C-F59713854639}'='Pictures'; '{754AC886-DF64-4CBA-86B5-F7FBF4FBCEF5}'='Desktop' }
  foreach($k in @($map.Keys)){
    $cur=(Get-ItemProperty $usf -Name $k -EA SilentlyContinue).$k; if(-not $cur){continue}
    $src=[Environment]::ExpandEnvironmentVariables($cur)
    if($src -like "$od*" -and (Test-Path $src)){
      $dst=Join-Path $env:USERPROFILE $map[$k]; New-Item $dst -ItemType Directory -Force | Out-Null
      if($moved -notcontains $src){ robocopy "$src" "$dst" /E /COPY:DAT /R:1 /W:1 /XJ /NFL /NDL /NJH /NJS /NP | Out-Null; if($LASTEXITCODE -ge 8){$ok=$false}; $moved+=$src }
      if($ok){ Set-ItemProperty $usf -Name $k -Value ('%USERPROFILE%\'+$map[$k]) -Type ExpandString }
    }
  }
  $rest=Join-Path $env:USERPROFILE 'OneDrive_Files'
  $xd=@(); foreach($m in $moved){ $xd+=$m }
  $rcArgs=@("$od","$rest",'/E','/COPY:DAT','/R:1','/W:1','/XJ','/NFL','/NDL','/NJH','/NJS','/NP'); if($xd.Count){ $rcArgs+='/XD'; $rcArgs+=$xd }
  & robocopy @rcArgs | Out-Null; if($LASTEXITCODE -ge 8){$ok=$false}
}
if(-not $ok){ Write-Host '          Some OneDrive files could not be copied (internet / space?) - OneDrive NOT removed, nothing deleted.' -ForegroundColor Red; return }
Stop-Process -Name OneDrive -Force -EA SilentlyContinue
$setups=@("$env:SystemRoot\System32\OneDriveSetup.exe","$env:SystemRoot\SysWOW64\OneDriveSetup.exe")
$setups+=@(Get-ChildItem "$env:LocalAppData\Microsoft\OneDrive","$env:ProgramFiles\Microsoft OneDrive","${env:ProgramFiles(x86)}\Microsoft OneDrive" -Filter OneDriveSetup.exe -Recurse -EA SilentlyContinue | %{ $_.FullName })
foreach($x in $setups){ if($x -and (Test-Path $x)){ try{ $pp=Start-Process $x -ArgumentList '/uninstall' -PassThru -WindowStyle Hidden; $null=$pp.WaitForExit(180000) }catch{} } }
New-Item 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\OneDrive' -Force | Out-Null
Set-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\OneDrive' -Name DisableFileSyncNGSC -Value 1 -Type DWord
Remove-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name OneDrive -EA SilentlyContinue
foreach($h in 'HKCU:\Software\Classes\CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}','HKCU:\Software\Classes\Wow6432Node\CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}'){ New-Item $h -Force | Out-Null; Set-ItemProperty $h -Name 'System.IsPinnedToNameSpaceTree' -Value 0 -Type DWord }
Write-Host ("          OneDrive removed. Your files: {0}  (+ Desktop/Documents/Pictures back to normal)" -f (Join-Path $env:USERPROFILE 'OneDrive_Files')) -ForegroundColor Green
if($od -and (Test-Path $od)){ Write-Host ("          The old folder {0} is still there - delete it yourself after you check your files." -f $od) -ForegroundColor Yellow }
#END#

#INPUT#
# keyboard: fastest repeat, no delay
Set-ItemProperty 'HKCU:\Control Panel\Keyboard' -Name KeyboardSpeed -Value '31' -EA SilentlyContinue
Set-ItemProperty 'HKCU:\Control Panel\Keyboard' -Name KeyboardDelay -Value '0' -EA SilentlyContinue
# mouse: 1:1 movement, no acceleration curve
Set-ItemProperty 'HKCU:\Control Panel\Mouse' -Name MouseSensitivity -Value '10' -EA SilentlyContinue
Set-ItemProperty 'HKCU:\Control Panel\Mouse' -Name MouseSpeed -Value '0' -EA SilentlyContinue
# no power saving on any USB / HID input device (no wake-up lag, no dropouts)
Get-ChildItem 'HKLM:\SYSTEM\CurrentControlSet\Enum\HID','HKLM:\SYSTEM\CurrentControlSet\Enum\USB' -Recurse -EA SilentlyContinue | ?{ $_.PSChildName -eq 'Device Parameters' } | %{
  New-ItemProperty -Path $_.PSPath -Name SelectiveSuspendEnabled -Value 0 -PropertyType DWord -Force -EA SilentlyContinue | Out-Null
  New-ItemProperty -Path $_.PSPath -Name EnhancedPowerManagementEnabled -Value 0 -PropertyType DWord -Force -EA SilentlyContinue | Out-Null }
# what devices are plugged in
$ids=@(Get-PnpDevice -PresentOnly -EA SilentlyContinue | ?{ $_.InstanceId -match 'VID_([0-9A-F]{4})' } | %{ ([regex]::Match($_.InstanceId,'VID_([0-9A-F]{4})')).Groups[1].Value } | Sort-Object -Unique)
$brands=[ordered]@{
 '046D'=@('Logitech','Logitech G HUB > your mouse > Report Rate = 1000 (or 2000/4000/8000 if offered)')
 '1532'=@('Razer','Razer Synapse > Performance > Polling Rate = 1000 (or HyperPolling 4000/8000)')
 '1038'=@('SteelSeries','SteelSeries GG > Engine > Polling Rate = 1000')
 '1B1C'=@('Corsair','Corsair iCUE > Device Settings > Polling Rate = 1000 (or 8000)')
 '0951'=@('HyperX','HyperX NGENUITY > Performance > Polling Rate = 1000')
 '03F0'=@('HyperX/HP','HyperX NGENUITY > Performance > Polling Rate = 1000')
 '0B05'=@('ASUS ROG','Armoury Crate > Device > Performance > Polling Rate = 1000')
 '1E7D'=@('Roccat/Turtle Beach','Swarm II app > Polling Rate = 1000')
 '258A'=@('Glorious / generic gaming','Glorious CORE (or the mouse button under it) > Polling Rate = 1000')
 '3554'=@('Pulsar / VXE','Pulsar Fusion or VXE Hub web app > Polling Rate = 1000+')
 '373B'=@('Lamzu / generic','brand web app > Polling Rate = 1000+')
 '1AF3'=@('ZOWIE','button under the mouse > set to 1000Hz')}
$pads=@{ '045E'='Xbox controller: use a USB cable for lowest delay; update it in the "Xbox Accessories" app'; '054C'='PlayStation controller: use a USB cable; in Steam > Settings > Controller turn on PlayStation support'; '2DC8'='8BitDo controller: update firmware with "8BitDo Ultimate Software", use the 2.4G dongle or cable'; '0E6F'='PDP controller: use the cable, update in Xbox Accessories app'; '24C6'='PowerA controller: use the cable, update in Xbox Accessories app' }
$found=$false
foreach($v in $ids){
  if($brands.Contains($v)){ $b=$brands[$v]; Write-Host ("          {0} found -> set it in {1}" -f $b[0],$b[1]) -ForegroundColor Yellow; $found=$true }
  if($pads.ContainsKey($v)){ Write-Host ("          {0}" -f $pads[$v]) -ForegroundColor Yellow; $found=$true }
}
if(-not $found){ Write-Host '          Mouse polling rate is set in the mouse app or a button under it - pick 1000 Hz or higher.' -ForegroundColor Yellow }
Write-Host '          (Windows cannot change mouse Hz itself - it lives in the mouse, so the app is the only way)' -ForegroundColor DarkGray
#END#

#COLOR#
$mode=$env:COLORMODE; $bk="$env:SystemDrive\Bu7maid_Backup\vibrance_original.txt"
if(-not (Test-Path "$env:SystemRoot\System32\nvapi64.dll")){ if($mode -ne 'REAPPLY'){ Write-Host '          colors: no NVIDIA driver found - skipped' -ForegroundColor DarkGray }; return }
Add-Type -TypeDefinition @"
using System; using System.Runtime.InteropServices;
public class BNv {
  [DllImport("nvapi64.dll", EntryPoint="nvapi_QueryInterface", CallingConvention=CallingConvention.Cdecl)] static extern IntPtr QI(uint id);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int InitD();
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int EnumD(int i, ref IntPtr h);
  [StructLayout(LayoutKind.Sequential)] public struct DVC { public uint ver; public int cur; public int min; public int max; }
  [StructLayout(LayoutKind.Sequential)] public struct DVCX { public uint ver; public int cur; public int min; public int max; public int def; }
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int GetD(IntPtr h, uint outId, ref DVC info);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int SetD(IntPtr h, uint outId, int level);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int GetX(IntPtr h, uint outId, ref DVCX info);
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int SetX(IntPtr h, uint outId, ref DVCX info);
  static Delegate F(uint id, Type t){ IntPtr p=QI(id); if(p==IntPtr.Zero) return null; return Marshal.GetDelegateForFunctionPointer(p,t); }
  // pct = wanted vibrance in NVIDIA Control Panel percent (0-100, 50 = default). restore>=0 puts back that percent.
  public static string Run(int pct, int restore){
    InitD init=(InitD)F(0x0150E828,typeof(InitD)); EnumD en=(EnumD)F(0x9ABDD40D,typeof(EnumD));
    if(init==null||en==null) return "missing";
    if(init()!=0) return "initfail";
    GetX gx=(GetX)F(0x0E45002D,typeof(GetX)); SetX sx=(SetX)F(0x4A82C2B1,typeof(SetX));
    GetD g0=(GetD)F(0x4085DE45,typeof(GetD)); SetD s0=(SetD)F(0x172409B4,typeof(SetD));
    string res=""; int want=restore>=0?restore:pct;
    for(int i=0;i<8;i++){
      IntPtr h=IntPtr.Zero; if(en(i,ref h)!=0) break;
      if(gx!=null && sx!=null){
        DVCX d=new DVCX(); d.ver=(uint)(Marshal.SizeOf(typeof(DVCX)) | (1<<16));
        if(gx(h,0,ref d)==0){
          int lo=d.min, hi=d.max; int lv=lo+(int)Math.Round((hi-lo)*(want/100.0));
          if(res.Length==0) res="orig="+(int)Math.Round((d.cur-lo)*100.0/Math.Max(1,hi-lo))+";";
          d.cur=lv; int r=sx(h,0,ref d);
          DVCX c=new DVCX(); c.ver=d.ver; int ok=gx(h,0,ref c);
          int now=(int)Math.Round((c.cur-lo)*100.0/Math.Max(1,hi-lo));
          res+="d"+i+"="+now+(r==0&&ok==0&&c.cur==lv?"ok":"bad")+";";
          continue;
        }
      }
      if(g0!=null && s0!=null){
        DVC d=new DVC(); d.ver=(uint)(Marshal.SizeOf(typeof(DVC)) | (1<<16));
        if(g0(h,0,ref d)!=0) continue;
        int lv=d.min+(int)Math.Round((d.max-d.min)*(want/100.0));
        if(res.Length==0) res="orig="+(int)Math.Round((d.cur-d.min)*100.0/Math.Max(1,d.max-d.min))+";";
        int r=s0(h,0,lv); DVC c=new DVC(); c.ver=d.ver; int ok=g0(h,0,ref c);
        res+="d"+i+"="+(int)Math.Round((c.cur-d.min)*100.0/Math.Max(1,d.max-d.min))+(r==0&&ok==0&&c.cur==lv?"ok":"bad")+";";
      }
    }
    return res;
  }
}
"@
try{
  if($mode -eq 'RESTORE'){
    $o=50
    $r=[BNv]::Run(50,$o); Remove-Item $bk -EA SilentlyContinue
    Write-Host '          colors back to default' -ForegroundColor DarkGray
  } else {
    $r=[BNv]::Run(70,-1)
    if($mode -ne 'REAPPLY'){
      if($r -match 'd\d=\d+ok'){ Write-Host ('          colors: Digital Vibrance set and verified ('+($r -replace ';',' ')+') - stronger colors, 0 FPS cost') -ForegroundColor Green }
      else { Write-Host ('          colors: the driver did not keep the change ('+$r+') - open GPU PANEL button and set Digital Vibrance 70%') -ForegroundColor Red }
    }
  }
}catch{ if($mode -ne 'REAPPLY'){ Write-Host '          colors: could not talk to the NVIDIA driver - use GPU PANEL button' -ForegroundColor Red } }
#END#
#BROWSERDL#
$mode=$env:BRMODE; $bk="$env:SystemDrive\Bu7maid_Backup"; New-Item -ItemType Directory -Path $bk -Force | Out-Null
$store="$bk\browser_downloads.txt"
try{ $dl=(New-Object -ComObject Shell.Application).NameSpace('shell:Downloads').Self.Path }catch{ $dl=$null }
if(-not $dl){ $dl=Join-Path $env:USERPROFILE 'Downloads' }
if(-not (Test-Path $dl)){ New-Item -ItemType Directory -Path $dl -Force | Out-Null }
Add-Type -AssemblyName System.Web.Extensions
$js=New-Object System.Web.Script.Serialization.JavaScriptSerializer; $js.MaxJsonLength=[int]::MaxValue; $js.RecursionLimit=300
$utf8=New-Object System.Text.UTF8Encoding($false)
function Sub($o,$k){ if(-not $o.ContainsKey($k) -or $o[$k] -isnot [System.Collections.IDictionary]){ $o[$k]=New-Object 'System.Collections.Generic.Dictionary[string,object]' }; return $o[$k] }
function Fix-Prefs($file,$name){
  try{
    $o=$js.DeserializeObject([IO.File]::ReadAllText($file))
    $d=Sub $o 'download'; $s=Sub $o 'savefile'
    if($mode -eq 'APPLY'){
      $known=@(); if(Test-Path $store){ $known=Get-Content $store | ForEach-Object { ($_ -split '\|')[0] } }
      if($known -notcontains $file){
        $a=if($d.ContainsKey('default_directory')){$d['default_directory']}else{'<none>'}; $b=if($d.ContainsKey('prompt_for_download')){([string]$d['prompt_for_download']).ToLower()}else{'<none>'}; $c=if($s.ContainsKey('default_directory')){$s['default_directory']}else{'<none>'}
        Add-Content -Path $store -Value ($file+'|'+$a+'|'+$b+'|'+$c)
      }
      $d['default_directory']=$dl; $d['prompt_for_download']=$false; $d['directory_upgrade']=$true; $s['default_directory']=$dl
    } else {
      $line=Get-Content $store -EA SilentlyContinue | Where-Object { $_.StartsWith($file+'|') } | Select-Object -First 1
      if(-not $line){ return $false }
      $p=$line -split '\|'
      if($p[1] -eq '<none>'){ [void]$d.Remove('default_directory') } else { $d['default_directory']=$p[1] }
      if($p[2] -eq '<none>'){ [void]$d.Remove('prompt_for_download') } else { $d['prompt_for_download']=($p[2] -eq 'true') }
      if($p[3] -eq '<none>'){ [void]$s.Remove('default_directory') } else { $s['default_directory']=$p[3] }
    }
    [IO.File]::WriteAllText($file,$js.Serialize($o),$utf8); return $true
  }catch{ return $false }
}
$browsers=@(
  @{n='Chrome';p='chrome';d="$env:LOCALAPPDATA\Google\Chrome\User Data"},
  @{n='Edge';p='msedge';d="$env:LOCALAPPDATA\Microsoft\Edge\User Data"},
  @{n='Brave';p='brave';d="$env:LOCALAPPDATA\BraveSoftware\Brave-Browser\User Data"},
  @{n='Vivaldi';p='vivaldi';d="$env:LOCALAPPDATA\Vivaldi\User Data"},
  @{n='Opera';p='opera';d="$env:APPDATA\Opera Software\Opera Stable";flat=$true},
  @{n='Opera GX';p='opera';d="$env:APPDATA\Opera Software\Opera GX Stable";flat=$true})
$done=@(); $skipped=@()
foreach($b in $browsers){
  if(-not (Test-Path $b.d)){ continue }
  $files=@(); if($b.flat){ $files=@(Join-Path $b.d 'Preferences') } else { $files=@(Get-ChildItem $b.d -Directory -EA SilentlyContinue | Where-Object { $_.Name -eq 'Default' -or $_.Name -like 'Profile *' } | ForEach-Object { Join-Path $_.FullName 'Preferences' }) }
  $files=@($files | Where-Object { Test-Path $_ })
  if($files.Count -eq 0){ continue }
  if(Get-Process -Name $b.p -EA SilentlyContinue){ $skipped+=$b.n; continue }
  $ok=$false; foreach($f in $files){ if(Fix-Prefs $f $b.n){ $ok=$true } }
  if($ok){ $done+=$b.n }
}
# Firefox: user.js is read at every start, works even while it is open
$ffRoot="$env:APPDATA\Mozilla\Firefox\Profiles"
if(Test-Path $ffRoot){
  foreach($pf in Get-ChildItem $ffRoot -Directory -EA SilentlyContinue){
    $uj=Join-Path $pf.FullName 'user.js'
    $old=@(); if(Test-Path $uj){ $old=@(Get-Content $uj | Where-Object { $_ -notmatch 'Bu7maid-dl' }) }
    if($mode -eq 'APPLY'){
      $esc=$dl.Replace('\','\\')
      $new=$old+@('user_pref("browser.download.folderList", 2); // Bu7maid-dl','user_pref("browser.download.dir", "'+$esc+'"); // Bu7maid-dl','user_pref("browser.download.useDownloadDir", true); // Bu7maid-dl')
      [IO.File]::WriteAllLines($uj,$new,$utf8)
    } elseif(Test-Path $uj){ if($old.Count -gt 0){ [IO.File]::WriteAllLines($uj,$old,$utf8) } else { Remove-Item $uj -Force -EA SilentlyContinue } }
    if($pf -eq (Get-ChildItem $ffRoot -Directory | Select-Object -First 1)){ if($mode -eq 'APPLY'){ $done+='Firefox' } }
  }
}
if($mode -eq 'APPLY'){
  if($done.Count -gt 0){ Write-Host ('          browsers: downloads go straight to '+$dl+' in '+($done -join ', ')+' (no "ask where to save")') }
  if($skipped.Count -gt 0){ Write-Host ('          browsers: '+($skipped -join ', ')+' is open - close it and press BOOST again to set its download folder') }
  if($done.Count -eq 0 -and $skipped.Count -eq 0){ Write-Host '          browsers: none found' }
}
#END#
#STARTALL#
$ErrorActionPreference='SilentlyContinue'
$keep='ctfmon|Discord|Bu7maid'
$bkd="$env:SystemDrive\Bu7maid_Backup"; New-Item $bkd -ItemType Directory -Force | Out-Null
$del=Test-Path (Join-Path $env:ProgramData 'Bu7maidZero\start_del.flag')
$list=Join-Path $bkd 'startup_deleted.txt'; $fold=Join-Path $bkd 'startup_items'; $off2=Join-Path $bkd 'startup_off2.txt'
$saved=@(); if(Test-Path $list){ $saved=@(Get-Content $list) }
$o2=@(); if(Test-Path $off2){ $o2=@(Get-Content $off2) }
$dis=[byte[]](3,0,0,0,0,0,0,0,0,0,0,0); $n=0
function DisableEntry($appr,$name){
  if(-not (Test-Path $appr)){ New-Item -Path $appr -Force | Out-Null }
  $cur=(Get-ItemProperty -Path $appr -Name $name -EA SilentlyContinue).$name
  if($cur -and (($cur[0] -band 1) -eq 1)){ return $false }
  Set-ItemProperty -Path $appr -Name $name -Value $dis -Type Binary
  $script:o2+=("{0}|{1}" -f $appr,$name); return $true
}
$runs=@(@('HKCU:\Software\Microsoft\Windows\CurrentVersion\Run','HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run'),
        @('HKLM:\Software\Microsoft\Windows\CurrentVersion\Run','HKLM:\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run'),
        @('HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run','HKLM:\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run32'))
foreach($r in $runs){
  $k=Get-Item -Path $r[0] -EA SilentlyContinue; if(-not $k){continue}
  foreach($name in $k.GetValueNames()){
    if($name -eq '' -or $name -match $keep){ continue }
    if($del){
      $val=[string]$k.GetValue($name,$null,'DoNotExpandEnvironmentNames'); $kind=[string]$k.GetValueKind($name)
      $saved+=("RUN|{0}|{1}|{2}|{3}" -f $r[0],$name,$kind,$val)
      Remove-ItemProperty -Path $r[0] -Name $name -EA SilentlyContinue; $n++
    } elseif(DisableEntry $r[1] $name){ $n++ }
  }
}
$dirs=@(@("$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup",'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder'),
        @("$env:ProgramData\Microsoft\Windows\Start Menu\Programs\StartUp",'HKLM:\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder'))
foreach($d in $dirs){
  if(-not (Test-Path $d[0])){continue}
  foreach($f in Get-ChildItem $d[0] -File -EA SilentlyContinue){
    if($f.Name -match $keep -or $f.Name -eq 'desktop.ini'){ continue }
    if($del){
      New-Item $fold -ItemType Directory -Force | Out-Null
      $dest=Join-Path $fold ([guid]::NewGuid().ToString('N').Substring(0,8)+'_'+$f.Name)
      Move-Item -LiteralPath $f.FullName -Destination $dest -Force -EA SilentlyContinue
      if(Test-Path $dest){ $saved+=("FILE|{0}|{1}" -f $dest,$f.FullName); $n++ }
    } elseif(DisableEntry $d[1] $f.Name){ $n++ }
  }
}
if($saved.Count){ $saved | Set-Content -Path $list -Encoding UTF8 }
if($o2.Count){ $o2 | Set-Content -Path $off2 -Encoding UTF8 }
if($del){ Write-Host ("          startup: DELETED {0} auto-start entries (only Discord and the keyboard service stay; RESTORE puts them back)" -f $n) -ForegroundColor DarkGray }
else { Write-Host ("          startup: turned OFF {0} auto-start entries (only Discord and the keyboard service stay; nothing deleted; RESTORE turns them back on)" -f $n) -ForegroundColor DarkGray }
#END#

#STARTBACK#
$ErrorActionPreference='SilentlyContinue'
$list="$env:SystemDrive\Bu7maid_Backup\startup_deleted.txt"
if(Test-Path $list){
  foreach($l in Get-Content $list){
    $p=$l.Split('|',5)
    if($p[0] -eq 'RUN' -and $p.Count -ge 5){
      if(-not (Test-Path $p[1])){ New-Item -Path $p[1] -Force | Out-Null }
      $t='String'; if($p[3] -eq 'ExpandString'){ $t='ExpandString' }
      New-ItemProperty -Path $p[1] -Name $p[2] -Value $p[4] -PropertyType $t -Force | Out-Null
    } elseif($p[0] -eq 'FILE' -and $p.Count -ge 3){
      if(Test-Path -LiteralPath $p[1]){ $dd=Split-Path $p[2]; New-Item $dd -ItemType Directory -Force | Out-Null; Move-Item -LiteralPath $p[1] -Destination $p[2] -Force }
    }
  }
  Remove-Item $list -Force
}
$off2="$env:SystemDrive\Bu7maid_Backup\startup_off2.txt"
if(Test-Path $off2){ foreach($l in Get-Content $off2){ $a=$l.Split('|',2); if($a.Count -eq 2){ Set-ItemProperty -Path $a[0] -Name $a[1] -Value ([byte[]](2,0,0,0,0,0,0,0,0,0,0,0)) -Type Binary -EA SilentlyContinue } }; Remove-Item $off2 -Force }
#END#

