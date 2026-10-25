@echo off
setlocal EnableExtensions
cd /d "%~dp0"
set "DOTNET_EXE="

rem 1. Try dotnet from PATH.
where dotnet.exe >nul 2>&1
if not errorlevel 1 set "DOTNET_EXE=dotnet.exe"

rem 2. Try standard SDK locations, even if PATH is missing.
if not defined DOTNET_EXE if exist "%ProgramFiles%\dotnet\dotnet.exe" set "DOTNET_EXE=%ProgramFiles%\dotnet\dotnet.exe"
if not defined DOTNET_EXE if defined ProgramW6432 if exist "%ProgramW6432%\dotnet\dotnet.exe" set "DOTNET_EXE=%ProgramW6432%\dotnet\dotnet.exe"
if not defined DOTNET_EXE if exist "%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe" set "DOTNET_EXE=%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe"
if not defined DOTNET_EXE goto NO_DOTNET

rem 3. A runtime alone is not enough: the SDK is required.
"%DOTNET_EXE%" --list-sdks | findstr /R /C:"^[0-9]" >nul
if errorlevel 1 goto NO_SDK

echo Using: "%DOTNET_EXE%"
"%DOTNET_EXE%" --version
if errorlevel 1 goto NO_SDK

echo.
echo Publishing Timerki...
"%DOTNET_EXE%" publish "Timerki\Timerki.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o "gotowy-exe"
if errorlevel 1 goto BUILD_FAILED

echo.
if not exist "gotowy-exe\Sounds" mkdir "gotowy-exe\Sounds"
echo SUCCESS: "%~dp0gotowy-exe\Timerki.exe"
echo Portable settings and audio will be saved next to EXE.
pause
exit /b 0

:NO_DOTNET
echo.
echo ERROR: dotnet.exe was not found in PATH or the standard folders.
echo Install the .NET 8 SDK for Windows x64 (not only Runtime):
echo https://dotnet.microsoft.com/download/dotnet/8.0
goto END_ERROR

:NO_SDK
echo.
echo ERROR: dotnet.exe is installed, but no working .NET SDK was found.
echo Install .NET 8 SDK for Windows x64:
echo https://dotnet.microsoft.com/download/dotnet/8.0
goto END_ERROR

:BUILD_FAILED
echo.
echo ERROR: compilation failed. See the build errors above.
:END_ERROR
pause
exit /b 1
