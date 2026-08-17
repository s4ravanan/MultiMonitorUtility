using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using MultiMonitorUtility.Services;

namespace MultiMonitorUtility;

public partial class LockScreenWindow : Window
{
    private readonly MonitorService.MonitorInfo _monitor;
    private readonly Action _dismiss;
    private bool _inputEnabled;

    public LockScreenWindow(
        MonitorService.MonitorInfo monitor,
        string? imagePath,
        Action dismiss)
    {
        InitializeComponent();

        _monitor = monitor;
        _dismiss = dismiss;
        DisplayLabel.Text = monitor.DeviceName;

        if (!string.IsNullOrWhiteSpace(imagePath))
        {
            try
            {
                BackgroundImage.Source = new BitmapImage(
                    new Uri(imagePath, UriKind.Absolute));
            }
            catch (Exception)
            {
                // A missing or invalid image should leave a usable black screen.
            }
        }

        PreviewKeyDown += DismissOnInput;
        PreviewMouseDown += DismissOnInput;
        PreviewMouseMove += DismissOnInput;
        PreviewStylusDown += DismissOnInput;
    }

    public void EnableInputDismissal() => _inputEnabled = true;

    protected override void OnClosed(EventArgs e)
    {
        PreviewKeyDown -= DismissOnInput;
        PreviewMouseDown -= DismissOnInput;
        PreviewMouseMove -= DismissOnInput;
        PreviewStylusDown -= DismissOnInput;
        base.OnClosed(e);
    }

    private void DismissOnInput(object sender, InputEventArgs e)
    {
        if (_inputEnabled)
        {
            _dismiss();
        }
    }

    public void PositionOnMonitor()
    {
        Left = _monitor.MonitorArea.Left;
        Top = _monitor.MonitorArea.Top;
        Width = _monitor.Width;
        Height = _monitor.Height;
    }
}
