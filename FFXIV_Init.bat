@echo off
setlocal EnableDelayedExpansion

:: 提权检查
@REM >nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"
@REM if '%errorlevel%' NEQ '0' (
@REM     echo 请求管理员权限...
@REM     goto UACPrompt
@REM ) else ( goto gotAdmin )

@REM :UACPrompt
@REM     echo Set UAC = CreateObject^("Shell.Application"^) > "%temp%\getadmin.vbs"
@REM     echo UAC.ShellExecute "%~s0", "", "", "runas", 1 >> "%temp%\getadmin.vbs"
@REM     "%temp%\getadmin.vbs"
@REM     exit /B

@REM :gotAdmin
@REM     if exist "%temp%\getadmin.vbs" ( del "%temp%\getadmin.vbs" )
@REM     pushd "%CD%"
@REM     CD /D "%~dp0"

:: 主脚本开始
echo ========================================
echo           环境检测和初始化脚本
echo ========================================
echo.

:: 1.1 检测是否在磁盘根目录
set "CURRENT_PATH=%~d0"
echo [步骤1] 检测到当前脚本所在路径: %CD%
if not "%CD%"=="%CURRENT_PATH%\" (
    echo 错误：脚本不在磁盘根目录运行！
    echo 请将脚本移动到磁盘根目录后重新运行。
    pause
    exit /B 1
)
echo 脚本在根目录运行，安装盘符: %CURRENT_PATH%
set "INSTALL_DRIVE=%CURRENT_PATH%"
echo.

:: 1.2 检测github连通性
echo [步骤2] 检测GitHub连通性
ping -n 1 github.com >nul 2>&1
if errorlevel 1 (
    echo 错误：无法连接到GitHub，请检查网络连接！
    pause
    exit /B 1
)
echo GitHub连通性检测通过！
echo.

:: 2. 创建pre文件夹
echo [步骤3] 复制配置文件
echo 正在创建pre文件夹...
if not exist "%INSTALL_DRIVE%\pre" mkdir "%INSTALL_DRIVE%\pre"
echo pre文件夹创建完成！
echo.

:: 3.1 复制游戏配置文件
:FFXIV_PATH
echo 请输入FFXIV安装路径（例如：C:\Program Files (x86)\FFXIV）：
set /p "PATH_FFXIV="
echo 您输入的路径是：%PATH_FFXIV%
echo 按Enter确认，按Ctrl+C取消...
pause >nul

:: 检测路径有效性
if not exist "%PATH_FFXIV%\game\My Games\FINAL FANTASY XIV - A Realm Reborn" (
    echo 错误：在指定路径下未找到游戏配置文件！
    echo 请检查路径是否正确。
    goto FFXIV_PATH
)

echo 正在复制游戏配置文件...
xcopy "%PATH_FFXIV%\game\My Games\FINAL FANTASY XIV - A Realm Reborn" "%INSTALL_DRIVE%\pre\FINAL FANTASY XIV - A Realm Reborn" /E /I /H /Y > nul
echo 游戏配置文件复制完成！
echo.

:: 3.2 复制XIVLauncherCN配置文件
if not exist "%appdata%\XIVLauncherCN" (
    echo 警告：在此台电脑上没有运行过XIVLauncherCN！
    echo 没有找到XIVLauncherCN的配置文件夹。
    choice /C YN /M "是否作为新环境继续运行？"
    if errorlevel 2 (
        echo 用户选择退出脚本。
        exit /B 0
    )
    echo 正在创建新的XIVLauncherCN配置文件夹...
    mkdir "%INSTALL_DRIVE%\pre\XIVLauncherCN"
    echo 新的XIVLauncherCN配置文件夹已创建！
    echo 跳过XIVLauncherCN配置和Penumbra配置复制。
    goto DOWNLOAD_DEPS
)

echo 正在复制XIVLauncherCN配置文件...
xcopy "%appdata%\XIVLauncherCN" "%INSTALL_DRIVE%\pre\XIVLauncherCN" /E /I /H /Y > nul
echo XIVLauncherCN配置文件复制完成！
echo.

echo 正在解析Penumbra配置文件...
set "MOD_PATH="

:: 使用PowerShell正确解析JSON
for /f "delims=" %%i in ('powershell -Command "$config = Get-Content '%INSTALL_DRIVE%\pre\XIVLauncherCN\pluginConfigs\Penumbra.json' | ConvertFrom-Json; $config.ModDirectory"') do (
    set "MOD_PATH=%%i"
)

