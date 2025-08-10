@echo off
setlocal

REM =================================================================
REM                  脚本 2: 打包WPF应用新版本
REM =================================================================
REM 说明: 
REM   此脚本现在会自动从 GitHub Actions 的环境变量中获取版本号。
REM   本地运行时，请先手动设置环境变量: set AppVersion=x.x.x
REM =================================================================

REM --- 配置区域 ---

REM 设置你的WPF应用发布后所在的目录
SET "AppPublishDir=publish"

REM [!!!] 版本号已移除，将从环境变量 %AppVersion% 中读取

REM 设置你的应用ID (需要和 kachina.config.json 中的 regName 一致)
SET "AppId=AutoHPMA"

REM 设置更新程序的文件名 (需要和 kachina.config.json 中的 updaterName 一致)
SET "UpdaterName=AutoHPMA.update.exe"

REM 设置最终生成的离线安装包文件名
SET "InstallerName=AutoHPMA.Install.exe"

REM --- 配置区域结束 ---


REM =================================================================
REM                  脚本执行区域
REM =================================================================

echo.
echo ========================================================
echo           开始构建新版本: %AppVersion%
echo ========================================================
echo.

REM 检查版本号是否已设置
if not defined AppVersion (echo [错误] 环境变量 AppVersion 未设置！ & goto end)

REM 检查依赖文件
if not exist kachina-builder.exe (echo [错误] 未找到 kachina-builder.exe。 & goto end)
if not exist kachina.config.json (echo [错误] 未找到 kachina.config.json。 & goto end)
if not exist "%UpdaterName%" (echo [错误] 未找到更新器 %UpdaterName% 。请先运行“1-创建更新器.bat”。 & goto end)
if not exist "%AppPublishDir%" (echo [错误] 应用发布目录不存在: %AppPublishDir% & goto end)


echo [步骤 1/2] 正在为版本 %AppVersion% 生成元数据和应用文件...
kachina-builder.exe gen -j 8 -i "%AppPublishDir%" -m metadata.json -o hashed -r %AppId% -t %AppVersion% -u %UpdaterName%
IF %ERRORLEVEL% NEQ 0 (echo [错误] 生成元数据失败！ & goto end)

echo.
echo [步骤 2/2] 正在打包生成最终的离线安装包...
kachina-builder.exe pack -c kachina.config.json -m metadata.json -d hashed -o %InstallerName%
IF %ERRORLEVEL% NEQ 0 (echo [错误] 打包离线安装包失败！ & goto end)

echo.
echo ========================================================
echo           版本 %AppVersion% 构建成功！
echo ========================================================
echo.
echo   离线安装包: %InstallerName%
echo   请将其上传到您在 config 文件中指定的 source URL。
echo.

:end
pause