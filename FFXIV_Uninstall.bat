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
echo        FFXIV环境卸载脚本
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
)

echo [步骤2] FFXIV路径: !FFXIV_PATH!
echo.

:: 确认操作
echo 即将开始卸载FFXIV环境，请确认以上信息正确...
echo 按Enter键继续，或按Ctrl+C取消...
pause >nul
echo.

:: 步骤3：检测硬链接文件夹是否存在
set "GAME_FOLDER=!FFXIV_PATH!\game\My Games\FINAL FANTASY XIV - A Realm Reborn"
set "BACKUP_FOLDER=!FFXIV_PATH!\game\My Games\FINAL FANTASY XIV - A Realm Reborn.old"

if not exist "!GAME_FOLDER!\" (
    echo [错误] 在指定路径下未找到FFXIV游戏硬链接文件夹!
    echo 请检查路径是否正确: !FFXIV_PATH!
    pause
    exit /b 1
)
echo [步骤3] FFXIV游戏硬链接文件夹验证成功
echo.

:: 步骤4：解除硬链接
echo [步骤4] 正在解除游戏配置硬链接...
rmdir "!GAME_FOLDER!"
if errorlevel 1 (
    echo [错误] 解除游戏配置硬链接失败!
    pause
    exit /b 1
)
echo [步骤4] 游戏配置硬链接解除成功
echo.

:: 步骤5：恢复原文件夹
echo [步骤5] 正在恢复原游戏配置文件夹...
if exist "!BACKUP_FOLDER!\" (
    ren "!BACKUP_FOLDER!" "FINAL FANTASY XIV - A Realm Reborn"
    echo [步骤5] 原游戏配置文件夹恢复完成
) else (
    echo [警告] 未找到备份文件夹，跳过恢复
)
echo.

:: 步骤6：解除XIV启动器硬链接
set "XIV_LAUNCHER=%appdata%\XIVLauncherCN"
echo [步骤6] 正在解除XIV启动器硬链接...
if exist "!XIV_LAUNCHER!\" (
    rmdir "!XIV_LAUNCHER!"
    echo [步骤6] XIV启动器硬链接解除成功
) else (
    echo [步骤6] XIV启动器硬链接不存在，跳过解除
)
echo.

:: 步骤7：恢复XIV启动器配置
set "XIV_BACKUP=%appdata%\XIVLauncherCN.old"
if exist "!XIV_BACKUP!\" (
    echo [步骤7] 正在恢复XIV启动器配置...
    ren "!XIV_BACKUP!" "XIVLauncherCN"
    echo [步骤7] XIV启动器配置恢复完成
) else (
    echo [步骤7] 未找到XIV启动器备份配置，跳过恢复
)
echo.

:: 步骤8：删除MareSynchronos桌面文件夹
echo [步骤8] 正在删除MareSynchronos桌面文件夹...
set "DESKTOP=%userprofile%\Desktop"
if not exist "!DESKTOP!\" set "DESKTOP=%userprofile%\OneDrive\Desktop"
if not exist "!DESKTOP!\" set "DESKTOP=%userprofile%\OneDrive\桌面"
if exist "!DESKTOP!\MareSynchronos\" (
    rmdir /s /q "!DESKTOP!\MareSynchronos"
    echo [步骤8] MareSynchronos桌面文件夹删除完成
) else (
    echo [步骤8] MareSynchronos桌面文件夹不存在，跳过删除
)
echo.

:: 完成
echo ========================================
echo        FFXIV环境卸载完成！
echo ========================================
echo.
echo 所有操作已完成，按任意键退出...
pause >nul
