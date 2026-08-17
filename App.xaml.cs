using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace MultiMonitorUtility;

public partial class App : Application
{
    private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 =
        new(-4);

    static App()
    {
        SetProcessDpiAwarenessContext(
            DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetProcessDpiAwarenessContext(
        IntPtr dpiContext);
}