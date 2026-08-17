using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;

namespace MultiMonitorUtility.Services;

public sealed class LockScreenService : IDisposable
{
    private readonly MonitorService _monitorService;
    private readonly Dictionary<string, string> _imagePaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<LockScreenWindow> _windows = new();
    private readonly DispatcherTimer _dismissalTimer;
    private readonly DispatcherTimer _screenSaverTimer;
    private bool _disposed;
    private bool _isVisible;
    private bool _activateOnScreenSaverTimeout;

    public LockScreenService(MonitorService? monitorService = null)
    {
        _monitorService = monitorService ?? new MonitorService();

        _dismissalTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _dismissalTimer.Tick += (_, _) =>
        {
            _dismissalTimer.Stop();
            foreach (var window in _windows)
            {
                window.EnableInputDismissal();
            }
        };

        _screenSaverTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _screenSaverTimer.Tick += (_, _) => CheckScreenSaverTimeout();
        SystemEvents.SessionSwitch += OnSessionSwitch;
    }

    public bool IsVisible => _isVisible;

    public void SetImagePath(string deviceName, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            _imagePaths.Remove(deviceName);
        }
        else
        {
            _imagePaths[deviceName] = path;
        }
    }

    public string? GetImagePath(string deviceName) =>
        _imagePaths.TryGetValue(deviceName, out var path) ? path : null;

    public void SetActivateOnScreenSaverTimeout(bool enabled)
    {
        _activateOnScreenSaverTimeout = enabled;
        if (enabled)
        {
            _screenSaverTimer.Start();
        }
        else
        {
            _screenSaverTimer.Stop();
        }
    }

    public void Show()
    {
        if (_disposed || _isVisible)
        {
            return;
        }

        Hide();
        foreach (var monitor in _monitorService.GetMonitors())
        {
            var window = new LockScreenWindow(
                monitor,
                GetImagePath(monitor.DeviceName),
                Hide);
            window.PositionOnMonitor();
            _windows.Add(window);
            window.Show();
        }

        _isVisible = _windows.Count > 0;
        _dismissalTimer.Start();
    }

    public void Hide()
    {
        _dismissalTimer.Stop();
        foreach (var window in _windows.ToList())
        {
            window.Close();
        }

        _windows.Clear();
        _isVisible = false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        SystemEvents.SessionSwitch -= OnSessionSwitch;
        _screenSaverTimer.Stop();
        Hide();
    }

    private void OnSessionSwitch(object? sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionUnlock)
        {
            Application.Current.Dispatcher.BeginInvoke(Hide);
        }
    }

    private void CheckScreenSaverTimeout()
    {
        if (!_activateOnScreenSaverTimeout || _isVisible ||
            !GetScreenSaverActive() || !GetScreenSaverTimeout(out var timeout))
        {
            return;
        }

        // Activate just before Windows starts its own screen saver. If we wait
        // until the exact boundary, Windows can switch desktops first and hide
        // this ordinary WPF preview immediately.
        var activationThreshold = Math.Max(1, timeout - 2);
        if (GetIdleSeconds() >= activationThreshold)
        {
            Show();
        }
    }

    private static bool GetScreenSaverActive()
    {
        return SystemParametersInfo(0x0010, 0, out var active, 0) && active != 0;
    }

    private static bool GetScreenSaverTimeout(out int seconds)
    {
        seconds = 0;
        return SystemParametersInfo(0x000E, 0, out seconds, 0);
    }

    private static double GetIdleSeconds()
    {
        var input = new LASTINPUTINFO { cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<LASTINPUTINFO>() };
        if (!GetLastInputInfo(ref input)) return 0;
        return (Environment.TickCount - input.dwTime) / 1000.0;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(
        uint action, uint parameter, out int result, uint updateIniFile);
}
