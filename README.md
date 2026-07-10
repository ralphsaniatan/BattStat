# BattStat 🔋

A lightweight Windows system tray battery monitor that tracks up to three USB and Bluetooth devices in one tray icon.

> **Why BattStat?**
> Manufacturer companion apps (like SteelSeries GG or VGN Hub) are bloated and resource-heavy just to check if wireless devices need charging. BattStat tracks headset, mouse, and Bluetooth batteries in a single, simple tray utility.

---

## How to Install & Run 📦

### 1. Download the Application
- **For Friends / Casual Users**:
  1. Go to the [Releases](https://github.com/ralphsaniatan/BattStat/releases) section of this repository.
  2. Download **`BattStat.exe`**.
  3. Move `BattStat.exe` to a permanent folder on your PC (e.g., `Documents\BattStat`).
  4. Double-click the file to launch it. It runs silently in your system tray (bottom-right).

- **For Developers / From Source**:
  1. Clone this repository.
  2. Run `build.bat` to compile the source code natively using Windows' built-in C# compiler.
  3. Run the generated `BattStat.exe` binary.

### 2. Configure Your Devices
1. Locate the **BattStat** icon in your system tray (three circles representing device batteries).
2. Right-click the icon and choose **Settings**.
3. Select your wireless devices from the dropdown menus (supports SteelSeries headsets, VGN/Compx mice, and connected Bluetooth peripherals).
4. Check **"Run application at Windows Startup"** to start the app automatically when your PC boots.
5. Click **Save & Close**.

---

## Features 🚀

- **Tray Status Rings**: Monitors three devices in the system tray using three stacked rings (Outer, Middle, Inner).
- **USB & Bluetooth Tracking**:
  - **USB**: Reads battery status directly from USB hardware (includes pre-mapped support for SteelSeries Arctis headsets and VGN CompX mice).
  - **Bluetooth**: Reads native battery levels cached by Windows.
- **Interactive Popup**:
  - Left-clicking the tray icon opens a portrait status window.
  - Hovering a device row highlights its ring, and hovering a ring highlights its device row.
  - Features smooth hover fade transitions.
  - Ring background tracks are shaded using a darker version of the ring's active color to show missing charge.
- **Startup Integration**: Easily register the app to launch on Windows startup via the settings panel.
- **Lightweight**: Single standalone binary (< 60 KB) with no background services and minimal memory footprint.

---

## Project Structure 📂

- [BatteryMonitor.cs](file:///k:/_dev/arctis-battery-monitor/BatteryMonitor.cs): C# source code for the polling engine, Win32 API calls, and GDI+ rendering.
- [build.bat](file:///k:/_dev/arctis-battery-monitor/build.bat): Batch script to compile the source code into the executable.
- [config.txt](file:///k:/_dev/arctis-battery-monitor/config.txt): Configuration file storing device selections and mappings.

---

## How to Build 🛠

1. Open Command Prompt or PowerShell in the project directory.
2. Run:
   ```cmd
   build.bat
   ```
3. The standalone `BattStat.exe` will be created in the root folder.

---

## License 📄

This project is open-source and free to use.
