# BattStat 🔋✨

A lightweight, premium Windows system tray battery monitor that tracks up to three USB-HID and Bluetooth devices using concentric activity rings.

> **Why BattStat?**
> This application was created out of pure frustration with having to install, launch, and run multiple bloated, resource-heavy manufacturer companion apps (like SteelSeries GG, VGN Hub, proprietary mouse software, etc.) just to check if your wireless peripherals need charging. BattStat provides a single, unified, ultra-lightweight tray interface with zero bloat.

---

## How to Install & Run 📦

### 1. Download the Application
- **For Friends / Casual Users (Easiest)**:
  1. Go to the [Releases](https://github.com/ralphsaniatan/BattStat/releases) section of this repository.
  2. Download the precompiled **`BattStat.exe`** executable file.
  3. Move the downloaded `BattStat.exe` to a permanent folder on your PC (e.g., `Documents\BattStat` or `C:\Program Files\BattStat`).
  4. Double-click the file to launch it! It will immediately run silently in your Windows System Tray (bottom-right).

- **For Developers / From Source**:
  1. Clone this repository to your machine.
  2. Double-click and run `build.bat` to compile the source code natively using Windows' bundled C# compiler.
  3. Launch the generated `BattStat.exe` binary.

### 2. Configure Your Devices
1. Locate the **BattStat** icon in your system tray (it will appear as three concentric circles representing your device batteries).
2. Right-click the icon and choose **Settings**.
3. Select your wireless devices from the dropdown menus (supports SteelSeries headsets, VGN/Compx mice, and any connected Bluetooth LE peripherals).
4. Check the box **"Run application at Windows Startup"** so it automatically starts when you turn on your PC.
5. Click **Save & Close**.

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
