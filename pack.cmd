@echo off
set BUILD_CFG=%1
if "%BUILD_CFG%" equ "" set BUILD_CFG=Debug
if exist "package-output" del "package-output" /f /q
dotnet pack src\Dataline.Dsrv.KomServer.Vsnr -c %BUILD_CFG% -o "package-output" --no-build --version-suffix "%2"
if %errorlevel% neq  0 exit /b %errorlevel%
