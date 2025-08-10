@echo off
setlocal

REM =================================================================
REM                  脚本 1: 创建更新器
REM =================================================================
REM 说明: 
REM   此脚本根据 kachina.config.json 文件生成更新程序。
REM   通常只需在项目开始时运行一次，或在修改了 config 文件后重新运行。
REM =================================================================

REM --- 配置区域 ---

REM 设置更新程序最终生成的文件名 (需要和 kachina.config.json 中的 updaterName 一致)
SET "UpdaterName=AutoHPMA.update.exe"

REM --- 配置区域结束 ---

echo.
echo [初始化] 正在根据 kachina.config.json 创建更新器...
echo.

REM 检查 kachina-builder 是否存在
if not exist kachina-builder.exe (
    echo [错误] 未在当前目录找到 kachina-builder.exe。
    goto end
)

REM 检查配置文件是否存在
if not exist kachina.config.json (
    echo [错误] 未在当前目录找到 kachina.config.json。
    goto end
)

kachina-builder.exe pack -c kachina.config.json -o %UpdaterName%

IF %ERRORLEVEL% NEQ 0 (
    echo [错误] 创建更新器失败！请检查 kachina.config.json 的配置。
    goto end
)

echo.
echo ========================================================
echo           更新器 (%UpdaterName%) 创建成功！
echo ========================================================
echo.
echo   现在您可以进行下一步，打包您的具体应用版本了。
echo.

:end
pause