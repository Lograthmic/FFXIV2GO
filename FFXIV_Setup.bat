@echo off
setlocal EnableDelayedExpansion

:: 请求管理员权限
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"
if '%errorlevel%' NEQ '0' (
    echo 请求管理员权限...
    goto UACPrompt
)
goto gotAdmin

:UACPrompt
echo Set UAC = CreateObject^("Shell.Application"^) > "%temp%\getadmin.vbs"
echo UAC.ShellExecute "%~s0", "", "", "runas", 1 >> "%temp%\getadmin.vbs"
"%temp%\getadmin.vbs"
exit /B

:gotAdmin
if exist "%temp%\getadmin.vbs" ( del "%temp%\getadmin.vbs" )
pushd "%CD%"
CD /D "%~dp0"

echo ========================================
echo        FFXIV环境配置脚本
echo ========================================
echo.

:: 步骤1：检测当前脚本所在盘符
set "CURRENT_DRIVE=%~d0"
echo [步骤1] 检测到当前脚本所在盘符: %CURRENT_DRIVE%
echo.

:: 步骤2：检测config.ini是否存在PATH_FFXIV
set "FFXIV_PATH="
if exist "config.ini" (
    echo [步骤2] 检测到config.ini文件，读取FFXIV路径...
    for /f "tokens=2 delims==" %%i in ('findstr "PATH_FFXIV" config.ini') do (
        set "FFXIV_PATH=%%i"
    )
)

if "!FFXIV_PATH!"=="" (
    echo [步骤2] 未找到FFXIV路径配置，请手动输入...
    set /p "FFXIV_PATH=请输入FFXIV安装路径（例如 D:\FFXIV）: "
    echo PATH_FFXIV=!FFXIV_PATH!>config.ini
    echo [步骤2] 已创建config.ini文件
)

echo [步骤2] FFXIV路径: !FFXIV_PATH!
echo.

:: 确认操作
echo 即将开始配置FFXIV环境，请确认以上信息正确...
echo 按Enter键继续，或按Ctrl+C取消...
pause >nul
echo.

:: 步骤3：检测FFXIV路径是否正确
set "GAME_FOLDER=!FFXIV_PATH!\game\My Games\FINAL FANTASY XIV - A Realm Reborn"
if not exist "!GAME_FOLDER!\" (
    echo [错误] 在指定路径下未找到FFXIV游戏文件夹!
    echo 请检查路径是否正确: !FFXIV_PATH!
    pause
    exit /b 1
)
echo [步骤3] FFXIV游戏文件夹验证成功
echo.

:: 步骤4：重命名原文件夹
if exist "!GAME_FOLDER!.old\" (
    echo [步骤4] 发现已存在的.old备份文件夹，正在删除...
    rmdir /s /q "!GAME_FOLDER!.old"
)
echo [步骤4] 正在备份原文件夹...
ren "!GAME_FOLDER!" "FINAL FANTASY XIV - A Realm Reborn.old"
if errorlevel 1 (
    echo [错误] 重命名原文件夹失败!
    pause
    exit /b 1
)
echo [步骤4] 原文件夹备份完成
echo.

:: 步骤5：创建硬链接
echo [步骤5] 正在创建游戏配置硬链接...
mklink /J "!GAME_FOLDER!" "!CURRENT_DRIVE!\pre\FINAL FANTASY XIV - A Realm Reborn"
if errorlevel 1 (
    echo [错误] 创建游戏配置硬链接失败!
    echo 正在恢复原文件夹...
    ren "!GAME_FOLDER!.old" "FINAL FANTASY XIV - A Realm Reborn"
    pause
    exit /b 1
)
echo [步骤5] 游戏配置硬链接创建成功
echo.

:: 步骤6：备份XIV启动器配置
set "XIV_LAUNCHER=%appdata%\XIVLauncherCN"
if exist "!XIV_LAUNCHER!\" (
    echo [步骤6] 正在备份XIV启动器配置...
    if exist "!XIV_LAUNCHER!.old\" (
        rmdir /s /q "!XIV_LAUNCHER!.old"
    )
    ren "!XIV_LAUNCHER!" "XIVLauncherCN.old"
    echo [步骤6] XIV启动器配置备份完成
)
echo.

:: 步骤7：创建XIV启动器硬链接
echo [步骤7] 正在创建XIV启动器硬链接...
mklink /J "!XIV_LAUNCHER!" "!CURRENT_DRIVE!\pre\XIVLauncherCN"
if errorlevel 1 (
    echo [错误] 创建XIV启动器硬链接失败!
    pause
    exit /b 1
)
echo [步骤7] XIV启动器硬链接创建成功
echo.

:: 步骤8：创建MareSynchronos桌面文件夹
echo [步骤8] 正在创建MareSynchronos桌面文件夹...
set "DESKTOP=%userprofile%\Desktop"
if not exist "!DESKTOP!\" set "DESKTOP=%userprofile%\OneDrive\Desktop"
if not exist "!DESKTOP!\" set "DESKTOP=%userprofile%\OneDrive\桌面"
mkdir "!DESKTOP!\MareSynchronos" 2>nul
echo [步骤8] MareSynchronos桌面文件夹创建完成
echo.

:: 步骤9-10：安装运行库
echo [步骤9] 正在安装VC++运行库...
start /wait "安装VC++运行库" "!CURRENT_DRIVE!\pre\VC_redist.x64.exe" /install /quiet /norestart
echo [步骤9] VC++运行库安装完成
echo.

echo [步骤10] 正在安装.NET运行库...
start /wait "安装.NET运行库" "!CURRENT_DRIVE!\pre\windowsdesktop-runtime-8.0.21-win-x64.exe" /install /quiet /norestart
echo [步骤10] .NET运行库安装完成
echo.

:: 完成
echo ========================================
echo        FFXIV环境配置完成！
echo ========================================
echo.
echo 所有操作已完成，按任意键退出...
pause >nul
