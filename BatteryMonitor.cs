using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

[assembly: AssemblyTitle("Arctis & VGN Battery Monitor")]
[assembly: AssemblyDescription("Lightweight tray battery monitor")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Antigravity")]
[assembly: AssemblyProduct("Battery Monitor")]
[assembly: AssemblyCopyright("Copyright © 2026")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyVersion("1.2.1.0")]
[assembly: AssemblyFileVersion("1.2.1.0")]

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
            notifyIcon.Text = "Battery Monitor";
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
            contextMenu.Items.Add("Settings", null, (s, e) => ShowSettingsWindow());
            contextMenu.Items.Add("Refresh", null, (s, e) => UpdateBatteryStatus());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Exit", null, (s, e) => ExitApplication());
            notifyIcon.ContextMenuStrip = contextMenu;

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
                }
            }
            catch { }
        }

        private void SaveConfiguration()
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
                                        battery = readBuf[2];
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
                else if (config.Protocol == "VGN")
                {
                    byte[] writeBuf = new byte[17];
                    writeBuf[0] = 8;
                    writeBuf[1] = 4;
                    writeBuf[16] = 73;

                    uint written;
                    if (WriteFile(hDevice, writeBuf, (uint)writeBuf.Length, out written, IntPtr.Zero))
                    {
                        byte[] readBuf = new byte[17];
                        uint read;
                        if (ReadFile(hDevice, readBuf, (uint)readBuf.Length, out read, IntPtr.Zero))
                        {
                            battery = readBuf[6];
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
                                battery = readBuf[config.CustomBatteryIndex];
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

        private void RunBatteryWarnings(string label, bool connected, int battery, ref bool warned10, ref bool warned25, ref bool warnedHealth, List<BatteryHistoryEntry> history)
        {
            if (!connected || battery < 0)
            {
                warned10 = false;
                warned25 = false;
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

            // Low battery checks
            if (battery <= 10)
            {
                if (!warned10)
                {
                    warned10 = true;
                    warned25 = true;
                    ShowNotification(label + " Device Critical", "Battery level is at " + battery + "%. Please charge.", ToolTipIcon.Warning);
                }
            }
            else if (battery <= 25)
            {
                if (!warned25)
                {
                    warned25 = true;
                    ShowNotification(label + " Device Low", "Battery level is at " + battery + "%.", ToolTipIcon.Info);
                }
                warned10 = false;
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
                notifyIcon.ShowBalloonTip(5000, title, text, iconType);
            }
            catch { }
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

        public void ShowSettingsWindow()
        {
            if (settingsForm != null && settingsForm.Visible)
            {
                settingsForm.Focus();
                return;
            }

            settingsForm = new Form();
            settingsForm.Text = "Universal Battery Monitor Settings";
            settingsForm.Size = new Size(390, 440); // Height increased to 440
            settingsForm.StartPosition = FormStartPosition.CenterScreen;
            settingsForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            settingsForm.MaximizeBox = false;
            settingsForm.MinimizeBox = false;
            settingsForm.BackColor = Color.FromArgb(25, 25, 25);
            settingsForm.ForeColor = Color.White;

            Font fontTitle = new Font("Segoe UI", 12f, FontStyle.Bold);
            Font fontLabel = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            Font fontSub = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            Font fontButton = new Font("Segoe UI", 9, FontStyle.Regular);

            // Title
            Label lblTitle = new Label();
            lblTitle.Text = "Universal Device Tracking Settings";
            lblTitle.Font = fontTitle;
            lblTitle.Location = new Point(15, 15);
            lblTitle.Size = new Size(360, 25);
            settingsForm.Controls.Add(lblTitle);

            // Fetch connected USB HID devices
            List<HidDeviceMetadata> connectedHid = GetConnectedHidDevices();
            List<SettingsDeviceItem> deviceItems = new List<SettingsDeviceItem>();
            List<string> seenKeys = new List<string>();

            foreach (var dev in connectedHid)
            {
                if (GetProtocolForDevice(dev.Vid, dev.ProductName) == "None")
                {
                    continue;
                }

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

            // Fetch connected Bluetooth devices
            List<BluetoothDeviceMetadata> connectedBt = GetConnectedBluetoothDevices();
            foreach (var dev in connectedBt)
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

            // --- OUTER RING CONFIG ---
            Label lblOuter = new Label();
            lblOuter.Text = "Outer Ring:";
            lblOuter.Font = fontLabel;
            lblOuter.Location = new Point(20, 50);
            lblOuter.Size = new Size(150, 20);
            settingsForm.Controls.Add(lblOuter);

            ComboBox cbOuterDevice = new ComboBox();
            cbOuterDevice.DropDownStyle = ComboBoxStyle.DropDownList;
            cbOuterDevice.Font = fontLabel;
            cbOuterDevice.Location = new Point(20, 75);
            cbOuterDevice.Size = new Size(345, 25);
            cbOuterDevice.BackColor = Color.FromArgb(50, 50, 50);
            cbOuterDevice.ForeColor = Color.White;
            cbOuterDevice.FlatStyle = FlatStyle.Flat;
            cbOuterDevice.Items.Add("[ None / Disabled ]");
            int selectedOuterIndex = 0;
            for (int i = 0; i < deviceItems.Count; i++)
            {
                cbOuterDevice.Items.Add(deviceItems[i].DisplayName);
                if (deviceItems[i].Protocol == "Bluetooth")
                {
                    if (outerConfig.Protocol == "Bluetooth" && deviceItems[i].DeviceName == outerConfig.DeviceName)
                    {
                        selectedOuterIndex = i + 1;
                    }
                }
                else
                {
                    if (deviceItems[i].Vid == outerConfig.Vid && deviceItems[i].Pid == outerConfig.Pid && outerConfig.Protocol != "Bluetooth")
                    {
                        selectedOuterIndex = i + 1;
                    }
                }
            }
            cbOuterDevice.SelectedIndex = selectedOuterIndex;
            settingsForm.Controls.Add(cbOuterDevice);

            Label lblOuterStatus = new Label();
            lblOuterStatus.Font = fontSub;
            lblOuterStatus.Location = new Point(20, 105);
            lblOuterStatus.Size = new Size(340, 20);
            settingsForm.Controls.Add(lblOuterStatus);

            // --- MIDDLE RING CONFIG ---
            Label lblMiddle = new Label();
            lblMiddle.Text = "Middle Ring:";
            lblMiddle.Font = fontLabel;
            lblMiddle.Location = new Point(20, 130);
            lblMiddle.Size = new Size(150, 20);
            settingsForm.Controls.Add(lblMiddle);

            ComboBox cbMiddleDevice = new ComboBox();
            cbMiddleDevice.DropDownStyle = ComboBoxStyle.DropDownList;
            cbMiddleDevice.Font = fontLabel;
            cbMiddleDevice.Location = new Point(20, 155);
            cbMiddleDevice.Size = new Size(345, 25);
            cbMiddleDevice.BackColor = Color.FromArgb(50, 50, 50);
            cbMiddleDevice.ForeColor = Color.White;
            cbMiddleDevice.FlatStyle = FlatStyle.Flat;
            cbMiddleDevice.Items.Add("[ None / Disabled ]");
            int selectedMiddleIndex = 0;
            for (int i = 0; i < deviceItems.Count; i++)
            {
                cbMiddleDevice.Items.Add(deviceItems[i].DisplayName);
                if (deviceItems[i].Protocol == "Bluetooth")
                {
                    if (middleConfig.Protocol == "Bluetooth" && deviceItems[i].DeviceName == middleConfig.DeviceName)
                    {
                        selectedMiddleIndex = i + 1;
                    }
                }
                else
                {
                    if (deviceItems[i].Vid == middleConfig.Vid && deviceItems[i].Pid == middleConfig.Pid && middleConfig.Protocol != "Bluetooth")
                    {
                        selectedMiddleIndex = i + 1;
                    }
                }
            }
            cbMiddleDevice.SelectedIndex = selectedMiddleIndex;
            settingsForm.Controls.Add(cbMiddleDevice);

            Label lblMiddleStatus = new Label();
            lblMiddleStatus.Font = fontSub;
            lblMiddleStatus.Location = new Point(20, 185);
            lblMiddleStatus.Size = new Size(340, 20);
            settingsForm.Controls.Add(lblMiddleStatus);

            // --- INNER RING CONFIG ---
            Label lblInner = new Label();
            lblInner.Text = "Inner Ring:";
            lblInner.Font = fontLabel;
            lblInner.Location = new Point(20, 210);
            lblInner.Size = new Size(150, 20);
            settingsForm.Controls.Add(lblInner);

            ComboBox cbInnerDevice = new ComboBox();
            cbInnerDevice.DropDownStyle = ComboBoxStyle.DropDownList;
            cbInnerDevice.Font = fontLabel;
            cbInnerDevice.Location = new Point(20, 235);
            cbInnerDevice.Size = new Size(345, 25);
            cbInnerDevice.BackColor = Color.FromArgb(50, 50, 50);
            cbInnerDevice.ForeColor = Color.White;
            cbInnerDevice.FlatStyle = FlatStyle.Flat;
            cbInnerDevice.Items.Add("[ None / Disabled ]");
            int selectedInnerIndex = 0;
            for (int i = 0; i < deviceItems.Count; i++)
            {
                cbInnerDevice.Items.Add(deviceItems[i].DisplayName);
                if (deviceItems[i].Protocol == "Bluetooth")
                {
                    if (innerConfig.Protocol == "Bluetooth" && deviceItems[i].DeviceName == innerConfig.DeviceName)
                    {
                        selectedInnerIndex = i + 1;
                    }
                }
                else
                {
                    if (deviceItems[i].Vid == innerConfig.Vid && deviceItems[i].Pid == innerConfig.Pid && innerConfig.Protocol != "Bluetooth")
                    {
                        selectedInnerIndex = i + 1;
                    }
                }
            }
            cbInnerDevice.SelectedIndex = selectedInnerIndex;
            settingsForm.Controls.Add(cbInnerDevice);

            Label lblInnerStatus = new Label();
            lblInnerStatus.Font = fontSub;
            lblInnerStatus.Location = new Point(20, 265);
            lblInnerStatus.Size = new Size(340, 20);
            settingsForm.Controls.Add(lblInnerStatus);

            // Update Label Text function
            Action updateLabels = () =>
            {
                lblOuterStatus.Text = GetStatusLabelText("Outer", outerConfig, LastOuterConnected, LastOuterBattery, LastOuterTransmitterConnected, LastOuterWired);
                lblMiddleStatus.Text = GetStatusLabelText("Middle", middleConfig, LastMiddleConnected, LastMiddleBattery, false, LastMiddleWired);
                lblInnerStatus.Text = GetStatusLabelText("Inner", innerConfig, LastInnerConnected, LastInnerBattery, false, LastInnerWired);
            };

            updateLabels();

            Label lblDiv = new Label();
            lblDiv.BackColor = Color.FromArgb(60, 60, 60);
            lblDiv.Location = new Point(15, 295);
            lblDiv.Size = new Size(340, 1);
            settingsForm.Controls.Add(lblDiv);

            CheckBox chkStartup = new CheckBox();
            chkStartup.Text = "Run application at Windows Startup";
            chkStartup.Font = fontLabel;
            chkStartup.Location = new Point(20, 305);
            chkStartup.Size = new Size(340, 25);

            string startupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\Startup");
            string shortcutPath = Path.Combine(startupFolder, "BattStat.lnk");
            chkStartup.Checked = File.Exists(shortcutPath);
            settingsForm.Controls.Add(chkStartup);

            // Action Buttons
            Button btnRefresh = new Button();
            btnRefresh.Text = "Refresh Scan";
            btnRefresh.Font = fontButton;
            btnRefresh.Location = new Point(20, 350);
            btnRefresh.Size = new Size(130, 30);
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            btnRefresh.Cursor = Cursors.Hand;
            btnRefresh.Click += (s, e) =>
            {
                UpdateBatteryStatus();
                updateLabels();
            };
            settingsForm.Controls.Add(btnRefresh);

            Button btnClose = new Button();
            btnClose.Text = "Save & Close";
            btnClose.Font = fontButton;
            btnClose.Location = new Point(230, 350);
            btnClose.Size = new Size(125, 30);
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            btnClose.Cursor = Cursors.Hand;
            btnClose.Click += (s, e) =>
            {
                // Save Outer
                SaveSelectedDevice(cbOuterDevice, outerConfig, deviceItems, "Outer Ring");
                // Save Middle
                SaveSelectedDevice(cbMiddleDevice, middleConfig, deviceItems, "Middle Ring");
                // Save Inner
                SaveSelectedDevice(cbInnerDevice, innerConfig, deviceItems, "Inner Ring");

                SaveConfiguration();

                // Handle startup checkbox
                bool exists = File.Exists(shortcutPath);
                if (chkStartup.Checked && !exists)
                {
                    try
                    {
                        string currentExe = Application.ExecutablePath;
                        string workingDir = Path.GetDirectoryName(currentExe);
                        Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                        dynamic shell = Activator.CreateInstance(shellType);
                        dynamic shortcut = shell.CreateShortcut(shortcutPath);
                        shortcut.TargetPath = currentExe;
                        shortcut.WorkingDirectory = workingDir;
                        shortcut.Description = "Universal Headset, Mouse, and Bluetooth Battery Monitor";
                        shortcut.Save();
                    }
                    catch { }
                }
                else if (!chkStartup.Checked && exists)
                {
                    File.Delete(shortcutPath);
                }

                UpdateBatteryStatus();
                settingsForm.Close();
            };
            settingsForm.Controls.Add(btnClose);

            settingsForm.ShowDialog();
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
            activeFlyout.Show();
            SetForegroundWindow(activeFlyout.Handle);
            activeFlyout.Activate();
        }

        public void ExitApplication()
        {
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

            this.Text = "BattStat v1.2.2";
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

            // --- HEADER ---
            Font fontTitle = new Font("Segoe UI", 10f, FontStyle.Regular);
            Font fontVersion = new Font("Segoe UI", 8.5f, FontStyle.Regular);

            using (Brush titleBrush = new SolidBrush(Color.FromArgb(170, 170, 170)))
            {
                g.DrawString("BattStat", fontTitle, titleBrush, 20, 14);
            }

            using (Brush verBrush = new SolidBrush(Color.FromArgb(90, 90, 90)))
            {
                using (StringFormat sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Far;
                    g.DrawString("v1.2.2", fontVersion, verBrush, new RectangleF(150, 16, 90, 20), sf);
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
            Font fontLabel = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            Font fontStatus = new Font("Segoe UI", 9f, FontStyle.Regular);

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
