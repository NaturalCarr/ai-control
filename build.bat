@echo off
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set SRC=%~dp0src\ai-control.cs
set OUT=%~dp0ai-control.exe
set ICON=%~dp0ai-control.ico
set ICON_EXPORT=%TEMP%\ai-control-icon-%RANDOM%.exe

echo Generating excavator app icon ...
"%CSC%" /target:winexe /out:"%ICON_EXPORT%" "%SRC%" ^
    /reference:System.Windows.Forms.dll ^
    /reference:System.Drawing.dll ^
    /reference:System.dll ^
    /nologo
if errorlevel 1 goto failed

"%ICON_EXPORT%" --export-icon "%ICON%" "%~dp0src\excavator.png"
if errorlevel 1 goto failed
del /q "%ICON_EXPORT%"

echo Building ai-control.exe ...
"%CSC%" /target:winexe /win32icon:"%ICON%" /out:"%OUT%" "%SRC%" ^
    /reference:System.Windows.Forms.dll ^
    /reference:System.Drawing.dll ^
    /reference:System.dll ^
    /nologo

if %ERRORLEVEL% == 0 (
    echo Done: %OUT%
) else (
    goto failed
)
goto end

:failed
if exist "%ICON_EXPORT%" del /q "%ICON_EXPORT%"
echo Build failed.
pause
:end
