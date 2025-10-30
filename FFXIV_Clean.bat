@echo off
setlocal enabledelayedexpansion

echo ========================================
echo           文件清理脚本
echo ========================================

set "SCRIPT_DRIVE=%~d0"
echo 检测到脚本所在盘符: %SCRIPT_DRIVE%
echo.

echo 正在扫描文件...
set "total_files=0"
set "total_size=0"

for /r "%SCRIPT_DRIVE%\" %%f in (*.log *.old) do (
    if exist "%%f" (
        set /a "total_files+=1"
        for %%s in ("%%f") do (
            set /a "total_size+=%%~zs"
        )
    )
)

call :ConvertSize !total_size! readable_size
echo.
echo 找到 !total_files! 个文件，总大小: !readable_size!
echo.

if !total_files! equ 0 (
    echo 没有找到需要清理的文件。
    pause
    exit /b
)

set /p "confirm=请按Enter键开始删除，或按Ctrl+C取消..."
echo.

echo 开始删除文件...
echo.

set "deleted_files=0"
set "freed_size=0"

for /r "%SCRIPT_DRIVE%\" %%f in (*.log *.old) do (
    if exist "%%f" (
        for %%s in ("%%f") do (
            set /a "freed_size+=%%~zs"
        )
        echo 删除: %%f
        del "%%f"
        set /a "deleted_files+=1"
    )
)

call :ConvertSize !freed_size! freed_readable

echo.
echo ========================================
echo 清理完成！
echo 共删除 !deleted_files! 个文件
echo 释放空间: !freed_readable!
echo ========================================

pause
exit /b

:: 简洁的大小转换函数
:ConvertSize
setlocal
set "size=%1"
set "unit=B"

if %size% gtr 1073741824 (
    set /a "size=size/1073741824"
    set "unit=GB"
) else if %size% gtr 1048576 (
    set /a "size=size/1048576"
    set "unit=MB"
) else if %size% gtr 1024 (
    set /a "size=size/1024"
    set "unit=KB"
)

endlocal & set "%2=%size% %unit%"
exit /b
