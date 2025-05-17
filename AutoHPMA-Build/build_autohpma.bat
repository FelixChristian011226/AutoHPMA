@echo off
cd /d %~dp0

@echo [Preparing]
del AutoHPMA-Setup.exe
copy /y publish.7z .\MicaSetup\Resources\Setups\publish.7z

@echo [Build Uninstall File]
for /f "usebackq tokens=*" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -property installationPath`) do set "path=%path%;%%i\MSBuild\Current\Bin;%%i\Common7\IDE"
msbuild MicaSetup\MicaSetup.Uninst.csproj /t:Rebuild /p:Configuration=Release /p:DeployOnBuild=true /p:PublishProfile=FolderProfile /restore

@echo [Copy Uninstall File]
copy /y .\MicaSetup\bin\Release\MicaSetup.exe .\MicaSetup\Resources\Setups\Uninst.exe

@echo [Build Setup File]
msbuild MicaSetup\MicaSetup.csproj /t:Build /p:Configuration=Release /p:DeployOnBuild=true /p:PublishProfile=FolderProfile /restore

@echo [Success]
copy /y .\MicaSetup\bin\Release\MicaSetup.exe .\AutoHPMA-Setup.exe

@echo [Build Success] 

@pause