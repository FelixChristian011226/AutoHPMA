@echo off
setlocal

REM =================================================================
REM                  �ű� 2: ���WPFӦ���°汾
REM =================================================================
REM ˵��: 
REM   �˽ű�֧������ģʽ:
REM   1. �Զ��� (GitHub Actions): ͨ�������в�������汾�š�
REM      (��: 2-installer.bat 1.2.3)
REM   2. �ֶ�ģʽ (����): ���δ�������������ʾ�ֶ�����汾�š�
REM =================================================================

REM --- �������� ---

REM [!!!] �汾�Ż�ȡ�߼�
REM ���ȴ������е�һ������ (%1) ��ȡ�汾��
SET "AppVersion=%1"

REM ��������в���Ϊ�գ�����ʾ�û��ֶ�����
if "%AppVersion%"=="" (
    echo.
    echo [��Ϣ] δ��⵽�����д���İ汾�ţ������ֶ�ģʽ��
    set /p AppVersion=������Ҫ����İ汾��: 
)

REM �����汾���Ƿ�Ϊ�� (��ֹ�û�ֱ�Ӱ��س�)
if "%AppVersion%"=="" (
    echo [����] �汾�Ų���Ϊ�գ���������ֹ��
    goto end
)
echo [��Ϣ] ��ȷ���汾��Ϊ: %AppVersion%
echo.


REM �������WPFӦ�÷��������ڵ�Ŀ¼
SET "AppPublishDir=publish"

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