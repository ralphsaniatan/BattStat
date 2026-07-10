@echo off
echo Compiling BatteryMonitor.cs...
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /out:BattStat.exe BatteryMonitor.cs
if %errorlevel% equ 0 (
    echo Compilation Succeeded!
    echo Created BattStat.exe
) else (
    echo Compilation Failed!
)
pause
