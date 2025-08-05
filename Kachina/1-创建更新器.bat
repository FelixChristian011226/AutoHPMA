@echo off
setlocal

REM =================================================================
REM                  �ű� 1: ����������
REM =================================================================
REM ˵��: 
REM   �˽ű����� kachina.config.json �ļ����ɸ��³���
REM   ͨ��ֻ������Ŀ��ʼʱ����һ�Σ������޸��� config �ļ����������С�
REM =================================================================

REM --- �������� ---

REM ���ø��³����������ɵ��ļ��� (��Ҫ�� kachina.config.json �е� updaterName һ��)
SET "UpdaterName=AutoHPMA.update.exe"

REM --- ����������� ---

echo.
echo [��ʼ��] ���ڸ��� kachina.config.json ����������...
echo.

REM ��� kachina-builder �Ƿ����
if not exist kachina-builder.exe (
    echo [����] δ�ڵ�ǰĿ¼�ҵ� kachina-builder.exe��
    goto end
)

REM ��������ļ��Ƿ����
if not exist kachina.config.json (
    echo [����] δ�ڵ�ǰĿ¼�ҵ� kachina.config.json��
    goto end
)

kachina-builder.exe pack -c kachina.config.json -o %UpdaterName%

IF %ERRORLEVEL% NEQ 0 (
    echo [����] ����������ʧ�ܣ����� kachina.config.json �����á�
    goto end
)

echo.
echo ========================================================
echo           ������ (%UpdaterName%) �����ɹ���
echo ========================================================
echo.
echo   ���������Խ�����һ����������ľ���Ӧ�ð汾�ˡ�
echo.

:end
pause