if defined MOD_PATH (
    echo 找到Mod路径: %MOD_PATH%
) else (
    echo 警告：无法从Penumbra配置文件中解析出Mod路径
    goto DOWNLOAD_DEPS
)

echo 正在复制mod文件...
xcopy "!MOD_PATH!" "%INSTALL_DRIVE%\pre\mods" /E /I /H /Y > nul
echo mod文件复制完成！
echo.

:DOWNLOAD_DEPS
:: 3.4 下载依赖文件
echo [步骤4] 下载依赖文件
echo.

:: 3.4.1 下载VC++可再发行程序包
echo 正在下载Microsoft Visual C++ 可再发行程序包...
powershell -Command "Invoke-WebRequest -Uri 'https://aka.ms/vc14/vc_redist.x64.exe' -OutFile '%INSTALL_DRIVE%\pre\vc_redist.x64.exe'"
if exist "%INSTALL_DRIVE%\pre\vc_redist.x64.exe" (
    echo VC++可再发行程序包下载完成！
) else (
    echo 警告：VC++可再发行程序包下载失败！
)

:: 3.4.2 下载.NET 8.0 Desktop Runtime
echo 正在下载.NET 8.0 Desktop Runtime...
powershell -Command "Invoke-WebRequest -Uri 'https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/8.0.21/windowsdesktop-runtime-8.0.21-win-x64.exe' -OutFile '%INSTALL_DRIVE%\pre\windowsdesktop-runtime-8.0.21-win-x64.exe'"
if exist "%INSTALL_DRIVE%\pre\windowsdesktop-runtime-8.0.21-win-x64.exe" (
    echo .NET 8.0 Desktop Runtime下载完成！
) else (
    echo 警告：.NET 8.0 Desktop Runtime下载失败！
)
echo.

:: 3.5 下载可选应用
echo [步骤5] 下载应用程序
echo.

:: 3.5.1 下载FFXIVLauncher
echo 正在下载FFXIVLauncher...
powershell -Command "Invoke-WebRequest -Uri 'https://github.com/ottercorp/FFXIVQuickLauncher/releases/download/6.4.6-15/XIVLauncherCN-win-Portable.7z' -OutFile '%INSTALL_DRIVE%\XIVLauncherCN.7z'"
if exist "%INSTALL_DRIVE%\XIVLauncherCN.7z" (
    echo 正在解压FFXIVLauncher...
    mkdir "%INSTALL_DRIVE%\XIVLauncherCN"
    tar -xf "%INSTALL_DRIVE%\XIVLauncherCN.7z" -C "%INSTALL_DRIVE%\XIVLauncherCN"
    if errorlevel 1 (
        echo tar解压失败，尝试其他方法...
        goto SCHEME1
    )
    del "%INSTALL_DRIVE%\XIVLauncherCN.7z"
    echo FFXIVLauncher下载和解压完成！
) else (
    echo 警告：FFXIVLauncher下载失败！
)
echo.

:: 3.5.2 询问是否下载ACT
choice /C YN /M "是否下载ACT国服整合版？"
if errorlevel 2 (
    echo 跳过ACT下载。
    goto CLEANUP
)

echo 正在下载ACT国服整合版...
powershell -Command "Invoke-WebRequest -Uri 'https://cafemenu.xivcdn.com/act/update/download?channel=release&variant=sfx&version=latest' -OutFile '%INSTALL_DRIVE%\ACT_Installer.exe'"
if exist "%INSTALL_DRIVE%\ACT_Installer.exe" (
    mkdir "%INSTALL_DRIVE%\ACT" 2>nul
    echo ACT国服整合版下载完成！
    echo 请手动运行 %INSTALL_DRIVE%\ACT_Installer.exe 并安装到 %INSTALL_DRIVE%\ACT 文件夹
) else (
    echo 警告：ACT国服整合版下载失败！
)
echo.

:CLEANUP
:: 3.6 清理临时文件
choice /C YN /M "是否删除虚拟环境的log文件以释放U盘空间？"
if errorlevel 2 (
    echo 跳过log文件清理。
    goto FINISH
)

if exist "%INSTALL_DRIVE%\FFXIV_Clean.bat" (
    echo 正在运行清理脚本...
    call "%INSTALL_DRIVE%\FFXIV_Clean.bat"
    echo 清理完成！
) else (
    echo 警告：未找到清理脚本 FFXIV_Clean.bat
)

:FINISH
echo.
echo ========================================
echo           脚本执行完成！
echo ========================================
echo 安装盘符: %INSTALL_DRIVE%
echo 配置文件位置: %INSTALL_DRIVE%\pre\
echo.
echo 请按Enter键退出...
pause >nul