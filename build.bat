@echo off
echo Compiling BatteryMonitor.cs...
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /out:BatteryMonitor.exe BatteryMonitor.cs
if %errorlevel% equ 0 (
    echo Compilation Succeeded!
    echo Created BatteryMonitor.exe
) else (
    echo Compilation Failed!
)
pause
