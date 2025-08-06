@echo off
setlocal

REM =================================================================
REM                  �ű� 2: ���WPFӦ���°汾
REM =================================================================
REM ˵��: 
REM   ÿ�η����°汾ʱ�������޸�����ġ��������򡿣�Ȼ�����д˽ű���
REM   ����ǰ����ȷ���Ѿ�ִ�й���1-����������.bat���������˸������ļ���
REM =================================================================

REM --- �������� ---

REM �������WPFӦ�÷��������ڵ�Ŀ¼
SET "AppPublishDir=AutoHPMA"

REM ���ü����������°汾��
SET "AppVersion=3.3.0"

REM �������Ӧ��ID (��Ҫ�� kachina.config.json �е� regName һ��)
SET "AppId=AutoHPMA"

REM ���ø��³�����ļ��� (��Ҫ�� kachina.config.json �е� updaterName һ��)
SET "UpdaterName=AutoHPMA.update.exe"

REM �����������ɵ����߰�װ���ļ���
SET "InstallerName=AutoHPMA.Install.exe"

REM --- ����������� ---


REM =================================================================
REM                  �ű�ִ������ (ͨ�������޸�)
REM =================================================================

echo.
echo ========================================================
echo           ��ʼ�����°汾: %AppVersion%
echo ========================================================
echo.

REM ��������ļ�
if not exist kachina-builder.exe (echo [����] δ�ҵ� kachina-builder.exe�� & goto end)
if not exist kachina.config.json (echo [����] δ�ҵ� kachina.config.json�� & goto end)
if not exist "%UpdaterName%" (echo [����] δ�ҵ������� %UpdaterName% ���������С�1-����������.bat���� & goto end)
if not exist "%AppPublishDir%" (echo [����] Ӧ�÷���Ŀ¼������: %AppPublishDir% & goto end)


echo [���� 1/2] ����Ϊ�汾 %AppVersion% ����Ԫ���ݺ�Ӧ���ļ�...
kachina-builder.exe gen -j 8 -i "%AppPublishDir%" -m metadata.json -o hashed -r %AppId% -t %AppVersion% -u %UpdaterName%
IF %ERRORLEVEL% NEQ 0 (echo [����] ����Ԫ����ʧ�ܣ� & goto end)

echo.
echo [���� 2/2] ���ڴ���������յ����߰�װ��...
kachina-builder.exe pack -c kachina.config.json -m metadata.json -d hashed -o %InstallerName%
IF %ERRORLEVEL% NEQ 0 (echo [����] ������߰�װ��ʧ�ܣ� & goto end)

echo.
echo ========================================================
echo           �汾 %AppVersion% �����ɹ���
echo ========================================================
echo.
echo   ���߰�װ��: %InstallerName%
echo   �뽫���ϴ������� config �ļ���ָ���� source URL��
echo.

:end
pause