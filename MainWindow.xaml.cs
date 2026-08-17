using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using MultiMonitorUtility.Services;

namespace MultiMonitorUtility;

public partial class MainWindow : Window
{
    private readonly LockScreenService _lockScreenService;
    private readonly List<MonitorDisplayModel> _monitors;

    public MainWindow()
    {
        InitializeComponent();
        var monitorService = new MonitorService();
        _lockScreenService = new LockScreenService(monitorService);
        _monitors = monitorService.GetMonitors().Select(m => new MonitorDisplayModel(m)).ToList();
        MonitorList.ItemsSource = _monitors;
        StatusText.Text = $"{_monitors.Count} monitor(s) detected.";
    }

    private void BrowseImage(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: MonitorDisplayModel monitor }) return;
        var dialog = new OpenFileDialog { Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All files|*.*", CheckFileExists = true, Title = $"Choose an image for {monitor.Name}" };
        if (dialog.ShowDialog(this) == true) monitor.ImagePath = dialog.FileName;
    }

    private void TestLockScreen(object sender, RoutedEventArgs e)
    {
        SaveServiceSettings();
        _lockScreenService.Show();
        StatusText.Text = "Preview active — move the mouse or press a key to dismiss.";
    }

    private void SaveSettings(object sender, RoutedEventArgs e)
    {
        SaveServiceSettings();
        StatusText.Text = "Settings applied.";
    }

    private void SaveServiceSettings()
    {
        foreach (var monitor in _monitors) _lockScreenService.SetImagePath(monitor.Name, monitor.ImagePath);
        _lockScreenService.SetActivateOnScreenSaverTimeout(ActivateOnTimeout.IsChecked == true);
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e) => _lockScreenService.Dispose();
}

public sealed class MonitorDisplayModel
{
    public string Name { get; }
    public string ImagePath { get; set; } = string.Empty;
    public MonitorDisplayModel(MonitorService.MonitorInfo monitor) => Name = monitor.DeviceName;
}
