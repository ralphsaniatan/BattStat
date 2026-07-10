# BattStat 🔋✨

A lightweight, premium Windows system tray battery monitor that tracks up to three USB-HID and Bluetooth devices using concentric activity rings.

> **Why BattStat?**
> This application was created out of pure frustration with having to install, launch, and run multiple bloated, resource-heavy manufacturer companion apps (like SteelSeries GG, VGN Hub, proprietary mouse software, etc.) just to check if your wireless peripherals need charging. BattStat provides a single, unified, ultra-lightweight tray interface with zero bloat.

---

## Features 🚀

- **Concentric Tray Activity Rings**: Monitors three devices simultaneously in the system tray using Apple Watch-style rings (Outer, Middle, Inner).
- **Universal Protocol Detection**:
  - **USB-HID Devices**: Sends raw feature report queries natively to identify battery status (includes pre-mapped protocols for **SteelSeries Arctis** headsets and **VGN CompX** mice).
  - **Bluetooth LE Devices**: Dynamically retrieves Windows' native cached PnP battery property (`{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2`) without requiring proprietary background software.
- **Interactive Portrait Flyout**:
  - Single-clicking the tray icon reveals a modern, borderless Win11-style portrait popup.
  - Features **bidirectional hover highlighting**—hovering a device row highlights its concentric ring, and hovering a ring highlights its corresponding row.
  - Interactive elements animate with **smooth 60 FPS float-interpolation transitions** (no harsh snaps).
  - Background tracks for the rings render in a **subtle, low-opacity shade of the active ring's color** representing the missing percentage.
- **Robust Utility Settings**: Includes automatic Windows startup registration, scan refreshing, and hardware telemetry detection.
- **Zero Bloat**: Compiled into a single standalone binary (< 60 KB) running on native Win32 APIs with negligible memory overhead.

---

## Project Structure 📂

- [BatteryMonitor.cs](file:///k:/_dev/arctis-battery-monitor/BatteryMonitor.cs): Unified C# source code containing the polling engine, Win32 P/Invoke APIs, and double-buffered GDI+ rendering.
- [build.bat](file:///k:/_dev/arctis-battery-monitor/build.bat): Simple batch script to compile the C# source into a standalone Windows Executable.
- [config.txt](file:///k:/_dev/arctis-battery-monitor/config.txt): Application configuration file storing the VID, PID, protocol, and target ring selections.

---

## How to Build 🛠️

BattStat has zero dependencies and can be compiled using the standard Microsoft C# compiler (`csc.exe`) bundled natively with Windows:

1. Open a Command Prompt or PowerShell window in the project directory.
2. Run the build batch file:
   ```cmd
   build.bat
   ```
3. The standalone `BatteryMonitor.exe` will be generated in the root directory.

---

## License 📄

This project is open-source and free to use.
