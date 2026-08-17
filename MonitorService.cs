using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MultiMonitorUtility.Services;

public sealed class MonitorService
{
    private const int MONITORINFOF_PRIMARY = 0x00000001;

    public IReadOnlyList<MonitorInfo> GetMonitors()
    {
        var monitors = new List<MonitorInfo>();

        EnumDisplayMonitors(
            IntPtr.Zero,
            IntPtr.Zero,
            (hMonitor, _, _, _) =>
            {
                var info = new MONITORINFOEX();
                info.cbSize = Marshal.SizeOf<MONITORINFOEX>();

                if (GetMonitorInfo(hMonitor, ref info))
                {
                    uint dpi = QueryMonitorDpi(hMonitor);

                    monitors.Add(new MonitorInfo
                    {
                        Handle = hMonitor,
                        DeviceName = info.szDevice,
                        IsPrimary =
                            (info.dwFlags & MONITORINFOF_PRIMARY) != 0,

                        MonitorArea = info.rcMonitor,
                        WorkArea = info.rcWork,

                        Dpi = dpi
                    });
                }

                return true;
            },
            IntPtr.Zero);

        return monitors;
    }

    private delegate bool MonitorEnumProc(
        IntPtr hMonitor,
        IntPtr hdcMonitor,
        IntPtr lprcMonitor,
        IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(
        IntPtr hdc,
        IntPtr lprcClip,
        MonitorEnumProc lpfnEnum,
        IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(
        IntPtr hMonitor,
        ref MONITORINFOEX lpmi);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    public sealed class MonitorInfo
    {
        public uint Dpi { get; init; }
        
        public IntPtr Handle { get; init; }

        public string DeviceName { get; init; } = string.Empty;

        public bool IsPrimary { get; init; }

        public RECT MonitorArea { get; init; }

        public RECT WorkArea { get; init; }

        public int Width =>
            MonitorArea.Right - MonitorArea.Left;

        public int Height =>
            MonitorArea.Bottom - MonitorArea.Top;

        public int WorkWidth =>
            WorkArea.Right - WorkArea.Left;

        public int WorkHeight =>
            WorkArea.Bottom - WorkArea.Top;
    }

    private static uint QueryMonitorDpi(IntPtr hMonitor)
    {
        try
        {
            int result = GetDpiForMonitor(
                hMonitor,
                DpiType.Effective,
                out uint dpiX,
                out _);

            return result == 0 ? dpiX : 96;
        }
        catch
        {
            return 96;
        }
    }

    [DllImport("Shcore.dll")]
    private static extern int GetDpiForMonitor(
        IntPtr hmonitor,
        DpiType dpiType,
        out uint dpiX,
        out uint dpiY);

    private enum DpiType
    {
        Effective = 0,
        Angular = 1,
        Raw = 2
    }
}