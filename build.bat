@echo off
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set SRC=%~dp0ai-control.cs
set OUT=%~dp0ai-control.exe

echo Building ai-control.exe ...
"%CSC%" /target:winexe /out:"%OUT%" "%SRC%" ^
    /reference:System.Windows.Forms.dll ^
    /reference:System.Drawing.dll ^
    /reference:System.dll ^
    /nologo

if %ERRORLEVEL% == 0 (
    echo Done: %OUT%
) else (
    echo Build failed.
    pause
)
