using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using MultiMonitorUtility.Services;

namespace MultiMonitorUtility;

public partial class SecondaryBarWindow : Window
{
    private const int BarHeight = 48;

    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    private static readonly IntPtr HWND_TOPMOST = new(-1);

    private readonly MonitorService.MonitorInfo _monitor;

    public SecondaryBarWindow(MonitorService.MonitorInfo monitor)
    {
        InitializeComponent();

        _monitor = monitor;

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PositionWindow();
    }

    private void PositionWindow()
    {
        var hwnd = new WindowInteropHelper(this).Handle;

        int left = _monitor.MonitorArea.Left;
        int top = _monitor.MonitorArea.Bottom - BarHeight;

        int width =
            _monitor.MonitorArea.Right -
            _monitor.MonitorArea.Left;

        int height = BarHeight;

        bool result = SetWindowPos(
            hwnd,
            HWND_TOPMOST,
            left,
            top,
            width,
            height,
            SWP_NOACTIVATE | SWP_SHOWWINDOW);

        GetWindowRect(hwnd, out RECT actual);

        DebugText.Text =
            $"{_monitor.DeviceName}\n" +
            $"Requested: X={left}, Y={top}, W={width}, H={height}\n" +
            $"Actual:    X={actual.Left}, Y={actual.Top}, " +
            $"W={actual.Right - actual.Left}, " +
            $"H={actual.Bottom - actual.Top}\n" +
            $"SetWindowPos: {result}";
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(
        IntPtr hWnd,
        out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}