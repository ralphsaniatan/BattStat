using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Net;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

[assembly: AssemblyTitle("BattStat")]
[assembly: AssemblyDescription("Lightweight tray battery monitor")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("ralphsaniatan")]
[assembly: AssemblyProduct("BattStat")]
[assembly: AssemblyCopyright("Copyright © 2026")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyVersion("1.3.0.0")]
[assembly: AssemblyFileVersion("1.3.0.0")]

namespace BatteryMonitorApp
{
    static class Program
    {
        private static System.Threading.Mutex mutex = null;

        [STAThread]
        static void Main()
        {
            const string appName = "Local\\ArctisBatteryMonitorMutex";
            bool createdNew;

            mutex = new System.Threading.Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new BatteryMonitorContext());
        }
    }

    public class DeviceConfig
    {
        public ushort Vid;
        public ushort Pid;
        public ushort UsagePage;
        public string Protocol; // "Arctis" | "VGN" | "Custom" | "None"
        public string DeviceName = "";

        // Custom protocol parameters
        public byte CustomReportId = 0;
        public byte[] CustomWritePayload = new byte[0];
        public int CustomReadLength = 65;
        public int CustomBatteryIndex = 2;
        public int CustomWiredIndex = -1;

        public DeviceConfig(ushort vid, ushort pid, ushort usagePage, string protocol)
        {
            Vid = vid;
            Pid = pid;
            UsagePage = usagePage;
            Protocol = protocol;
        }
    }

    public class HidDeviceMetadata
    {
        public string Path;
        public ushort Vid;
        public ushort Pid;
        public ushort UsagePage;
        public ushort Usage;
        public ushort OutLength;
        public ushort InLength;
        public string ProductName;
        public string ManufacturerName;

        public string DisplayName
        {
            get
            {
                string name = "";
                if (!string.IsNullOrEmpty(ManufacturerName)) name += ManufacturerName.Trim() + " ";
                if (!string.IsNullOrEmpty(ProductName)) name += ProductName.Trim();
                if (string.IsNullOrEmpty(name)) name = "Unknown Device";
                return string.Format("{0} (VID: 0x{1:X4}, PID: 0x{2:X4})", name, Vid, Pid);
            }
        }
    }

    public class BatteryMonitorContext : ApplicationContext
    {
        // Win32 API Imports
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteFile(
            IntPtr hFile,
            byte[] lpBuffer,
            uint nNumberOfBytesToWrite,
            out uint lpNumberOfBytesWritten,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadFile(
            IntPtr hFile,
            byte[] lpBuffer,
            uint nNumberOfBytesToRead,
            out uint lpNumberOfBytesRead,
            IntPtr lpOverlapped);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern void HidD_GetHidGuid(out Guid HidGuid);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SetupDiGetClassDevs(
            ref Guid Guid,
            string Enumerator,
            IntPtr hwndParent,
            uint Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiEnumDeviceInterfaces(
            IntPtr DeviceInfoSet,
            IntPtr DeviceInfoData,
            ref Guid InterfaceClassGuid,
            uint MemberIndex,
            ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool SetupDiGetDeviceInterfaceDetail(
            IntPtr DeviceInfoSet,
            ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData,
            IntPtr DeviceInterfaceDetailData,
            uint DeviceInterfaceDetailDataSize,
            out uint RequiredSize,
            IntPtr DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern bool HidD_GetAttributes(IntPtr HidDeviceObject, ref HIDD_ATTRIBUTES Attributes);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern bool HidD_GetPreparsedData(IntPtr HidDeviceObject, out IntPtr PreparsedData);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern bool HidD_FreePreparsedData(IntPtr PreparsedData);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern int HidP_GetCaps(IntPtr PreparsedData, ref HIDP_CAPS Capabilities);

        [DllImport("hid.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool HidD_GetProductString(IntPtr HidDeviceObject, byte[] Buffer, uint BufferLength);

        [DllImport("hid.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool HidD_GetManufacturerString(IntPtr HidDeviceObject, byte[] Buffer, uint BufferLength);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiEnumDeviceInfo(
            IntPtr DeviceInfoSet,
            uint MemberIndex,
            ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool SetupDiGetDeviceInstanceId(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            System.Text.StringBuilder DeviceInstanceId,
            uint DeviceInstanceIdSize,
            out uint RequiredSize);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetupDiGetDevicePropertyW(
            IntPtr deviceInfoSet,
            ref SP_DEVINFO_DATA deviceInfoData,
            ref DEVPROPKEY propertyKey,
            out uint propertyType,
            byte[] propertyBuffer,
            uint propertyBufferSize,
            out uint requiredSize,
            uint flags);

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVINFO_DATA
        {
            public uint cbSize;
            public Guid classGuid;
            public uint devInst;
            public IntPtr reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DEVPROPKEY
        {
            public Guid fmtid;
            public uint pid;
        }

        public class BluetoothDeviceMetadata
        {
            public string InstanceId { get; set; }
            public string FriendlyName { get; set; }
            public int BatteryLevel { get; set; }
        }

        private List<BluetoothDeviceMetadata> GetConnectedBluetoothDevices()
        {
            List<BluetoothDeviceMetadata> list = new List<BluetoothDeviceMetadata>();
            Guid bluetoothGuid = new Guid("e0cbf06c-cd8b-4647-bb8a-263b43f0f974");
            IntPtr hDevInfo = SetupDiGetClassDevs(ref bluetoothGuid, null, IntPtr.Zero, 2); // DIGCF_PRESENT = 2
            if (hDevInfo == (IntPtr)(-1)) return list;

            SP_DEVINFO_DATA devInfoData = new SP_DEVINFO_DATA();
            devInfoData.cbSize = (uint)Marshal.SizeOf(devInfoData);

            uint index = 0;
            while (SetupDiEnumDeviceInfo(hDevInfo, index, ref devInfoData))
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder(256);
                uint reqSize;
                if (SetupDiGetDeviceInstanceId(hDevInfo, ref devInfoData, sb, (uint)sb.Capacity, out reqSize))
                {
                    string instanceId = sb.ToString();
                    string friendlyName = GetDeviceFriendlyName(hDevInfo, devInfoData);
                    int battery = GetDeviceBatteryProperty(hDevInfo, devInfoData);

                    if (!string.IsNullOrEmpty(friendlyName) && 
                        !friendlyName.Contains("Enumerator") && 
                        !friendlyName.Contains("Adapter") && 
                        !friendlyName.Contains("Service Discovery") && 
                        !friendlyName.Contains("Intel(R)") && 
                        !friendlyName.Contains("Realtek") && 
                        !friendlyName.Contains("Qualcomm") &&
                        !friendlyName.Contains("MediaTek") &&
                        !friendlyName.ToLower().Contains("avrcp") &&
                        !friendlyName.ToLower().Contains("transport") &&
                        !friendlyName.ToLower().Contains("hands-free") &&
                        !friendlyName.ToLower().Contains("handsfree") &&
                        !friendlyName.ToLower().Contains("audio gateway") &&
                        !friendlyName.ToLower().Contains("l2cap") &&
                        !friendlyName.ToLower().Contains("identification"))
                    {
                        list.Add(new BluetoothDeviceMetadata
                        {
                            InstanceId = instanceId,
                            FriendlyName = friendlyName,
                            BatteryLevel = battery
                        });
                    }
                }
                index++;
            }
            SetupDiDestroyDeviceInfoList(hDevInfo);
            return list;
        }

        private string GetDeviceFriendlyName(IntPtr hDevInfo, SP_DEVINFO_DATA devInfoData)
        {
            DEVPROPKEY key = new DEVPROPKEY();
            key.fmtid = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"); // DEVPKEY_Device_FriendlyName
            key.pid = 14;

            uint propType;
            uint reqSize;
            byte[] buffer = new byte[512];
            if (SetupDiGetDevicePropertyW(hDevInfo, ref devInfoData, ref key, out propType, buffer, (uint)buffer.Length, out reqSize, 0))
            {
                if (reqSize > 2)
                {
                    return System.Text.Encoding.Unicode.GetString(buffer, 0, (int)reqSize).Split('\0')[0].Trim();
                }
            }

            // Fallback to DeviceDesc
            key.fmtid = new Guid("a52027e4-ee48-47d9-9224-6948edb3ca00"); // DEVPKEY_Device_DeviceDesc
            key.pid = 2;
            if (SetupDiGetDevicePropertyW(hDevInfo, ref devInfoData, ref key, out propType, buffer, (uint)buffer.Length, out reqSize, 0))
            {
                if (reqSize > 2)
                {
                    return System.Text.Encoding.Unicode.GetString(buffer, 0, (int)reqSize).Split('\0')[0].Trim();
                }
            }

            return "";
        }

        private int GetDeviceBatteryProperty(IntPtr hDevInfo, SP_DEVINFO_DATA devInfoData)
        {
            DEVPROPKEY key = new DEVPROPKEY();
            key.fmtid = new Guid("104ea319-6ee2-4701-bd47-8ddbf425bbe5"); // DEVPKEY_Device_BatteryLevel
            key.pid = 2;

            uint propType;
            uint reqSize;
            byte[] buffer = new byte[4];
            if (SetupDiGetDevicePropertyW(hDevInfo, ref devInfoData, ref key, out propType, buffer, (uint)buffer.Length, out reqSize, 0))
            {
                if (reqSize >= 1)
                {
                    return buffer[0];
                }
            }
            return -1;
        }

        // Win32 Structures
        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVICE_INTERFACE_DATA
        {
            public uint cbSize;
            public Guid InterfaceClassGuid;
            public uint Flags;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HIDD_ATTRIBUTES
        {
            public int Size;
            public ushort VendorID;
            public ushort ProductID;
            public ushort VersionNumber;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HIDP_CAPS
        {
            public ushort Usage;
            public ushort UsagePage;
            public ushort InputReportByteLength;
            public ushort OutputReportByteLength;
            public ushort FeatureReportByteLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            public ushort[] Reserved;
            public ushort NumberLinkCollectionNodes;
            public ushort NumberInputButtonCaps;
            public ushort NumberInputValueCaps;
            public ushort NumberInputDataIndices;
            public ushort NumberOutputButtonCaps;
            public ushort NumberOutputValueCaps;
            public ushort NumberOutputDataIndices;
            public ushort NumberFeatureButtonCaps;
            public ushort NumberFeatureValueCaps;
            public ushort NumberFeatureDataIndices;
        }

        public struct BatteryHistoryEntry
        {
            public DateTime Time;
            public int Battery;
            public BatteryHistoryEntry(DateTime time, int battery)
            {
                Time = time;
                Battery = battery;
            }
        }

        // Application State
        private NotifyIcon notifyIcon;
        private ContextMenuStrip contextMenu;
        private Timer timer;

        private bool warnedOuter25 = false;
        private bool warnedOuter10 = false;
        private bool warnedOuterHealth = false;
        private List<BatteryHistoryEntry> outerHistory = new List<BatteryHistoryEntry>();

        private bool warnedMiddle25 = false;
        private bool warnedMiddle10 = false;
        private bool warnedMiddleHealth = false;
        private List<BatteryHistoryEntry> middleHistory = new List<BatteryHistoryEntry>();

        private bool warnedInner25 = false;
        private bool warnedInner10 = false;
        private bool warnedInnerHealth = false;
        private List<BatteryHistoryEntry> innerHistory = new List<BatteryHistoryEntry>();

        // Dynamic Configurations
        public DeviceConfig outerConfig = new DeviceConfig(0x1038, 0x12AD, 0xFF43, "Arctis");
        public DeviceConfig middleConfig = new DeviceConfig(0x3554, 0xF503, 0xFF02, "VGN");
        public DeviceConfig innerConfig = new DeviceConfig(0, 0, 0, "None");

        public bool autoUpdateEnabled = true;

        // Shared state for Settings Form
        public bool LastOuterTransmitterConnected { get; private set; }
        public bool LastOuterConnected { get; private set; }
        public int LastOuterBattery { get; private set; }
        public bool LastOuterWired { get; private set; }

        public bool LastMiddleConnected { get; private set; }
        public int LastMiddleBattery { get; private set; }
        public bool LastMiddleWired { get; private set; }

        public bool LastInnerConnected { get; private set; }
        public int LastInnerBattery { get; private set; }
        public bool LastInnerWired { get; private set; }

        public string LastOuterDeviceName { get; private set; }
        public string LastMiddleDeviceName { get; private set; }
        public string LastInnerDeviceName { get; private set; }

        private Form settingsForm = null;
        private FlyoutForm activeFlyout = null;
        private DateTime lastClosedTime = DateTime.MinValue;

        public BatteryMonitorContext()
        {
            // Load custom configurations on startup
            LoadConfiguration();

            // Initialize Tray Icon
            notifyIcon = new NotifyIcon();
            notifyIcon.Text = "BattStat";
            notifyIcon.Visible = true;
            notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ShowFlyoutWindow();
                }
            };

            // Create Context Menu
            contextMenu = new ContextMenuStrip();
            updateMenuItem = new ToolStripMenuItem("Update Available!");
            updateMenuItem.Visible = false;
            updateMenuItem.Click += (s, e) => System.Diagnostics.Process.Start("https://github.com/ralphsaniatan/BattStat/releases/latest");
            contextMenu.Items.Add(updateMenuItem);

            contextMenu.Items.Add("Settings", null, (s, e) => ShowSettingsWindow());
            contextMenu.Items.Add("Refresh", null, (s, e) => UpdateBatteryStatus());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Exit", null, (s, e) => ExitApplication());
            notifyIcon.ContextMenuStrip = contextMenu;

            // Start update check in background
            if (autoUpdateEnabled)
            {
                Task.Run(() => CheckForUpdates());
            }

            // Initial status query
            UpdateBatteryStatus();

            // Configure timer (runs every 60 seconds)
            timer = new Timer();
            timer.Interval = 60000;
            timer.Tick += (s, e) => UpdateBatteryStatus();
            timer.Start();
        }

        private string GetConfigFilePath()
        {
            string dir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            return Path.Combine(dir, "config.txt");
        }

        private void LoadConfiguration()
        {
            string path = GetConfigFilePath();
            if (!File.Exists(path)) return;

            try
            {
                string[] lines = File.ReadAllLines(path);
                foreach (string line in lines)
                {
                    if (string.IsNullOrEmpty(line) || !line.Contains("=")) continue;
                    string[] parts = line.Split(new char[] { '=' }, 2);
                    string key = parts[0].Trim();
                    string val = parts[1].Trim();

                    if (key == "OuterVid") outerConfig.Vid = Convert.ToUInt16(val, val.StartsWith("0x") ? 16 : 10);
                    else if (key == "OuterPid") outerConfig.Pid = Convert.ToUInt16(val, val.StartsWith("0x") ? 16 : 10);
                    else if (key == "OuterUsagePage") outerConfig.UsagePage = Convert.ToUInt16(val, val.StartsWith("0x") ? 16 : 10);
                    else if (key == "OuterProto") outerConfig.Protocol = val;
                    else if (key == "OuterDeviceName") outerConfig.DeviceName = val;
                    else if (key == "OuterCustomReportId") outerConfig.CustomReportId = Convert.ToByte(val);
                    else if (key == "OuterCustomWritePayload") outerConfig.CustomWritePayload = HexStringToBytes(val);
                    else if (key == "OuterCustomReadLength") outerConfig.CustomReadLength = Convert.ToInt32(val);
                    else if (key == "OuterCustomBatteryIndex") outerConfig.CustomBatteryIndex = Convert.ToInt32(val);
                    else if (key == "OuterCustomWiredIndex") outerConfig.CustomWiredIndex = Convert.ToInt32(val);

                    else if (key == "MiddleVid") middleConfig.Vid = Convert.ToUInt16(val, val.StartsWith("0x") ? 16 : 10);
                    else if (key == "MiddlePid") middleConfig.Pid = Convert.ToUInt16(val, val.StartsWith("0x") ? 16 : 10);
                    else if (key == "MiddleUsagePage") middleConfig.UsagePage = Convert.ToUInt16(val, val.StartsWith("0x") ? 16 : 10);
                    else if (key == "MiddleProto") middleConfig.Protocol = val;
                    else if (key == "MiddleDeviceName") middleConfig.DeviceName = val;
                    else if (key == "MiddleCustomReportId") middleConfig.CustomReportId = Convert.ToByte(val);
                    else if (key == "MiddleCustomWritePayload") middleConfig.CustomWritePayload = HexStringToBytes(val);
                    else if (key == "MiddleCustomReadLength") middleConfig.CustomReadLength = Convert.ToInt32(val);
                    else if (key == "MiddleCustomBatteryIndex") middleConfig.CustomBatteryIndex = Convert.ToInt32(val);
                    else if (key == "MiddleCustomWiredIndex") middleConfig.CustomWiredIndex = Convert.ToInt32(val);

                    else if (key == "InnerVid") innerConfig.Vid = Convert.ToUInt16(val, val.StartsWith("0x") ? 16 : 10);
                    else if (key == "InnerPid") innerConfig.Pid = Convert.ToUInt16(val, val.StartsWith("0x") ? 16 : 10);
                    else if (key == "InnerUsagePage") innerConfig.UsagePage = Convert.ToUInt16(val, val.StartsWith("0x") ? 16 : 10);
                    else if (key == "InnerProto") innerConfig.Protocol = val;
                    else if (key == "InnerDeviceName") innerConfig.DeviceName = val;
                    else if (key == "InnerCustomReportId") innerConfig.CustomReportId = Convert.ToByte(val);
                    else if (key == "InnerCustomWritePayload") innerConfig.CustomWritePayload = HexStringToBytes(val);
                    else if (key == "InnerCustomReadLength") innerConfig.CustomReadLength = Convert.ToInt32(val);
                    else if (key == "InnerCustomBatteryIndex") innerConfig.CustomBatteryIndex = Convert.ToInt32(val);
                    else if (key == "InnerCustomWiredIndex") innerConfig.CustomWiredIndex = Convert.ToInt32(val);
                    else if (key == "AutoUpdate") autoUpdateEnabled = (val.ToLower() == "true");
                }
            }
            catch { }
        }

        public void SaveConfiguration()
        {
            try
            {
                string path = GetConfigFilePath();
                List<string> lines = new List<string>();
                lines.Add("OuterVid=" + outerConfig.Vid);
                lines.Add("OuterPid=" + outerConfig.Pid);
                lines.Add("OuterUsagePage=" + outerConfig.UsagePage);
                lines.Add("OuterProto=" + outerConfig.Protocol);
                lines.Add("OuterDeviceName=" + outerConfig.DeviceName);
                lines.Add("OuterCustomReportId=" + outerConfig.CustomReportId);
                lines.Add("OuterCustomWritePayload=" + BytesToHexString(outerConfig.CustomWritePayload));
                lines.Add("OuterCustomReadLength=" + outerConfig.CustomReadLength);
                lines.Add("OuterCustomBatteryIndex=" + outerConfig.CustomBatteryIndex);
                lines.Add("OuterCustomWiredIndex=" + outerConfig.CustomWiredIndex);

                lines.Add("MiddleVid=" + middleConfig.Vid);
                lines.Add("MiddlePid=" + middleConfig.Pid);
                lines.Add("MiddleUsagePage=" + middleConfig.UsagePage);
                lines.Add("MiddleProto=" + middleConfig.Protocol);
                lines.Add("MiddleDeviceName=" + middleConfig.DeviceName);
                lines.Add("MiddleCustomReportId=" + middleConfig.CustomReportId);
                lines.Add("MiddleCustomWritePayload=" + BytesToHexString(middleConfig.CustomWritePayload));
                lines.Add("MiddleCustomReadLength=" + middleConfig.CustomReadLength);
                lines.Add("MiddleCustomBatteryIndex=" + middleConfig.CustomBatteryIndex);
                lines.Add("MiddleCustomWiredIndex=" + middleConfig.CustomWiredIndex);

                lines.Add("InnerVid=" + innerConfig.Vid);
                lines.Add("InnerPid=" + innerConfig.Pid);
                lines.Add("InnerUsagePage=" + innerConfig.UsagePage);
                lines.Add("InnerProto=" + innerConfig.Protocol);
                lines.Add("InnerDeviceName=" + innerConfig.DeviceName);
                lines.Add("InnerCustomReportId=" + innerConfig.CustomReportId);
                lines.Add("InnerCustomWritePayload=" + BytesToHexString(innerConfig.CustomWritePayload));
                lines.Add("InnerCustomReadLength=" + innerConfig.CustomReadLength);
                lines.Add("InnerCustomBatteryIndex=" + innerConfig.CustomBatteryIndex);
                lines.Add("InnerCustomWiredIndex=" + innerConfig.CustomWiredIndex);
                lines.Add("AutoUpdate=" + autoUpdateEnabled.ToString());

                File.WriteAllLines(path, lines.ToArray());
            }
            catch { }
        }

        private byte[] HexStringToBytes(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return new byte[0];
            List<byte> bytes = new List<byte>();
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes.Add(Convert.ToByte(hex.Substring(i, 2), 16));
            }
            return bytes.ToArray();
        }

        private string BytesToHexString(byte[] bytes)
        {
            if (bytes == null) return "";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (byte b in bytes)
            {
                sb.Append(b.ToString("X2"));
            }
            return sb.ToString();
        }

        private static string GetProductString(IntPtr hDevice)
        {
            byte[] buffer = new byte[256];
            if (HidD_GetProductString(hDevice, buffer, (uint)buffer.Length))
            {
                return System.Text.Encoding.Unicode.GetString(buffer).Split('\0')[0].Trim();
            }
            return "";
        }

        private static string GetManufacturerString(IntPtr hDevice)
        {
            byte[] buffer = new byte[256];
            if (HidD_GetManufacturerString(hDevice, buffer, (uint)buffer.Length))
            {
                return System.Text.Encoding.Unicode.GetString(buffer).Split('\0')[0].Trim();
            }
            return "";
        }

        private List<HidDeviceMetadata> GetConnectedHidDevices()
        {
            List<HidDeviceMetadata> devices = new List<HidDeviceMetadata>();
            Guid hidGuid;
            HidD_GetHidGuid(out hidGuid);

            IntPtr hDevInfo = SetupDiGetClassDevs(ref hidGuid, null, IntPtr.Zero, 0x10); // DIGCF_PRESENT | DIGCF_DEVICEINTERFACE
            if (hDevInfo == (IntPtr)(-1)) return devices;

            SP_DEVICE_INTERFACE_DATA interfaceData = new SP_DEVICE_INTERFACE_DATA();
            interfaceData.cbSize = (uint)Marshal.SizeOf(interfaceData);

            uint index = 0;
            while (SetupDiEnumDeviceInterfaces(hDevInfo, IntPtr.Zero, ref hidGuid, index, ref interfaceData))
            {
                uint requiredSize = 0;
                SetupDiGetDeviceInterfaceDetail(hDevInfo, ref interfaceData, IntPtr.Zero, 0, out requiredSize, IntPtr.Zero);

                if (requiredSize > 0)
                {
                    IntPtr detailDataBuffer = Marshal.AllocHGlobal((int)requiredSize);
                    try
                    {
                        int cbSize = (IntPtr.Size == 8) ? 8 : (Marshal.SystemDefaultCharSize == 2 ? 6 : 5);
                        Marshal.WriteInt32(detailDataBuffer, cbSize);

                        if (SetupDiGetDeviceInterfaceDetail(hDevInfo, ref interfaceData, detailDataBuffer, requiredSize, out requiredSize, IntPtr.Zero))
                        {
                            IntPtr pDevicePath = new IntPtr(detailDataBuffer.ToInt64() + 4);
                            string path = Marshal.PtrToStringAuto(pDevicePath);
                            if (!string.IsNullOrEmpty(path))
                            {
                                // Open with query access (0) to avoid locked devices (Access Denied)
                                IntPtr hDevice = CreateFile(
                                    path,
                                    0, // Query access
                                    3, // FILE_SHARE_READ | FILE_SHARE_WRITE
                                    IntPtr.Zero,
                                    3,
                                    0,
                                    IntPtr.Zero);

                                if (hDevice != (IntPtr)(-1))
                                {
                                    HIDD_ATTRIBUTES attrs = new HIDD_ATTRIBUTES();
                                    attrs.Size = Marshal.SizeOf(attrs);
                                    if (HidD_GetAttributes(hDevice, ref attrs))
                                    {
                                        HidDeviceMetadata dev = new HidDeviceMetadata();
                                        dev.Path = path;
                                        dev.Vid = attrs.VendorID;
                                        dev.Pid = attrs.ProductID;
                                        dev.ProductName = GetProductString(hDevice);
                                        dev.ManufacturerName = GetManufacturerString(hDevice);

                                        IntPtr preparsedData;
                                        if (HidD_GetPreparsedData(hDevice, out preparsedData))
                                        {
                                            HIDP_CAPS caps = new HIDP_CAPS();
                                            int capsStatus = HidP_GetCaps(preparsedData, ref caps);
                                            if (capsStatus == 0x00110000 || capsStatus == 1 || caps.UsagePage != 0)
                                            {
                                                dev.UsagePage = caps.UsagePage;
                                                dev.Usage = caps.Usage;
                                                dev.OutLength = caps.OutputReportByteLength;
                                                dev.InLength = caps.InputReportByteLength;

                                                // Exclude standard system controls, keyboards, mice coordinates (UsagePage 1, 12, etc.)
                                                // Keep only custom vendor-defined channels (>= 0xFF00) or standard UPS/battery pages (0x84, 0x85)
                                                if (caps.UsagePage >= 0xFF00 || caps.UsagePage == 0x84 || caps.UsagePage == 0x85)
                                                {
                                                    devices.Add(dev);
                                                }
                                            }
                                            HidD_FreePreparsedData(preparsedData);
                                        }
                                    }
                                    CloseHandle(hDevice);
                                }
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(detailDataBuffer);
                    }
                }
                index++;
            }
            SetupDiDestroyDeviceInfoList(hDevInfo);
            return devices;
        }

        private IntPtr OpenDeviceForReadWrite(string path)
        {
            return CreateFile(
                path,
                0xC0000000, // GENERIC_READ | GENERIC_WRITE
                3,          // FILE_SHARE_READ | FILE_SHARE_WRITE
                IntPtr.Zero,
                3,          // OPEN_EXISTING
                0,
                IntPtr.Zero);
        }

        private bool PollDeviceBattery(DeviceConfig config, List<HidDeviceMetadata> connectedDevices, out int battery, out bool wired)
        {
            battery = 0;
            wired = false;

            if (config.Protocol == "None") return false;

            if (config.Protocol == "Bluetooth")
            {
                List<BluetoothDeviceMetadata> btDevices = GetConnectedBluetoothDevices();
                BluetoothDeviceMetadata target = btDevices.Find(d => d.FriendlyName == config.DeviceName);
                if (target != null)
                {
                    battery = target.BatteryLevel;
                    wired = false;
                    return true;
                }
                return false;
            }

            HidDeviceMetadata dev = null;
            if (config.Protocol == "Arctis")
            {
                dev = connectedDevices.Find(d => d.Vid == config.Vid && d.Pid == config.Pid && d.UsagePage == 0xFF43);
            }
            else if (config.Protocol == "VGN")
            {
                dev = connectedDevices.Find(d => d.Vid == config.Vid && d.Pid == config.Pid && d.UsagePage == 0xFF02);
            }
            else
            {
                dev = connectedDevices.Find(d => d.Vid == config.Vid && d.Pid == config.Pid && d.UsagePage == config.UsagePage);
                if (dev == null)
                {
                    dev = connectedDevices.Find(d => d.Vid == config.Vid && d.Pid == config.Pid);
                }
            }
            if (dev == null) return false;

            IntPtr hDevice = OpenDeviceForReadWrite(dev.Path);
            if (hDevice == (IntPtr)(-1)) return false;

            try
            {
                if (config.Protocol == "Arctis")
                {
                    byte[] writeBuf = new byte[dev.OutLength > 0 ? dev.OutLength : 65];
                    writeBuf[0] = 0x06;
                    writeBuf[1] = 0x14;

                    uint written;
                    if (WriteFile(hDevice, writeBuf, (uint)writeBuf.Length, out written, IntPtr.Zero))
                    {
                        byte[] readBuf = new byte[dev.InLength > 0 ? dev.InLength : 65];
                        uint read;
                        if (ReadFile(hDevice, readBuf, (uint)readBuf.Length, out read, IntPtr.Zero))
                        {
                            if (readBuf[2] == 0x03)
                            {
                                Array.Clear(writeBuf, 0, writeBuf.Length);
                                writeBuf[0] = 0x06;
                                writeBuf[1] = 0x18;
                                if (WriteFile(hDevice, writeBuf, (uint)writeBuf.Length, out written, IntPtr.Zero))
                                {
                                    if (ReadFile(hDevice, readBuf, (uint)readBuf.Length, out read, IntPtr.Zero))
                                    {
                                        battery = ClampBattery(readBuf[2]);
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
                else if (config.Protocol == "VGN")
                {
                    // HID writes must match the device's actual output report length
                    int outLen = dev.OutLength > 0 ? dev.OutLength : 17;
                    byte[] writeBuf = new byte[outLen];
                    writeBuf[0] = 8;
                    writeBuf[1] = 4;
                    writeBuf[outLen - 1] = 73;

                    uint written;
                    if (WriteFile(hDevice, writeBuf, (uint)writeBuf.Length, out written, IntPtr.Zero))
                    {
                        byte[] readBuf = new byte[17];
                        uint read;
                        if (ReadFile(hDevice, readBuf, (uint)readBuf.Length, out read, IntPtr.Zero) && read >= 8)
                        {
                            battery = ClampBattery(readBuf[6]);
                            wired = (readBuf[7] == 1);
                            return true;
                        }
                    }
                }
                else if (config.Protocol == "Custom")
                {
                    int outLen = dev.OutLength > 0 ? dev.OutLength : 65;
                    byte[] writeBuf = new byte[outLen];
                    writeBuf[0] = config.CustomReportId;
                    
                    if (config.CustomWritePayload != null)
                    {
                        int len = Math.Min(config.CustomWritePayload.Length, outLen - 1);
                        Array.Copy(config.CustomWritePayload, 0, writeBuf, 1, len);
                    }

                    uint written;
                    if (WriteFile(hDevice, writeBuf, (uint)writeBuf.Length, out written, IntPtr.Zero))
                    {
                        int inLen = config.CustomReadLength > 0 ? config.CustomReadLength : (dev.InLength > 0 ? dev.InLength : 65);
                        byte[] readBuf = new byte[inLen];
                        uint read;
                        if (ReadFile(hDevice, readBuf, (uint)readBuf.Length, out read, IntPtr.Zero))
                        {
                            if (config.CustomBatteryIndex >= 0 && config.CustomBatteryIndex < readBuf.Length)
                            {
                                battery = ClampBattery(readBuf[config.CustomBatteryIndex]);
                                if (config.CustomWiredIndex >= 0 && config.CustomWiredIndex < readBuf.Length)
                                {
                                    wired = (readBuf[config.CustomWiredIndex] == 1);
                                }
                                return true;
                            }
                        }
                    }
                }
            }
            catch { }
            finally
            {
                CloseHandle(hDevice);
            }
            return false;
        }

        // Reject implausible battery readings (garbage 0xFF bytes etc.)
        private static int ClampBattery(int raw)
        {
            if (raw < 0 || raw > 100) return -1; // treated as "no data" by callers
            return raw;
        }

        private void RunBatteryWarnings(string label, bool connected, int battery, ref bool warned10, ref bool warned25, ref bool warnedHealth, List<BatteryHistoryEntry> history)
        {
            if (!connected || battery < 0)
            {
                warned10 = false;
                warned25 = false;
                warnedHealth = false; // re-arm rapid-drain detection after reconnect
                history.Clear();      // stale samples from before disconnect skew the drain window
                return;
            }
            DateTime now = DateTime.Now;
            history.Add(new BatteryHistoryEntry(now, battery));
            history.RemoveAll(x => x.Time < now.AddMinutes(-70));

            // Health check (rapid drain)
            var entries35 = history.FindAll(x => x.Time >= now.AddMinutes(-35));
            if (entries35.Count >= 20)
            {
                var hist30 = entries35.Find(x => x.Time <= now.AddMinutes(-25));
                if (hist30.Time != DateTime.MinValue)
                {
                    int diff = hist30.Battery - battery;
                    if (diff >= 15 && !warnedHealth)
                    {
                        warnedHealth = true;
                        ShowNotification(label + " Device Battery Warning", label + " battery draining rapidly! (dropped " + diff + "% in 30 mins).", ToolTipIcon.Warning);
                    }
                }
            }
            
            // Charging reset
            var histReset = entries35.Find(x => x.Time <= now.AddMinutes(-25));
            if (histReset.Time != DateTime.MinValue && battery > histReset.Battery)
            {
                warnedHealth = false;
            }

            // Low battery checks.
            // warned10/warned25 stay latched once fired (until level recovers above 25%) so an
            // oscillating gauge cannot re-fire notifications every poll.
            if (battery <= 10)
            {
                if (!warned10)
                {
                    warned10 = true;
                    warned25 = true;
                    ShowLowBatteryNotification(label, battery, true);
                    ShowNotification(label + " Device Critical", "Battery level is at " + battery + "%. Please charge.", ToolTipIcon.Error);
                }
            }
            else if (battery <= 15)
            {
                // low-battery toast fires at 15% and below (but above the critical 10%)
                if (!warned25)
                {
                    warned25 = true;
                    ShowLowBatteryNotification(label, battery, false);
                }
                // NOTE: do not reset warned10 here — see oscillation comment above.
                // It clears only when the level recovers above 25%.
            }
            else if (battery <= 25)
            {
                if (!warned25)
                {
                    warned25 = true;
                    ShowLowBatteryNotification(label, battery, false);
                }
                // NOTE: do not reset warned10 here — see oscillation comment above.
                // It clears only when the level recovers above 25%.
            }
            else
            {
                warned10 = false;
                warned25 = false;
            }
        }

        private string GetTooltipLine(string positionName, DeviceConfig config, bool connected, int battery, bool transmitterConnected, bool wired)
        {
            string cleanName = GetFriendlyDeviceName(config.DeviceName, config.Vid, config.Pid);
            if (string.IsNullOrEmpty(cleanName)) cleanName = positionName + " Ring";

            if (config.Protocol == "None") return cleanName + ": Disabled";
            if (connected) return cleanName + ": " + (battery >= 0 ? battery + "%" : "Connected (No Battery Data)") + (wired ? " [Charging]" : "");
            if (transmitterConnected) return cleanName + ": Powered Off";
            return cleanName + ": Disconnected";
        }

        public void UpdateBatteryStatus()
        {
            List<HidDeviceMetadata> connectedDevices = GetConnectedHidDevices();

            // 1. Poll Devices
            int outerBattery = 0;
            bool outerWired = false;
            bool outerConnected = PollDeviceBattery(outerConfig, connectedDevices, out outerBattery, out outerWired);
            bool outerTransmitterConnected = connectedDevices.Exists(d => d.Vid == outerConfig.Vid && d.Pid == outerConfig.Pid && d.UsagePage == outerConfig.UsagePage);

            int middleBattery = 0;
            bool middleWired = false;
            bool middleConnected = PollDeviceBattery(middleConfig, connectedDevices, out middleBattery, out middleWired);

            int innerBattery = 0;
            bool innerWired = false;
            bool innerConnected = PollDeviceBattery(innerConfig, connectedDevices, out innerBattery, out innerWired);

            // 2. Save Shared States
            LastOuterTransmitterConnected = outerTransmitterConnected;
            LastOuterConnected = outerConnected;
            LastOuterBattery = outerBattery;
            LastOuterWired = outerWired;

            LastMiddleConnected = middleConnected;
            LastMiddleBattery = middleBattery;
            LastMiddleWired = middleWired;

            LastInnerConnected = innerConnected;
            LastInnerBattery = innerBattery;
            LastInnerWired = innerWired;

            // 3. Find dynamic names
            HidDeviceMetadata outerDev = connectedDevices.Find(d => d.Vid == outerConfig.Vid && d.Pid == outerConfig.Pid);
            LastOuterDeviceName = (outerDev != null && !string.IsNullOrEmpty(outerDev.ProductName)) ? outerDev.ProductName : outerConfig.DeviceName;

            HidDeviceMetadata middleDev = connectedDevices.Find(d => d.Vid == middleConfig.Vid && d.Pid == middleConfig.Pid);
            LastMiddleDeviceName = (middleDev != null && !string.IsNullOrEmpty(middleDev.ProductName)) ? middleDev.ProductName : middleConfig.DeviceName;

            HidDeviceMetadata innerDev = connectedDevices.Find(d => d.Vid == innerConfig.Vid && d.Pid == innerConfig.Pid);
            LastInnerDeviceName = (innerDev != null && !string.IsNullOrEmpty(innerDev.ProductName)) ? innerDev.ProductName : innerConfig.DeviceName;

            // 4. Low battery warnings
            RunBatteryWarnings("Outer", outerConnected, outerBattery, ref warnedOuter10, ref warnedOuter25, ref warnedOuterHealth, outerHistory);
            RunBatteryWarnings("Middle", middleConnected, middleBattery, ref warnedMiddle10, ref warnedMiddle25, ref warnedMiddleHealth, middleHistory);
            RunBatteryWarnings("Inner", innerConnected, innerBattery, ref warnedInner10, ref warnedInner25, ref warnedInnerHealth, innerHistory);

            // 5. Tooltip
            string oTooltip = GetTooltipLine("Outer", outerConfig, outerConnected, outerBattery, outerTransmitterConnected, outerWired);
            string mTooltip = GetTooltipLine("Middle", middleConfig, middleConnected, middleBattery, false, middleWired);
            string iTooltip = GetTooltipLine("Inner", innerConfig, innerConnected, innerBattery, false, innerWired);

            string combined = oTooltip + "\n" + mTooltip + "\n" + iTooltip;
            if (combined.Length > 63) combined = combined.Substring(0, 63);
            notifyIcon.Text = combined;

            // 6. Draw Icon
            Icon oldIcon = notifyIcon.Icon;
            Icon newIcon = GetTrayIcon(outerConnected, outerBattery, middleConnected, middleBattery, innerConnected, innerBattery);
            notifyIcon.Icon = newIcon;

            if (oldIcon != null)
            {
                DestroyIcon(oldIcon.Handle);
                oldIcon.Dispose();
            }
        }

        private Icon GetTrayIcon(bool outerConnected, int outerBattery, bool middleConnected, int middleBattery, bool innerConnected, int innerBattery)
        {
            using (Bitmap bmp = new Bitmap(16, 16))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);

                    int activeCount = (outerConfig.Protocol != "None" ? 1 : 0) + (middleConfig.Protocol != "None" ? 1 : 0) + (innerConfig.Protocol != "None" ? 1 : 0);
                    float penW = activeCount == 1 ? 3.2f : (activeCount == 2 ? 2.4f : 1.6f);
                    
                    int currentIdx = 0;
                    Action<DeviceConfig, bool, int, Color> DrawDynamicTray = (cfg, conn, bat, col) => {
                        if (cfg.Protocol == "None") return;
                        float xy = 0f, size = 0f;
                        if (activeCount == 1) { xy = 3.2f; size = 9.6f; }
                        else if (activeCount == 2) { 
                            if (currentIdx == 0) { xy = 1.6f; size = 12.8f; }
                            else { xy = 4.8f; size = 6.4f; }
                        }
                        else {
                            if (currentIdx == 0) { xy = 1.2f; size = 13.6f; }
                            else if (currentIdx == 1) { xy = 3.2f; size = 9.6f; }
                            else { xy = 5.2f; size = 5.6f; }
                        }
                        currentIdx++;
                        DrawTrayRing(g, cfg, conn, bat, xy, size, penW, col);
                    };

                    DrawDynamicTray(outerConfig, outerConnected, outerBattery, Color.FromArgb(255, 17, 72));
                    DrawDynamicTray(middleConfig, middleConnected, middleBattery, Color.FromArgb(0, 180, 255));
                    DrawDynamicTray(innerConfig, innerConnected, innerBattery, Color.FromArgb(170, 255, 0));

                    return Icon.FromHandle(bmp.GetHicon());
                }
            }
        }

        private void DrawTrayRing(Graphics g, DeviceConfig config, bool connected, int battery, float xy, float size, float penWidth, Color ringColor)
        {
            if (config.Protocol == "None") return;

            // Background circle (Dark Grey if connected and has battery, solid Grey if disconnected or no battery data)
            Color bgCol = (connected && battery >= 0) ? Color.FromArgb(40, 255, 255, 255) : Color.FromArgb(100, 128, 128, 128);
            using (Pen penBg = new Pen(bgCol, penWidth))
            {
                g.DrawEllipse(penBg, xy, xy, size, size);
            }

            // Active status arc
            if (connected && battery >= 0)
            {
                Color activeCol = ringColor;
                if (battery <= 25) activeCol = Color.FromArgb(231, 76, 60); // Red warn

                using (Pen penActive = new Pen(activeCol, penWidth))
                {
                    // Apple Watch style: rounded caps!
                    penActive.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    penActive.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                    float sweep = 360f * (battery / 100f);
                    if (sweep > 0)
                    {
                        g.DrawArc(penActive, xy, xy, size, size, -90f, sweep);
                    }
                }
            }
        }

        private void ShowNotification(string title, string text, ToolTipIcon iconType)
        {
            try
            {
                // Windows silently drops balloons when Focus Assist / Quiet Hours is on,
                // or notifications for this app are disabled in Settings.
                notifyIcon.ShowBalloonTip(8000, title, text, iconType);
            }
            catch { }
        }

        private BatteryToast activeToast = null;

        // Distinct synthesized alert: critical = urgent double-beep, low = soft single chime.
        // Played directly (Console.Beep) so it sounds even in Do Not Disturb mode.
        private void PlayAlertSound(bool critical)
        {
            try
            {
                if (critical)
                {
                    // urgent descending pair
                    Console.Beep(880, 180);
                    Console.Beep(660, 260);
                }
                else
                {
                    // gentle single tone
                    Console.Beep(740, 220);
                }
            }
            catch { }
        }

        // Custom in-app toast for low/critical battery warnings: no banner title,
        // simple text, blinking warning icon, close button. Not affected by
        // Focus Assist / Do Not Disturb because it is our own window.
        public void ShowLowBatteryNotification(string deviceLabel, int level, bool critical)
        {
            // Truncate long device names so the toast stays one clean line
            const int maxName = 16;
            string name = deviceLabel;
            if (name.Length > maxName) name = name.Substring(0, maxName - 1).TrimEnd() + "\u2026"; // ellipsis

            string msg = name + ": " + level + "%";

            bool existing = activeToast != null && !activeToast.IsDisposed;
            if (existing)
            {
                activeToast.UpdateMessage(msg);
                activeToast.Critical = critical;
                activeToast.Invalidate();
            }
            else
            {
                activeToast = new BatteryToast(msg);
                activeToast.Critical = critical;
                activeToast.Show();
                PlayAlertSound(critical);
            }
        }

        private string GetProtocolForDevice(ushort vid, string productName)
        {
            if (vid == 0x1038 || (!string.IsNullOrEmpty(productName) && productName.ToLower().Contains("arctis")))
            {
                return "Arctis";
            }
            if (vid == 0x3554 || (!string.IsNullOrEmpty(productName) && (productName.ToLower().Contains("vgn") || productName.ToLower().Contains("vxe") || productName.ToLower().Contains("atk") || productName.ToLower().Contains("compx"))))
            {
                return "VGN";
            }
            return "None";
        }

        public string GetFriendlyDeviceName(string rawName, ushort vid, ushort pid)
        {
            if (vid == 0x3554)
            {
                return "ATK Pro Mouse";
            }
            if (vid == 0x1038)
            {
                return "SteelSeries Arctis Headset";
            }

            if (string.IsNullOrEmpty(rawName)) return "";
            string clean = rawName;
            int idx = clean.IndexOf(" (VID:");
            if (idx >= 0) clean = clean.Substring(0, idx);
            return clean.Trim();
        }

        public class SettingsDeviceItem
        {
            public string DisplayName { get; set; }
            public ushort Vid { get; set; }
            public ushort Pid { get; set; }
            public ushort UsagePage { get; set; }
            public string Protocol { get; set; }
            public string DeviceName { get; set; }
        }

        public Point LastFlyoutPos = Point.Empty;
        public bool HasFlyoutPos = false;

        public void ShowSettingsWindow()
        {
            // Open the dedicated settings window in the SAME position/size as the
            // flyout. If the flyout is open, fade it out simultaneously (settings
            // fades in from the right); on close, flyout fades back in at its spot.
            bool flyoutOpen = activeFlyout != null && !activeFlyout.IsDisposed && activeFlyout.Visible;
            bool knownPos = flyoutOpen || HasFlyoutPos;
            Point pos = flyoutOpen ? activeFlyout.Location : LastFlyoutPos;
            if (!knownPos)
            {
                Point mp = Control.MousePosition;
                pos = new Point(mp.X - 130, mp.Y - 440 - 12);
            }
            Rectangle wa = Screen.FromPoint(pos).WorkingArea;
            if (pos.X < wa.Left + 8) pos.X = wa.Left + 8;
            if (pos.Y < wa.Top + 8) pos.Y = wa.Top + 8;

            // Note: don't close the flyout here — showing the settings window takes
            // focus, which triggers the flyout's own fade-out. True crossfade.
            SettingsWindow sw = new SettingsWindow(this, pos, knownPos, pos);
            sw.Show();
        }

        private string GetStatusLabelText(string position, DeviceConfig config, bool connected, int battery, bool transmitterConnected, bool wired)
        {
            string txt = "Status: ";
            if (config.Protocol == "None") txt += "Disabled";
            else if (connected) txt += "Connected (" + battery + "%)" + (wired ? " [Charging]" : "");
            else if (transmitterConnected) txt += "Powered Off";
            else txt += "Disconnected";
            return txt;
        }

        private void SaveSelectedDevice(ComboBox cb, DeviceConfig config, List<SettingsDeviceItem> deviceItems, string defaultName)
        {
            if (cb.SelectedIndex == 0)
            {
                config.Protocol = "None";
                config.DeviceName = "";
            }
            else
            {
                SettingsDeviceItem sel = deviceItems[cb.SelectedIndex - 1];
                config.Vid = sel.Vid;
                config.Pid = sel.Pid;
                config.UsagePage = sel.UsagePage;
                config.Protocol = sel.Protocol;
                config.DeviceName = sel.DeviceName;
            }
        }

        public List<SettingsDeviceItem> ScanAvailableDevices()
        {
            List<SettingsDeviceItem> deviceItems = new List<SettingsDeviceItem>();
            List<string> seenKeys = new List<string>();

            foreach (var dev in GetConnectedHidDevices())
            {
                if (GetProtocolForDevice(dev.Vid, dev.ProductName) == "None") continue;

                string key = "HID_" + dev.Vid.ToString("X4") + "_" + dev.Pid.ToString("X4");
                if (!seenKeys.Contains(key))
                {
                    seenKeys.Add(key);
                    deviceItems.Add(new SettingsDeviceItem
                    {
                        DisplayName = dev.DisplayName,
                        Vid = dev.Vid,
                        Pid = dev.Pid,
                        UsagePage = dev.UsagePage,
                        Protocol = GetProtocolForDevice(dev.Vid, dev.ProductName),
                        DeviceName = !string.IsNullOrEmpty(dev.ProductName) ? dev.ProductName : "HID Device"
                    });
                }
            }

            foreach (var dev in GetConnectedBluetoothDevices())
            {
                string key = "BT_" + dev.FriendlyName;
                if (!seenKeys.Contains(key))
                {
                    seenKeys.Add(key);
                    string disp = "Bluetooth: " + dev.FriendlyName;
                    if (dev.BatteryLevel >= 0) disp += " (" + dev.BatteryLevel + "%)";

                    deviceItems.Add(new SettingsDeviceItem
                    {
                        DisplayName = disp,
                        Vid = 0,
                        Pid = 0,
                        UsagePage = 0,
                        Protocol = "Bluetooth",
                        DeviceName = dev.FriendlyName
                    });
                }
            }

            return deviceItems;
        }

        // Apply a picker-popup selection to a device config (used by SettingsWindow)
        public void ApplyDeviceSelection(DeviceConfig config, int selectedIndex, List<SettingsDeviceItem> deviceItems)
        {
            if (selectedIndex <= 0)
            {
                config.Protocol = "None";
                config.DeviceName = "";
            }
            else
            {
                SettingsDeviceItem sel = deviceItems[selectedIndex - 1];
                config.Vid = sel.Vid;
                config.Pid = sel.Pid;
                config.UsagePage = sel.UsagePage;
                config.Protocol = sel.Protocol;
                config.DeviceName = sel.DeviceName;
            }
        }

        // Reopen the main flyout at a given position (after settings closes)
        public void ReopenFlyoutAt(Point pos)
        {
            lastClosedTime = DateTime.MinValue; // skip debounce
            activeFlyout = new FlyoutForm(this, pos.X, pos.Y);
            activeFlyout.Show();
        }

        public void ClearActiveFlyout()
        {
            activeFlyout = null;
            lastClosedTime = DateTime.Now;
        }

        public void ShowFlyoutWindow()
        {
            // Reset activeFlyout if it was disposed or hidden behind the scenes
            if (activeFlyout != null && (activeFlyout.IsDisposed || !activeFlyout.Visible))
            {
                activeFlyout = null;
            }

            if (activeFlyout != null)
            {
                activeFlyout.StartCloseAnimation(null);
                activeFlyout = null;
                return;
            }

            // Debounce double-clicks or immediate re-opens from tray click deactivation
            if ((DateTime.Now - lastClosedTime).TotalMilliseconds < 300)
            {
                return;
            }

            int activeCount = (outerConfig.Protocol != "None" ? 1 : 0) + (middleConfig.Protocol != "None" ? 1 : 0) + (innerConfig.Protocol != "None" ? 1 : 0);
            int fWidth = 260;
            int fHeight = 440 - ((3 - activeCount) * 45);

            Point mousePos = Control.MousePosition;
            int x = mousePos.X - (fWidth / 2);
            int y = mousePos.Y - fHeight - 10;

            Rectangle screen = Screen.FromPoint(mousePos).WorkingArea;
            if (x < screen.Left + 10) x = screen.Left + 10;
            if (x + fWidth > screen.Right - 10) x = screen.Right - fWidth - 10;
            if (y < screen.Top + 10) y = screen.Top + 10;
            if (y + fHeight > screen.Bottom - 10) y = screen.Bottom - fHeight - 10;

            activeFlyout = new FlyoutForm(this, x, y);
            LastFlyoutPos = new Point(x, y); // remember for settings window positioning
            HasFlyoutPos = true;
            activeFlyout.Show();
            SetForegroundWindow(activeFlyout.Handle);
            activeFlyout.Activate();
        }

        private ToolStripMenuItem updateMenuItem;

        private void CheckForUpdates()
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "BattStat-Update-Checker");
                    // Don't park a thread indefinitely on a stalled connection
                    wc.DownloadStringCompleted += (s, e) =>
                    {
                        if (e.Error != null || e.Cancelled || string.IsNullOrEmpty(e.Result)) return;
                        try
                        {
                            Match m = Regex.Match(e.Result, "\"tag_name\"\\s*:\\s*\"v?([0-9\\.]+)\"");
                            if (!m.Success) return;

                            string latestVersionStr = m.Groups[1].Value;
                            Version latest = new Version(latestVersionStr);
                            Version current = Assembly.GetExecutingAssembly().GetName().Version;

                            if (latest > current)
                            {
                                if (contextMenu.IsDisposed) return; // app shutting down
                                Action updateUI = () => {
                                    updateMenuItem.Text = "Update Available! (v" + latestVersionStr + ")";
                                    updateMenuItem.Visible = true;
                                    Font old = updateMenuItem.Font;
                                    updateMenuItem.Font = new Font(updateMenuItem.Font, FontStyle.Bold);
                                    if (old != null) old.Dispose();
                                    ShowNotification("Update Available", "BattStat v" + latestVersionStr + " is available! Right-click the tray icon to download.", ToolTipIcon.Info);
                                };

                                if (contextMenu.InvokeRequired)
                                    contextMenu.BeginInvoke(updateUI); // don't block; avoid deadlock on exit
                                else
                                    updateUI();
                            }
                        }
                        catch { }
                    };
                    wc.DownloadStringAsync(new Uri("https://api.github.com/repos/ralphsaniatan/BattStat/releases/latest"));
                }
            }
            catch { /* Ignore network errors */ }
        }

        public void ExitApplication()
        {
            // Close any modal dialog first — Application.Exit() cannot unwind a
            // nested ShowDialog() message loop, which would leave a zombie process
            // holding the single-instance mutex.
            if (settingsForm != null && !settingsForm.IsDisposed)
            {
                settingsForm.Close();
                settingsForm.Dispose();
                settingsForm = null;
            }
            if (activeFlyout != null && !activeFlyout.IsDisposed)
            {
                activeFlyout.Close();
                activeFlyout.Dispose();
                activeFlyout = null;
            }

            timer.Stop();
            timer.Dispose();
            notifyIcon.Visible = false;

            if (notifyIcon.Icon != null)
            {
                DestroyIcon(notifyIcon.Icon.Handle);
                notifyIcon.Icon.Dispose();
            }
            notifyIcon.Dispose();

            Application.Exit();
        }
    }
    // Custom low-battery toast: no banner title, simple message, blinking
    // warning icon, close button. Own window => unaffected by Focus Assist/DND.
    public class BatteryToast : Form
    {
        private Timer blinkTimer;
        private bool blinkOn = true;
        private string message;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public BatteryToast(string msg)
        {
            message = msg;

            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = Color.FromArgb(28, 28, 28);
            SizeForMessage();

            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(wa.Right - this.Width - 12, wa.Bottom - this.Height - 12);

            try
            {
                int attribute = 33; // DWMWA_WINDOW_CORNER_PREFERENCE
                int preference = 2; // round corners
                DwmSetWindowAttribute(this.Handle, attribute, ref preference, sizeof(int));
            }
            catch { }

            Button btnClose = new Button();
            btnClose.Text = "\u2715";
            btnClose.Font = new Font("Segoe UI", 9f);
            btnClose.Size = new Size(26, 24);
            btnClose.Location = new Point(this.Width - 30, 4);
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.ForeColor = Color.FromArgb(150, 150, 155);
            btnClose.Cursor = Cursors.Hand;
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);

            this.Paint += BatteryToast_Paint;

            blinkTimer = new Timer();
            blinkTimer.Interval = 1100;
            blinkTimer.Tick += (s, e) =>
            {
                blinkOn = !blinkOn;
                this.Invalidate(new Rectangle(10, 10, 44, 44));
            };
            blinkTimer.Start();

            autoCloseTimer = new Timer();
            autoCloseTimer.Interval = 15000;
            autoCloseTimer.Tick += (s, e) =>
            {
                autoCloseTimer.Stop();
                this.Close();
            };
            autoCloseTimer.Start();
        }

        private Timer autoCloseTimer;

        public void UpdateMessage(string msg)
        {
            message = msg;
            blinkOn = true;
            SizeForMessage();
            PositionBottomRight();
            this.Invalidate();
        }

        // Fixed compact size — message must be kept short by the caller
        private void SizeForMessage()
        {
            this.Size = new Size(280, 48);
        }

        private void PositionBottomRight()
        {
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(wa.Right - this.Width - 12, wa.Bottom - this.Height - 12);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (blinkTimer != null)
            {
                blinkTimer.Stop();
                blinkTimer.Dispose();
                blinkTimer = null;
            }
            if (autoCloseTimer != null)
            {
                autoCloseTimer.Stop();
                autoCloseTimer.Dispose();
                autoCloseTimer = null;
            }
            base.OnFormClosed(e);
        }

        private void BatteryToast_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            using (Pen p = new Pen(Color.FromArgb(55, 55, 58), 1))
                g.DrawRectangle(p, 0, 0, this.Width - 1, this.Height - 1);

            // measure text first so icon + text share one true centerline
            using (Font f = new Font("Segoe UI", 11.5f))
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(230, 230, 235)))
            {
                SizeF ts = g.MeasureString(message, f);
                float midY = this.Height / 2f;

                float ty = midY - ts.Height / 2f;
                if (ty < 8) ty = 8;
                g.DrawString(message, f, tb, new RectangleF(42, ty, this.Width - 78, this.Height - 10));

                // blinking warning triangle, centered on the same midline
                int iw = 14, ih = 12;
                int ix = 18;
                float fy = midY - ih / 2f;
                Point top = new Point(ix + iw / 2, (int)fy);
                Point bl = new Point(ix, (int)fy + ih);
                Point br = new Point(ix + iw, (int)fy + ih);
                Color cOn = critical ? Color.FromArgb(255, 90, 70) : Color.FromArgb(255, 176, 32);
                Color cOff = critical ? Color.FromArgb(110, 55, 45) : Color.FromArgb(115, 82, 22);
                using (System.Drawing.Drawing2D.GraphicsPath tri = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    tri.AddPolygon(new Point[] { top, bl, br });
                    using (SolidBrush b = new SolidBrush(blinkOn ? cOn : cOff))
                        g.FillPath(b, tri);
                }
                using (Pen pm = new Pen(Color.Black, 0.9f))
                {
                    g.DrawLine(pm, ix + iw / 2f, fy + 1.8f, ix + iw / 2f, fy + 3.8f);
                }
                g.FillEllipse(Brushes.Black, ix + iw / 2f - 0.4f, fy + 4.4f, 0.9f, 0.9f);
            }
        }

        private bool critical = true;
        public bool Critical
        {
            get { return critical; }
            set { critical = value; }
        }
    }

    // Dedicated settings window, styled like the main flyout modal.
    // Fades/slides in from the right; on close it slides back right and,
    // if it was opened from the flyout, the flyout fades back in.
    public class SettingsWindow : Form
    {
        private BatteryMonitorContext context;
        private Timer animTimer;
        private bool isClosing = false;
        private double currentOpacity = 0.0;
        private int xOffset = 36; // start offset to the right, slides left into place
        private Action onClosedCallback = null;
        private Point targetLocation;
        private bool returnToFlyout;
        private Point flyoutReturnPos;
        private bool closeWasSave = false;

        private List<BatteryMonitorContext.SettingsDeviceItem> deviceItems;
        private Label[] slotPills = new Label[3];
        private DeviceConfig[] slotConfigs = new DeviceConfig[3];
        private CheckBox chkStartup, chkUpdate;
        private string startupShortcutPath;
        private bool pickerOpen = false;
        private DateTime pickerClosedTime = DateTime.MinValue;
        private Timer pollTimer;

        // Is any modal picker/popup of ours currently open?
        private bool PopupIsOpen()
        {
            foreach (Form f in Application.OpenForms)
            {
                if (f != this && f.Visible && f.GetType().Name == "PickerPopup") return true;
            }
            return false;
        }
        private Font fontValueShared;
        private Color settingsHostlessBack = Color.FromArgb(28, 28, 28);

        // Rounded-rectangle path helper
        private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                int d = radius * 2;
                System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath();
                gp.AddArc(r.X, r.Y, d, d, 180, 90);
                gp.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                gp.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                gp.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                gp.CloseFigure();
                return gp;
            }
        }

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        // True when the currently focused window belongs to this process
        // (our picker popups, toasts, etc.) — focus shifts between our own
        // windows must not count as "user clicked outside".
        private bool ForegroundIsOurs()
        {
            IntPtr fg = GetForegroundWindow();
            if (fg == IntPtr.Zero) return true; // can't tell — assume ours, don't close
            uint pid;
            GetWindowThreadProcessId(fg, out pid);
            return pid == (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public SettingsWindow(BatteryMonitorContext ctx, Point location, bool fromFlyout, Point flyoutPos)
        {
            context = ctx;
            targetLocation = location;
            returnToFlyout = fromFlyout;
            flyoutReturnPos = flyoutPos;

            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            // Same shape/size as the main flyout: 260 wide, 440 tall for 3 active rings
            int activeCount = (ctx.outerConfig.Protocol != "None" ? 1 : 0) + (ctx.middleConfig.Protocol != "None" ? 1 : 0) + (ctx.innerConfig.Protocol != "None" ? 1 : 0);
            int formHeight = 440 - ((3 - activeCount) * 45);
            this.Size = new Size(260, formHeight);
            this.BackColor = Color.FromArgb(28, 28, 28);
            this.DoubleBuffered = true;
            this.Opacity = 0.0;
            this.Location = new Point(location.X + xOffset, location.Y);

            try
            {
                int attribute = 33; // DWMWA_WINDOW_CORNER_PREFERENCE
                int preference = 2; // round
                DwmSetWindowAttribute(this.Handle, attribute, ref preference, sizeof(int));
            }
            catch { }

            // Owner-drawn UI: single paint surface + hit zones (no child controls)
            this.Paint += SettingsPaint;
            this.MouseMove += Settings_MouseMove;
            this.MouseLeave += Settings_MouseLeave;
            this.MouseClick += Settings_MouseClick;
            ComputeLayout();

            this.Deactivate += (s, e) =>
            {
                // Ignore if focus went to another window of OUR OWN process
                if (ForegroundIsOurs()) return;
                if (!isClosing) StartClose();
            };

            animTimer = new Timer();
            animTimer.Interval = 15;
            animTimer.Tick += AnimTick;
            animTimer.Start();

            // Safety net: poll foreground window — if focus is in another app,
            // close. Covers any missed Deactivate.
            pollTimer = new Timer();
            pollTimer.Interval = 250;
            pollTimer.Tick += (s, e) =>
            {
                if (isClosing) return;
                if (!ForegroundIsOurs()) StartClose();
            };
            pollTimer.Start();

            if (pendingStartup == false && pendingUpdate == false)
            {
                // first open: seed checkbox states from the real config
                startupShortcutPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Microsoft\Windows\Start Menu\Programs\Startup\BattStat.lnk");
                pendingStartup = File.Exists(startupShortcutPath);
                pendingUpdate = context.autoUpdateEnabled;
            }
        }

        private int expandedSlot = -1;
        private bool pendingStartup;
        private bool pendingUpdate;
        private int?[] pendingSelections = new int?[3]; // staged device picks, applied on Save
        private float expandAnim = 1f;   // 0..1 reveal animation for the options list
        private Timer expandAnimTimer;

        // Ring accent colors — same as the flyout rings
        private static readonly Color[] RingColors = new Color[]
        {
            Color.FromArgb(255, 17, 72),   // outer: red/pink
            Color.FromArgb(0, 180, 255),   // middle: cyan
            Color.FromArgb(170, 255, 0)    // inner: lime
        };

        private void StartExpandAnimation()
        {
            expandAnim = 0f;
            if (expandAnimTimer == null)
            {
                expandAnimTimer = new Timer();
                expandAnimTimer.Interval = 15;
                expandAnimTimer.Tick += (s, e) =>
                {
                    expandAnim = Math.Min(1f, expandAnim + 0.12f);
                    this.Invalidate();
                    if (expandAnim >= 1f)
                    {
                        expandAnimTimer.Stop();
                    }
                };
            }
            expandAnimTimer.Start();
        }

        private int CurrentSelectionIndex(DeviceConfig cfg)
        {
            if (deviceItems == null) return 0;
            int current = 0;
            for (int i = 0; i < deviceItems.Count; i++)
            {
                var it = deviceItems[i];
                bool match;
                if (it.Protocol == "Bluetooth")
                    match = cfg.Protocol == "Bluetooth" && it.DeviceName == cfg.DeviceName;
                else
                    match = it.Vid == cfg.Vid && it.Pid == cfg.Pid && cfg.Protocol != "Bluetooth" && cfg.Protocol != "None";
                if (match) { current = i + 1; break; }
            }
            return current;
        }

        private static string TruncateOption(string s)
        {
            if (s.Length <= 24) return s;
            return s.Substring(0, 23).TrimEnd() + "\u2026";
        }

        private void ToggleExpand(int slot)
        {
            bool opening = expandedSlot != slot;
            expandedSlot = (expandedSlot == slot) ? -1 : slot;
            ComputeLayout();
            if (opening) StartExpandAnimation();
            this.Invalidate();
        }


        private List<HitZone> zones = new List<HitZone>();
        private int hoverZone = -1;
        private Font fTitle = new Font("Segoe UI", 11f, FontStyle.Bold);
        private Font fSection = new Font("Segoe UI", 8.25f, FontStyle.Bold);
        private Font fValue = new Font("Segoe UI", 9f);
        private Font fCheck = new Font("Segoe UI", 9f);
        private Font fChevron = new Font("Segoe UI", 9f, FontStyle.Bold);

        private class HitZone
        {
            public Rectangle Rect;
            public string Action; // toggle | option | startup | updates | save
            public int A, B;      // slot / option indices
        }

        private static System.Drawing.Drawing2D.GraphicsPath Rounded(Rectangle r, int rad)
        {
            System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath();
            int d = rad * 2;
            gp.AddArc(r.X, r.Y, d, d, 180, 90);
            gp.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            gp.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            gp.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            gp.CloseFigure();
            return gp;
        }

        private void ComputeLayout()
        {
            zones.Clear();
            hoverZone = -1; // zone list rebuilt — stale index would crash Paint
            int W = this.Width, H = this.Height;

            // Window size NEVER changes — expanded options overlay the bottom bar
            // option hit zones — added FIRST so ZoneAt's reverse scan finds them
            // before the section toggles they overlap
            if (expandedSlot != -1 && deviceItems != null)
            {
                int count = deviceItems.Count + 1;
                int optTop = 54 + expandedSlot * 64 + 20;
                for (int o = 0; o < count; o++)
                    zones.Insert(0, new HitZone { Rect = new Rectangle(0, optTop + o * 30, W, 30), Action = "option", A = expandedSlot, B = o });
            }

            int y2 = 54;
            for (int i = 0; i < 3; i++)
            {
                // hit zone matches the painted section block exactly (y..y+56)
                zones.Add(new HitZone { Rect = new Rectangle(0, y2, W, 56), Action = "toggle", A = i });
                y2 += 64;
            }

            zones.Add(new HitZone { Rect = new Rectangle(12, H - 102, W - 24, 24), Action = "startup" });
            zones.Add(new HitZone { Rect = new Rectangle(12, H - 76, W - 24, 24), Action = "updates" });
            zones.Add(new HitZone { Rect = new Rectangle(16, H - 46, W - 32, 32), Action = "save" });
        }

        private HitZone ZoneAt(Point p)
        {
            // OPTION ZONES ALWAYS WIN — they're the top z-layer when a list is
            // open. Everything else is checked afterwards.
            if (expandedSlot != -1)
            {
                foreach (var z in zones)
                    if (z.Action == "option" && z.Rect.Contains(p)) return z;
                // inside the open list area but not on a row = dead space
                return null;
            }
            for (int i = zones.Count - 1; i >= 0; i--)
                if (zones[i].Rect.Contains(p)) return zones[i];
            return null;
        }

        private void SettingsPaint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int W = this.Width, H = this.Height;
            bool dim = expandedSlot != -1; // list open = rest of UI dimmed + locked
            Color cDimText = Color.FromArgb(105, 105, 110);

            using (Pen pb = new Pen(Color.FromArgb(55, 55, 58), 1))
                g.DrawRectangle(pb, 0, 0, W - 1, H - 1);

            // Title
            using (SolidBrush b = new SolidBrush(dim ? cDimText : Color.White))
                g.DrawString("Settings", fTitle, b, 20, 13);

            // divider under title (full width)
            using (Pen dp = new Pen(Color.FromArgb(48, 48, 52), 1))
                g.DrawLine(dp, 0, 46, W, 46);

            if (deviceItems == null) deviceItems = context.ScanAvailableDevices();

            slotConfigs[0] = context.outerConfig;
            slotConfigs[1] = context.middleConfig;
            slotConfigs[2] = context.innerConfig;
            string[] names = new string[] { "OUTER RING", "MIDDLE RING", "INNER RING" };

            int zi = 0; // zone pointer (zones were laid out in same order)
            int y = 54; // consistent breathing room below the title divider
            int expOptTop = 0, expOptBottom = 0, expLeft = 15, expRight = W - 15;

            for (int i = 0; i < 3; i++)
            {
                DeviceConfig cfg = slotConfigs[i];
                bool exp = i == expandedSlot;
                bool secHot = hoverZone >= 0 && zones[hoverZone] != null && zones[hoverZone].Action == "toggle" && zones[hoverZone].A == i;

                // HOVER BAND FIRST (background layer): covers the full stride
                // INCLUDING this section's divider line, so the highlight is one
                // unbroken block. Text is drawn on top afterwards.
                // The FIRST section's band reaches up to the title divider (46),
                // so there's no unhighlighted gap between them.
                if (secHot)
                {
                    // start at the divider ABOVE this section (y-8) so the band
                    // is bounded divider-to-divider with no unhighlighted strip;
                    // the first section reaches up to the title divider (46)
                    int bandTop = (i == 0) ? 46 : y - 8;
                    using (SolidBrush hb = new SolidBrush(
                        dim ? Color.FromArgb(38, 38, 42) : Color.FromArgb(52, 52, 56)))
                        g.FillRectangle(hb, 0, bandTop, W, (y + 57) - bandTop);
                }

                // divider under section — painted after the band so it stays a
                // crisp visible line even on the hovered row
                using (Pen dp = new Pen(Color.FromArgb(48, 48, 52), 1))
                    g.DrawLine(dp, 0, y + 56, W, y + 56);

                // section label takes its RING's accent color (matches the flyout rings);
                // dims when another section is expanded
                Color ringCol = RingColors[i];
                Color secCol = dim && !exp
                    ? Color.FromArgb(90, 105, 105, 110)
                    : (exp ? ControlPaint.LightLight(ringCol) : ringCol);
                Color valCol = dim && !exp ? cDimText : Color.White;

                // section label
                using (SolidBrush b = new SolidBrush(secCol))
                    g.DrawString(names[i], fSection, b, 20, y + 4);

                // "+" toggle on the right, aligned with the value line
                string plus = exp ? "\u2212" : "+";
                using (SolidBrush b = new SolidBrush(secCol))
                    g.DrawString(plus, fChevron, b, new RectangleF(W - 40, y + 22, 20, 20), new StringFormat { Alignment = StringAlignment.Far });

                // value row — honors any staged (not yet saved) pick
                string val;
                if (pendingSelections[i].HasValue && deviceItems != null)
                {
                    int sel = pendingSelections[i].Value;
                    if (sel <= 0) val = "No device selected (disabled)";
                    else if (sel - 1 < deviceItems.Count) val = deviceItems[sel - 1].DisplayName;
                    else val = "No device selected (disabled)";
                }
                else if (cfg.Protocol == "None") val = "No device selected (disabled)";
                else if (cfg.Protocol == "Bluetooth") val = cfg.DeviceName;
                else val = context.GetFriendlyDeviceName(cfg.DeviceName, cfg.Vid, cfg.Pid);
                using (Font fv = new Font("Segoe UI", 9f))
                using (SolidBrush b = new SolidBrush(valCol))
                    g.DrawString(val, fv, b, 20, y + 22);

                if (exp)
                {
                    // EXPANDED: the options list becomes its own z-layer painted
                    // LAST (see PaintExpandedList) — it OVERLAYS the sections
                    // below; nothing moves or shifts down.
                    y += 64;
                }
                else
                {
                    y += 64;
                }
            }

            // Bottom bar (checkboxes + Save) — painted BEFORE the expanded list
            // so the opaque dropdown covers them when they overlap. ------------
            DrawCheck(g, 18, H - 98, pendingStartup, "Run application at Windows startup", dim);
            DrawCheck(g, 18, H - 74, pendingUpdate, "Check for updates on startup", dim);

            Rectangle sr = new Rectangle(16, H - 46, W - 32, 32);
            bool saveHot = hoverZone >= 0 && zones[hoverZone] != null && zones[hoverZone].Action == "save" && !dim;
            Color sBack = dim ? Color.FromArgb(45, 60, 85) : (saveHot ? Color.FromArgb(25, 135, 225) : Color.FromArgb(0, 120, 212));
            using (System.Drawing.Drawing2D.GraphicsPath gp = Rounded(sr, 6))
            using (SolidBrush b = new SolidBrush(sBack))
                g.FillPath(b, gp);
            using (SolidBrush b = new SolidBrush(dim ? cDimText : Color.White))
            {
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("Save", new Font("Segoe UI", 9f, FontStyle.Bold), b, new RectangleF(sr.X, sr.Y, sr.Width, sr.Height), sf);
            }

            // EXPANDED LIST LAST = highest z-index. Its opaque background covers
            // anything underneath it (checkboxes, save button) while open.
            PaintExpandedList(g, W);
        }

        private void PaintExpandedList(Graphics g, int W)
        {
            if (expandedSlot == -1 || deviceItems == null) return;

            DeviceConfig cfg = slotConfigs[expandedSlot];
            List<string> options = new List<string>();
            options.Add("None / Disable");
            foreach (var it in deviceItems)
            {
                // same friendly naming the flyout uses — kills "SteelSeries SteelSeries…"
                string nm = it.Protocol == "Bluetooth" ? it.DeviceName : context.GetFriendlyDeviceName(it.DeviceName, it.Vid, it.Pid);
                if (string.IsNullOrEmpty(nm)) nm = it.DisplayName;
                options.Add(nm);
            }
            int current = CurrentSelectionIndex(cfg);

            bool anyOptHot = false;
            int hotOptIdx = -1;
            foreach (var z in zones)
            {
                if (z.Action == "option" && zones.IndexOf(z) == hoverZone) { anyOptHot = true; hotOptIdx = z.B; break; }
            }

            // OPAQUE dropdown background — starts right below the section LABEL
            // (covers the device-name line, which is replaced by the list) and
            // sized to just the option rows.
            int optTop = 54 + expandedSlot * 64 + 20;
            int listH = options.Count * 30;
            using (SolidBrush bb = new SolidBrush(Color.FromArgb(28, 28, 28)))
                g.FillRectangle(bb, 1, optTop, W - 2, listH + 2);

            int oy = optTop;
            float reveal = expandAnim;
            for (int o = 0; o < options.Count; o++)
            {
                bool sel = o == current;
                float rowReveal = Math.Max(0f, Math.Min(1f, reveal * options.Count - o));
                bool hot = anyOptHot && o == hotOptIdx;

                // full-width hover band; selected row keeps a stable subtle tint
                if (hot)
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(52, 52, 56)))
                        g.FillRectangle(b, 1, oy, W - 2, 30);
                }
                else if (sel)
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(38, 38, 41)))
                        g.FillRectangle(b, 1, oy, W - 2, 30);
                }

                string label = TruncateOption(options[o]);
                using (Font fv = new Font("Segoe UI", 9f, sel ? FontStyle.Bold : FontStyle.Regular))
                using (SolidBrush b = new SolidBrush(Color.FromArgb((int)(255 * rowReveal), sel ? Color.White : Color.FromArgb(200, 200, 205))))
                    g.DrawString(label, fv, b, new RectangleF(20, oy + 6, W - 60, 20));
                if (sel)
                    using (Font fc = new Font("Segoe MDL2 Assets", 9f))
                    using (SolidBrush b = new SolidBrush(Color.FromArgb((int)(255 * rowReveal), Color.FromArgb(0, 150, 240))))
                        g.DrawString("\uE73E", fc, b, W - 30, oy + 6);

                using (Pen sp = new Pen(Color.FromArgb(40, 40, 44), 1))
                    g.DrawLine(sp, 1, oy + 29, W - 1, oy + 29);

                oy += 30;
            }
        }

        private void DrawCheck(Graphics g, int x, int y, bool checkedVal, string text, bool dim)
        {
            Color txtCol = dim ? Color.FromArgb(105, 105, 110) : Color.White;
            Color boxBack = checkedVal ? Color.FromArgb(0, 120, 212) : Color.FromArgb(45, 45, 48);
            if (dim) boxBack = Color.FromArgb(38, 48, 66);
            Rectangle box = new Rectangle(x, y + 1, 15, 15);
            using (System.Drawing.Drawing2D.GraphicsPath gp = Rounded(box, 3))
            {
                using (SolidBrush b = new SolidBrush(boxBack)) g.FillPath(b, gp);
                using (Pen p = new Pen(checkedVal ? boxBack : Color.FromArgb(90, 90, 95), 1)) g.DrawPath(p, gp);
            }
            if (checkedVal)
            {
                // draw the checkmark as vector lines, centered in the box —
                // no font glyph (E73E renders off-center at small sizes)
                using (Pen ck = new Pen(Color.White, 1.6f))
                {
                    ck.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    ck.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    int cx = box.X + box.Width / 2;
                    int cy = box.Y + box.Height / 2;
                    g.DrawLine(ck, cx - 3, cy + 0, cx - 1, cy + 3);   // down-stroke
                    g.DrawLine(ck, cx - 1, cy + 3, cx + 4, cy - 3);   // up-stroke
                }
            }
            using (SolidBrush b = new SolidBrush(txtCol))
                g.DrawString(text, fCheck, b, x + 24, y);
        }

        private void Settings_MouseMove(object sender, MouseEventArgs e)
        {
            HitZone z = ZoneAt(e.Location);
            // While a selection list is open, ONLY its option rows are hoverable —
            // the dimmed background layer must not react to the mouse.
            if (expandedSlot != -1 && (z == null || z.Action != "option"))
            {
                if (hoverZone != -1) { hoverZone = -1; Cursor = Cursors.Default; this.Invalidate(); }
                return;
            }
            int idx = z != null ? zones.IndexOf(z) : -1;
            Cursor = z != null ? Cursors.Hand : Cursors.Default;
            if (idx != hoverZone) { hoverZone = idx; this.Invalidate(); }
        }

        private void Settings_MouseLeave(object sender, EventArgs e)
        {
            if (hoverZone != -1) { hoverZone = -1; this.Invalidate(); }
        }

        private void Settings_MouseClick(object sender, MouseEventArgs e)
        {
            HitZone z = ZoneAt(e.Location);
            if (z == null)
            {
                // clicking empty space while a list is open closes it
                if (expandedSlot != -1) ToggleExpand(expandedSlot);
                return;
            }
            switch (z.Action)
            {
                case "toggle":
                    // while a list is open, other sections are LOCKED — clicking
                    // them just closes the open list
                    if (expandedSlot != -1 && z.A != expandedSlot)
                    {
                        ToggleExpand(expandedSlot);
                        break;
                    }
                    ToggleExpand(z.A);
                    break;
                case "option":
                    // STAGE the selection — applied to the live config only on Save
                    pendingSelections[expandedSlot] = z.B;
                    expandedSlot = -1;
                    ComputeLayout();
                    this.Invalidate();
                    break;
                case "startup":
                case "updates":
                    // locked while a list is open — ignore clicks
                    if (expandedSlot != -1) break;
                    if (z.Action == "startup") pendingStartup = !pendingStartup;
                    else pendingUpdate = !pendingUpdate;
                    this.Invalidate();
                    break;
                case "save":
                    // locked while a list is open
                    if (expandedSlot != -1) break;
                    SaveAndClose();
                    break;
            }
        }



        private string PillText(DeviceConfig cfg)
        {
            if (cfg.Protocol == "None") return "+  Tap to choose a device";
            if (cfg.Protocol == "Bluetooth") return "\u25CF  " + cfg.DeviceName;
            return "\u25CF  " + context.GetFriendlyDeviceName(cfg.DeviceName, cfg.Vid, cfg.Pid);
        }

        // Pill text for a slot, honoring any staged (not yet saved) device pick
        private string PillTextForSlot(int slot)
        {
            if (pendingSelections[slot].HasValue && deviceItems != null)
            {
                int sel = pendingSelections[slot].Value;
                if (sel <= 0) return "+  Tap to choose a device";
                if (sel - 1 < deviceItems.Count)
                {
                    var it = deviceItems[sel - 1];
                    return "●  " + it.DisplayName;
                }
            }
            return PillText(slotConfigs[slot]);
        }

        private void RescanDevices()
        {
            deviceItems = context.ScanAvailableDevices();
            if (slotPills[0] != null && !slotPills[0].IsDisposed)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (slotPills[i] != null && !slotPills[i].IsDisposed)
                        slotPills[i].Text = PillText(slotConfigs[i]);
                }
            }
        }

        private void ShowPicker(int slotIndex)
        {
            if (deviceItems == null) RescanDevices();

            DeviceConfig cfg = slotConfigs[slotIndex];

            List<string> options = new List<string>();
            options.Add("[ None / Disabled ]");
            foreach (var it in deviceItems) options.Add(it.DisplayName);

            int current = 0;
            for (int i = 0; i < deviceItems.Count; i++)
            {
                var it = deviceItems[i];
                bool match;
                if (it.Protocol == "Bluetooth")
                    match = cfg.Protocol == "Bluetooth" && it.DeviceName == config_DeviceName(cfg) && cfg.Protocol == "Bluetooth";
                else
                    match = it.Vid == cfg.Vid && it.Pid == cfg.Pid && cfg.Protocol != "Bluetooth" && cfg.Protocol != "None";
                if (match) { current = i + 1; break; }
            }

            Point anchor = slotPills[slotIndex].PointToScreen(new Point(4, slotPills[slotIndex].Height + 3));
            PickerPopup popup = new PickerPopup(options, current);

            Rectangle wa = Screen.FromPoint(anchor).WorkingArea;
            if (anchor.Y + popup.Height > wa.Bottom) anchor.Y = wa.Bottom - popup.Height - 4;
            if (anchor.X + popup.Width > wa.Right) anchor.X = wa.Right - popup.Width - 4;
            popup.Location = anchor;

            pickerOpen = true;
            popup.FormClosed += (s, e) =>
            {
                pickerOpen = false;
                pickerClosedTime = DateTime.Now;
                this.Activate(); // retake focus so the flyout-return chain stays intact
            };

            if (popup.ShowDialog() == DialogResult.OK && popup.SelectedIndex >= 0)
            {
                context.ApplyDeviceSelection(cfg, popup.SelectedIndex, deviceItems);
                slotPills[slotIndex].Text = PillText(cfg);
            }
        }

        private static string config_DeviceName(DeviceConfig cfg) { return cfg.DeviceName; }

        private void SaveAndClose()
        {
            closeWasSave = true;
            // apply any staged device selections to the live config before saving
            for (int i = 0; i < 3; i++)
            {
                if (pendingSelections[i].HasValue)
                {
                    context.ApplyDeviceSelection(slotConfigs[i], pendingSelections[i].Value, deviceItems);
                    pendingSelections[i] = null;
                }
            }
            context.autoUpdateEnabled = pendingUpdate;
            context.SaveConfiguration();

            try
            {
                bool exists = File.Exists(startupShortcutPath);
                if (pendingStartup && !exists)
                {
                    string currentExe = Application.ExecutablePath;
                    Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                    dynamic shell = Activator.CreateInstance(shellType);
                    dynamic shortcut = shell.CreateShortcut(startupShortcutPath);
                    shortcut.TargetPath = currentExe;
                    shortcut.WorkingDirectory = Path.GetDirectoryName(currentExe);
                    shortcut.Description = "Universal Headset, Mouse, and Bluetooth Battery Monitor";
                    shortcut.Save();
                }
                else if (!pendingStartup && exists)
                {
                    File.Delete(startupShortcutPath);
                }
            }
            catch { }

            context.UpdateBatteryStatus();
            StartClose();
        }

        public void ForceClose()
        {
            isClosing = true;
            if (animTimer != null) { animTimer.Stop(); animTimer.Dispose(); animTimer = null; }
            if (pollTimer != null) { pollTimer.Stop(); pollTimer.Dispose(); pollTimer = null; }
            if (expandAnimTimer != null) { expandAnimTimer.Stop(); expandAnimTimer.Dispose(); expandAnimTimer = null; }
            this.Close();
            this.Dispose();
        }

        public void StartClose()
        {
            if (isClosing) return;
            isClosing = true;
            // dismissed without Save → discard staged selections
            if (!closeWasSave)
            {
                for (int i = 0; i < 3; i++) pendingSelections[i] = null;
            }
            if (animTimer != null) animTimer.Start();
        }

        private void AnimTick(object sender, EventArgs e)
        {
            if (!isClosing)
            {
                // FADE IN + SLIDE LEFT FROM THE RIGHT
                bool done = true;
                if (currentOpacity < 1.0)
                {
                    currentOpacity = Math.Min(1.0, currentOpacity + 0.08);
                    this.Opacity = currentOpacity;
                    done = false;
                }
                if (xOffset > 0)
                {
                    xOffset = Math.Max(0, xOffset - 4);
                    this.Location = new Point(targetLocation.X + xOffset, targetLocation.Y);
                    done = false;
                }
                if (done) animTimer.Stop();
            }
            else
            {
                // FADE OUT + SLIDE BACK RIGHT
                bool done = true;
                if (currentOpacity > 0.0)
                {
                    currentOpacity = Math.Max(0.0, currentOpacity - 0.08);
                    this.Opacity = currentOpacity;
                    done = false;
                }
                if (xOffset < 36)
                {
                    xOffset = Math.Min(36, xOffset + 4);
                    this.Location = new Point(targetLocation.X + xOffset, targetLocation.Y);
                    done = false;
                }
                if (done)
                {
                    animTimer.Stop();
                    animTimer.Dispose();
                    animTimer = null;
                    if (pollTimer != null) { pollTimer.Stop(); pollTimer.Dispose(); pollTimer = null; }
                    if (expandAnimTimer != null) { expandAnimTimer.Stop(); expandAnimTimer.Dispose(); expandAnimTimer = null; }
                    this.Close();
                    this.Dispose();
                    // Return to the flyout only when settings was explicitly
                    // closed via Save. Outside-click dismissal = stay closed.
                    if (returnToFlyout && closeWasSave)
                    {
                        context.ReopenFlyoutAt(flyoutReturnPos);
                    }
                }
            }
        }

        // Compact themed list popup for device selection
        private class PickerPopup : Form
        {
            public int SelectedIndex = -1;
            private int hoveredIndex = -1;
            private List<string> items;

            [DllImport("dwmapi.dll")]
            private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

            public PickerPopup(List<string> itemNames, int selectedIndex)
            {
                items = itemNames; SelectedIndex = selectedIndex;

                this.FormBorderStyle = FormBorderStyle.None;
                this.ShowInTaskbar = false;
                this.TopMost = true;
                this.StartPosition = FormStartPosition.Manual;
                this.BackColor = Color.FromArgb(28, 28, 28);
                this.Width = 230; // fits inside the settings window width
                // taller rows so text never clips: Segoe UI 9f needs ~24px
                this.Height = items.Count * 26 + 12;

                try
                {
                    int attribute = 33;
                    int preference = 2;
                    DwmSetWindowAttribute(this.Handle, attribute, ref preference, sizeof(int));
                }
                catch { }

                this.Paint += (s, e) =>
                {
                    Graphics g = e.Graphics;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using (Pen p = new Pen(Color.FromArgb(55, 55, 58), 1))
                        g.DrawRectangle(p, 0, 0, this.Width - 1, this.Height - 1);

                    for (int i = 0; i < items.Count; i++)
                    {
                        int ry = 4 + i * 26;
                        bool hov = i == hoveredIndex;
                        if (hov)
                        {
                            using (System.Drawing.Drawing2D.GraphicsPath gp = RoundedPath(new Rectangle(4, ry - 1, this.Width - 8, 24), 5))
                            using (Brush b = new SolidBrush(Color.FromArgb(38, 38, 38)))
                                g.FillPath(b, gp);
                        }
                        bool selected = i == SelectedIndex;
                        // truncate long names so they never overflow
                        string label = items[i];
                        using (Font f = new Font("Segoe UI", 9f))
                        {
                            SizeF ts = g.MeasureString(label, f);
                            while (ts.Width > this.Width - 52 && label.Length > 4)
                            {
                                label = label.Substring(0, label.Length - 2);
                                ts = g.MeasureString(label + "\u2026", f);
                            }
                            if (label != items[i]) label = label + "\u2026";

                            using (Brush tb = new SolidBrush(selected ? Color.FromArgb(0, 150, 240) : Color.FromArgb(210, 210, 215)))
                                g.DrawString(label, selected ? new Font("Segoe UI", 9f, FontStyle.Bold) : f, tb, new RectangleF(14, ry + 2, this.Width - 44, 22));
                        }
                        if (selected)
                        {
                            using (Font f = new Font("Segoe MDL2 Assets", 9f))
                            using (Brush tb = new SolidBrush(Color.FromArgb(0, 150, 240)))
                                g.DrawString("\uE73E", f, tb, this.Width - 26, ry + 3);
                        }
                    }
                };
                this.MouseMove += (s, e) =>
                {
                    int idx = (e.Y - 4) / 26;
                    if (idx >= 0 && idx < items.Count && idx != hoveredIndex) { hoveredIndex = idx; this.Invalidate(); }
                };
                this.MouseLeave += (s, e) => { if (hoveredIndex != -1) { hoveredIndex = -1; this.Invalidate(); } };
                this.MouseClick += (s, e) =>
                {
                    int idx = (e.Y - 4) / 26;
                    if (idx >= 0 && idx < items.Count)
                    {
                        choseItem = true;
                        SelectedIndex = idx;
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                };
            }

            private bool choseItem = false;

            // Classic combobox-dropdown trick: grab mouse capture when shown.
            // Any click OUTSIDE our bounds makes Windows release our capture,
            // which we treat as "dismissed without selecting".
            protected override void OnShown(EventArgs e)
            {
                base.OnShown(e);
                Capture = true;
            }

            protected override void OnMouseCaptureChanged(EventArgs e)
            {
                base.OnMouseCaptureChanged(e);
                // Capture lost without a choice made = clicked outside
                if (!choseItem && Visible && !IsDisposed && !Disposing)
                {
                    SelectedIndex = -1;
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            }

            protected override void OnFormClosed(FormClosedEventArgs e)
            {
                Capture = false;
                base.OnFormClosed(e);
            }

            private static System.Drawing.Drawing2D.GraphicsPath RoundedPath(Rectangle r, int radius)
            {
                System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath();
                int d = radius * 2;
                gp.AddArc(r.X, r.Y, d, d, 180, 90);
                gp.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                gp.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                gp.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                gp.CloseFigure();
                return gp;
            }

            [DllImport("user32.dll")]
            private static extern IntPtr GetForegroundWindow();
        }
    }

    public class FlyoutForm : Form
    {
        private BatteryMonitorContext context;
        private int targetX;
        private int targetY;
        private bool isClosing = false;
        private Timer animTimer;
        private double currentOpacity = 0.0;
        private int currentYOffset = 15;
        private Action onClosedCallback = null;
        private int hoveredIndex = -1; // -1 = none, 0 = outer, 1 = middle, 2 = inner

        private float[] hoverFactors = new float[] { 1f, 1f, 1f };
        private float[] highlightFactors = new float[] { 0f, 0f, 0f };

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public FlyoutForm(BatteryMonitorContext context, int targetX, int targetY)
        {
            this.context = context;
            this.targetX = targetX;
            this.targetY = targetY;

            // Enable double buffering to prevent flickering during hover transitions
            this.DoubleBuffered = true;

            this.Text = "BattStat v1.3.0";
            int activeCount = (context.outerConfig.Protocol != "None" ? 1 : 0) + (context.middleConfig.Protocol != "None" ? 1 : 0) + (context.innerConfig.Protocol != "None" ? 1 : 0);
            int formHeight = 440 - ((3 - activeCount) * 45);
            this.Size = new Size(260, formHeight);
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(28, 28, 28); // Dark charcoal background
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.Opacity = 0.0;
            this.Location = new Point(targetX, targetY + currentYOffset);

            this.Deactivate += (s, e) => StartCloseAnimation(null);
            this.Paint += FlyoutForm_Paint;
            this.MouseMove += FlyoutForm_MouseMove;
            this.MouseLeave += FlyoutForm_MouseLeave;

            // Enable rounded corners on Windows 11 using DWM API
            try
            {
                int attribute = 33; // DWMWA_WINDOW_CORNER_PREFERENCE
                int preference = 2; // DWMWCP_ROUND (standard rounded corners)
                DwmSetWindowAttribute(this.Handle, attribute, ref preference, sizeof(int));
            }
            catch { }

            // Custom borderless action buttons at the bottom-right (aligned using Segoe MDL2 Assets system icons)
            Font fontIcons = new Font("Segoe MDL2 Assets", 10.5f, FontStyle.Regular);

            Button btnRefresh = new Button();
            btnRefresh.Text = "\uE72C";
            btnRefresh.Font = fontIcons;
            btnRefresh.Location = new Point(175, formHeight - 41);
            btnRefresh.Size = new Size(32, 32);
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 40, 40);
            btnRefresh.FlatAppearance.MouseDownBackColor = Color.FromArgb(50, 50, 50);
            btnRefresh.BackColor = Color.Transparent;
            btnRefresh.ForeColor = Color.FromArgb(120, 120, 120);
            btnRefresh.Cursor = Cursors.Hand;
            btnRefresh.Click += (s, e) => {
                context.UpdateBatteryStatus();
                this.Invalidate();
            };
            btnRefresh.MouseEnter += (s, e) => {
                btnRefresh.ForeColor = Color.White;
                this.hoveredIndex = -1; // Clear ring/row highlights when buttons are hovered
            };
            btnRefresh.MouseLeave += (s, e) => btnRefresh.ForeColor = Color.FromArgb(120, 120, 120);
            this.Controls.Add(btnRefresh);

            Button btnSettings = new Button();
            btnSettings.Text = "\uE713";
            btnSettings.Font = fontIcons;
            btnSettings.Location = new Point(210, formHeight - 41);
            btnSettings.Size = new Size(32, 32);
            btnSettings.FlatStyle = FlatStyle.Flat;
            btnSettings.FlatAppearance.BorderSize = 0;
            btnSettings.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 40, 40);
            btnSettings.FlatAppearance.MouseDownBackColor = Color.FromArgb(50, 50, 50);
            btnSettings.BackColor = Color.Transparent;
            btnSettings.ForeColor = Color.FromArgb(120, 120, 120);
            btnSettings.Cursor = Cursors.Hand;
            btnSettings.Click += (s, e) => {
                StartCloseAnimation(() => context.ShowSettingsWindow());
            };
            btnSettings.MouseEnter += (s, e) => {
                btnSettings.ForeColor = Color.White;
                this.hoveredIndex = -1; // Clear ring/row highlights when buttons are hovered
            };
            btnSettings.MouseLeave += (s, e) => btnSettings.ForeColor = Color.FromArgb(120, 120, 120);
            this.Controls.Add(btnSettings);

            // Configure animation timer
            animTimer = new Timer();
            animTimer.Interval = 15;
            animTimer.Tick += AnimTimer_Tick;
            animTimer.Start();
        }

        private void AnimTimer_Tick(object sender, EventArgs e)
        {
            bool needsRepaint = false;

            if (!isClosing)
            {
                // FADE IN & SLIDE UP
                if (currentOpacity < 1.0)
                {
                    currentOpacity += 0.08;
                    if (currentOpacity > 1.0) currentOpacity = 1.0;
                    this.Opacity = currentOpacity;
                }
                if (currentYOffset > 0)
                {
                    currentYOffset -= 2;
                    if (currentYOffset < 0) currentYOffset = 0;
                    this.Location = new Point(targetX, targetY + currentYOffset);
                }

                // Smoothly animate hover and highlight factors
                for (int i = 0; i < 3; i++)
                {
                    float targetHover = 1.0f;
                    float targetHighlight = 0.0f;

                    if (hoveredIndex >= 0)
                    {
                        if (hoveredIndex == i)
                        {
                            targetHover = 1.0f;
                            targetHighlight = 1.0f;
                        }
                        else
                        {
                            targetHover = 0.25f;
                            targetHighlight = 0.0f;
                        }
                    }

                    // Interpolate hover factors (opacity of active elements)
                    if (hoverFactors[i] < targetHover)
                    {
                        hoverFactors[i] = Math.Min(targetHover, hoverFactors[i] + 0.08f);
                        needsRepaint = true;
                    }
                    else if (hoverFactors[i] > targetHover)
                    {
                        hoverFactors[i] = Math.Max(targetHover, hoverFactors[i] - 0.08f);
                        needsRepaint = true;
                    }

                    // Interpolate highlight factors (active row highlight card background)
                    if (highlightFactors[i] < targetHighlight)
                    {
                        highlightFactors[i] = Math.Min(targetHighlight, highlightFactors[i] + 0.12f);
                        needsRepaint = true;
                    }
                    else if (highlightFactors[i] > targetHighlight)
                    {
                        highlightFactors[i] = Math.Max(targetHighlight, highlightFactors[i] - 0.12f);
                        needsRepaint = true;
                    }
                }
            }
            else
            {
                // FADE OUT & SLIDE DOWN
                bool done = true;
                if (currentOpacity > 0.0)
                {
                    currentOpacity -= 0.08;
                    if (currentOpacity < 0.0) currentOpacity = 0.0;
                    this.Opacity = currentOpacity;
                    done = false;
                }
                if (currentYOffset < 15)
                {
                    currentYOffset += 2;
                    if (currentYOffset > 15) currentYOffset = 15;
                    this.Location = new Point(targetX, targetY + currentYOffset);
                    done = false;
                }

                if (done)
                {
                    animTimer.Stop();
                    animTimer.Dispose();
                    this.Close();
                    this.Dispose();
                    if (onClosedCallback != null)
                    {
                        onClosedCallback();
                    }
                }
            }

            if (needsRepaint)
            {
                this.Invalidate();
            }
        }

        public void StartCloseAnimation(Action callback)
        {
            if (isClosing) return;
            isClosing = true;
            context.ClearActiveFlyout();
            onClosedCallback = callback;
            animTimer.Start();
        }

        private void FlyoutForm_MouseMove(object sender, MouseEventArgs e)
        {
            int newHoveredIndex = -1;
            int x = e.X;
            int y = e.Y;

            // 1. Check if hovering device list rows
            if (x >= 0 && x <= this.Width)
            {
                int currY = 255;
                if (context.outerConfig.Protocol != "None")
                {
                    if (y >= currY && y < currY + 45) newHoveredIndex = 0;
                    currY += 45;
                }
                if (context.middleConfig.Protocol != "None")
                {
                    if (y >= currY && y < currY + 45) newHoveredIndex = 1;
                    currY += 45;
                }
                if (context.innerConfig.Protocol != "None")
                {
                    if (y >= currY && y < currY + 45) newHoveredIndex = 2;
                    currY += 45;
                }
            }

            // 2. Check if hovering activity rings
            if (newHoveredIndex == -1)
            {
                float cx = this.Width / 2f;
                float cy = 150f;
                double dist = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));

                if (dist >= 74 && dist <= 90)
                {
                    if (context.outerConfig.Protocol != "None") newHoveredIndex = 0;
                }
                else if (dist >= 54 && dist <= 70)
                {
                    if (context.middleConfig.Protocol != "None") newHoveredIndex = 1;
                }
                else if (dist >= 34 && dist <= 50)
                {
                    if (context.innerConfig.Protocol != "None") newHoveredIndex = 2;
                }
            }

            if (newHoveredIndex != hoveredIndex)
            {
                hoveredIndex = newHoveredIndex;
            }
        }

        private void FlyoutForm_MouseLeave(object sender, EventArgs e)
        {
            if (hoveredIndex != -1)
            {
                hoveredIndex = -1;
            }
        }

        private void FlyoutForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Draw border
            using (Pen borderPen = new Pen(Color.FromArgb(50, 50, 50), 1))
            {
                g.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
            }

            // --- HEADER --- (font scale unified with the settings window)
            using (Font fontTitle = new Font("Segoe UI", 11f, FontStyle.Bold))
            using (Font fontVersion = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (Brush titleBrush = new SolidBrush(Color.FromArgb(170, 170, 170)))
            {
                g.DrawString("BattStat", fontTitle, titleBrush, 20, 14);
            }

            using (Font fontVersion2 = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (Brush verBrush = new SolidBrush(Color.FromArgb(90, 90, 90)))
            {
                using (StringFormat sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Far;
                    g.DrawString("v1.2.3", fontVersion2, verBrush, new RectangleF(150, 16, 90, 20), sf);
                }
            }

            // --- LARGE CONCENTRIC ACTIVITY RINGS ---
            float cx = this.Width / 2f;
            float cy = 150f;

            int activeCount = (context.outerConfig.Protocol != "None" ? 1 : 0) + 
                              (context.middleConfig.Protocol != "None" ? 1 : 0) + 
                              (context.innerConfig.Protocol != "None" ? 1 : 0);
            float penW = activeCount == 1 ? 32f : (activeCount == 2 ? 24f : 16f);
            
            int currentIdx = 0;
            Func<DeviceConfig, float> GetRadius = (cfg) => {
                if (cfg.Protocol == "None") return 0f;
                float r = 0f;
                if (activeCount == 1) r = 62f;
                else if (activeCount == 2) r = (currentIdx == 0) ? 74f : 46f;
                else r = (currentIdx == 0) ? 82f : (currentIdx == 1 ? 62f : 42f);
                currentIdx++;
                return r;
            };

            // Outer Ring (Red/Pink)
            Color outerBase = Color.FromArgb(255, 17, 72);
            DrawLargeRing(g, context.outerConfig, context.LastOuterConnected, context.LastOuterBattery, cx, cy, GetRadius(context.outerConfig), penW, outerBase, hoverFactors[0]);

            // Middle Ring (Cyan/Blue)
            Color middleBase = Color.FromArgb(0, 180, 255);
            DrawLargeRing(g, context.middleConfig, context.LastMiddleConnected, context.LastMiddleBattery, cx, cy, GetRadius(context.middleConfig), penW, middleBase, hoverFactors[1]);

            // Inner Ring (Lime Green)
            Color innerBase = Color.FromArgb(170, 255, 0);
            DrawLargeRing(g, context.innerConfig, context.LastInnerConnected, context.LastInnerBattery, cx, cy, GetRadius(context.innerConfig), penW, innerBase, hoverFactors[2]);

            // --- DIVIDERS & ROWS ---
            int startY = 255;
            using (Pen divPen = new Pen(Color.FromArgb(40, 40, 40), 1))
            {
                g.DrawLine(divPen, 0, startY, this.Width, startY);
                
                if (context.outerConfig.Protocol != "None")
                {
                    DrawHoverRowBackground(g, startY, highlightFactors[0]);
                    string outerRaw = !string.IsNullOrEmpty(context.LastOuterDeviceName) ? context.LastOuterDeviceName : 
                                      (!string.IsNullOrEmpty(context.outerConfig.DeviceName) ? context.outerConfig.DeviceName : "Outer Ring");
                    string outerName = context.GetFriendlyDeviceName(outerRaw, context.outerConfig.Vid, context.outerConfig.Pid);
                    if (string.IsNullOrEmpty(outerName)) outerName = "Outer Ring";
                    DrawDeviceRow(g, startY, outerName, context.LastOuterConnected, context.LastOuterBattery, outerBase, context.LastOuterWired, hoverFactors[0], highlightFactors[0]);
                    startY += 45;
                    g.DrawLine(divPen, 0, startY, this.Width, startY);
                }

                if (context.middleConfig.Protocol != "None")
                {
                    DrawHoverRowBackground(g, startY, highlightFactors[1]);
                    string middleRaw = !string.IsNullOrEmpty(context.LastMiddleDeviceName) ? context.LastMiddleDeviceName : 
                                       (!string.IsNullOrEmpty(context.middleConfig.DeviceName) ? context.middleConfig.DeviceName : "Middle Ring");
                    string middleName = context.GetFriendlyDeviceName(middleRaw, context.middleConfig.Vid, context.middleConfig.Pid);
                    if (string.IsNullOrEmpty(middleName)) middleName = "Middle Ring";
                    DrawDeviceRow(g, startY, middleName, context.LastMiddleConnected, context.LastMiddleBattery, middleBase, context.LastMiddleWired, hoverFactors[1], highlightFactors[1]);
                    startY += 45;
                    g.DrawLine(divPen, 0, startY, this.Width, startY);
                }

                if (context.innerConfig.Protocol != "None")
                {
                    DrawHoverRowBackground(g, startY, highlightFactors[2]);
                    string innerRaw = !string.IsNullOrEmpty(context.LastInnerDeviceName) ? context.LastInnerDeviceName : 
                                       (!string.IsNullOrEmpty(context.innerConfig.DeviceName) ? context.innerConfig.DeviceName : "Inner Ring");
                    string innerName = context.GetFriendlyDeviceName(innerRaw, context.innerConfig.Vid, context.innerConfig.Pid);
                    if (string.IsNullOrEmpty(innerName)) innerName = "Inner Ring";
                    DrawDeviceRow(g, startY, innerName, context.LastInnerConnected, context.LastInnerBattery, innerBase, context.LastInnerWired, hoverFactors[2], highlightFactors[2]);
                    startY += 45;
                    g.DrawLine(divPen, 0, startY, this.Width, startY);
                }
            }
        }

        private void DrawHoverRowBackground(Graphics g, int yStart, float highlightFactor)
        {
            if (highlightFactor <= 0f) return;
            int alpha = (int)(255 * highlightFactor);
            if (alpha > 255) alpha = 255;
            using (Brush hoverBrush = new SolidBrush(Color.FromArgb(alpha, 38, 38, 38)))
            {
                g.FillRectangle(hoverBrush, 1, yStart + 1, this.Width - 2, 43);
            }
        }

        private void DrawLargeRing(Graphics g, DeviceConfig config, bool connected, int battery, float cx, float cy, float radius, float penWidth, Color ringColor, float hoverFactor)
        {
            if (config.Protocol == "None") return;

            float size = radius * 2;
            float x = cx - radius;
            float y = cy - radius;

            // Background dark circle track (renders in a low-opacity shade of the active ring color)
            int trackAlpha = (int)(35 * hoverFactor);
            using (Pen penBg = new Pen(Color.FromArgb(trackAlpha, ringColor), penWidth))
            {
                g.DrawEllipse(penBg, x, y, size, size);
            }

            // Active status arc
            if (connected && battery >= 0)
            {
                Color activeCol = ringColor;
                if (battery <= 25) activeCol = Color.FromArgb(231, 76, 60);

                int arcAlpha = (int)(255 * hoverFactor);
                using (Pen penActive = new Pen(Color.FromArgb(arcAlpha, activeCol), penWidth))
                {
                    penActive.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                    penActive.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                    float sweep = 360f * (battery / 100f);
                    if (sweep > 0)
                    {
                        g.DrawArc(penActive, x, y, size, size, -90f, sweep);
                    }
                }
            }
        }

        private void DrawDeviceRow(Graphics g, int yStart, string label, bool connected, int battery, Color themeColor, bool wired, float hoverFactor, float highlightFactor)
        {
            using (Font fontLabel = new Font("Segoe UI", 9f, FontStyle.Regular))
            using (Font fontStatus = new Font("Segoe UI", 9f, FontStyle.Regular))
            {
            int yCenter = yStart + 22;

            // Draw colored indicator dot (smoothly fades with hoverFactor)
            int dotAlpha = (int)(80 + (hoverFactor - 0.25f) / 0.75f * 175);
            Color dotColor = connected ? Color.FromArgb(dotAlpha, themeColor) : Color.FromArgb(dotAlpha, 80, 80, 80);
            using (Brush dotBrush = new SolidBrush(dotColor))
            {
                g.FillEllipse(dotBrush, 24, yCenter - 4, 8, 8);
            }

            // Truncate label if too long
            string displayLabel = label;
            if (displayLabel.Length > 20)
            {
                displayLabel = displayLabel.Substring(0, 17) + "...";
            }

            // Determine text colors based on highlight and hover factors
            // Text alpha transitions smoothly between:
            // - 180 (normal, hoverFactor=1.0, highlight=0.0)
            // - 255 (active, hoverFactor=1.0, highlight=1.0)
            // - 65 (faded, hoverFactor=0.25, highlight=0.0)
            int textAlpha = (int)(65 + (hoverFactor - 0.25f) / 0.75f * 115 + highlightFactor * 75);
            if (textAlpha > 255) textAlpha = 255;
            if (textAlpha < 0) textAlpha = 0;

            int statusAlpha = (int)(50 + (hoverFactor - 0.25f) / 0.75f * 50 + highlightFactor * 50);
            if (statusAlpha > 255) statusAlpha = 255;
            if (statusAlpha < 0) statusAlpha = 0;

            // Draw label text
            using (Brush textBrush = new SolidBrush(Color.FromArgb(textAlpha, 255, 255, 255)))
            {
                g.DrawString(displayLabel, fontLabel, textBrush, 45, yCenter - 9);
            }

            // Draw status text right-aligned
            string statusText = connected ? (battery >= 0 ? battery + "%" : "No Battery Data") : "Disconnected";
            if (connected && battery >= 0 && wired) 
            {
                statusText += " (Charging)";
            }

            using (Brush statusBrush = new SolidBrush(Color.FromArgb(statusAlpha, 255, 255, 255)))
            {
                using (StringFormat sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Far; // Right aligned
                    g.DrawString(statusText, fontStatus, statusBrush, new RectangleF(110, yCenter - 8, 130, 20), sf);
                }
            }
            }
        }
    }
}
