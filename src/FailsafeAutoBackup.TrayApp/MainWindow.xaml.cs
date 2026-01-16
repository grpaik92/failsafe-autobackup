using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using FailsafeAutoBackup.IPC.Client;
using FailsafeAutoBackup.Shared.Configuration;
using FailsafeAutoBackup.Shared.IPC;
using FailsafeAutoBackup.Shared.Models;
using Hardcodet.Wpf.TaskbarNotification;
using MessageBox = System.Windows.MessageBox;

namespace FailsafeAutoBackup.TrayApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly NamedPipeClient _ipcClient;
    private readonly DispatcherTimer _statusTimer;
    private readonly TaskbarIcon _trayIcon;
    private ServiceConfiguration _configuration;

    public MainWindow()
    {
        InitializeComponent();
        
        _ipcClient = new NamedPipeClient();
        _configuration = new ServiceConfiguration();
        
        // Initialize tray icon
        _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
        
        // Setup status update timer
        _statusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _statusTimer.Tick += StatusTimer_Tick;
        _statusTimer.Start();

        // Hide on startup (start minimized to tray)
        WindowState = WindowState.Minimized;
        ShowInTaskbar = false;
        
        // Try to connect to service
        ConnectToService();
        
        // Load settings
        LoadSettings();
    }

    private async void ConnectToService()
    {
        try
        {
            var connected = await _ipcClient.ConnectAsync();
            if (connected)
            {
                UpdateStatus(true, DateTime.Now);
                ActivityLog.Text = "Connected to service\n" + ActivityLog.Text;
            }
            else
            {
                UpdateStatus(false, DateTime.MinValue);
                ActivityLog.Text = "Failed to connect to service\n" + ActivityLog.Text;
            }
        }
        catch (Exception ex)
        {
            ActivityLog.Text = $"Error connecting: {ex.Message}\n" + ActivityLog.Text;
        }
    }

    private async void StatusTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            if (!_ipcClient.IsConnected)
            {
                await _ipcClient.ConnectAsync();
            }

            if (_ipcClient.IsConnected)
            {
                var message = new IPCMessage
                {
                    Type = MessageType.GetStatus
                };

                var response = await _ipcClient.SendMessageAsync(message);
                if (response != null && response.Type == MessageType.StatusResponse)
                {
                    // Parse status from response payload
                    UpdateStatus(true, DateTime.Now);
                }
            }
            else
            {
                UpdateStatus(false, DateTime.MinValue);
            }
        }
        catch (Exception ex)
        {
            UpdateStatus(false, DateTime.MinValue);
        }
    }

    private void UpdateStatus(bool isRunning, DateTime lastHeartbeat)
    {
        Dispatcher.Invoke(() =>
        {
            StatusIndicator.Fill = isRunning ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
            StatusText.Text = isRunning ? "Running" : "Stopped";
            LastHeartbeatText.Text = $"Last heartbeat: {(lastHeartbeat != DateTime.MinValue ? lastHeartbeat.ToLocalTime().ToString("g") : "N/A")}";
        });
    }

    private void LoadSettings()
    {
        BackupIntervalTextBox.Text = _configuration.BackupIntervalMinutes.ToString();
        BackupFolderTextBox.Text = _configuration.BackupFolderPath;
        EnableWordCheckBox.IsChecked = _configuration.EnableWordBackup;
        EnablePdfCheckBox.IsChecked = _configuration.EnablePdfBackup;
        CreateShortcutCheckBox.IsChecked = _configuration.CreateDesktopShortcut;
    }

    private void TrayIcon_OnTrayLeftMouseUp(object sender, RoutedEventArgs e)
    {
        ShowWindow();
    }

    private void OpenDashboard_Click(object sender, RoutedEventArgs e)
    {
        ShowWindow();
    }

    private void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        ShowInTaskbar = true;
        Activate();
    }

    private async void RestartService_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to restart the backup service?",
            "Restart Service",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                // Use sc.exe to restart the service
                var psi = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = "stop \"FailsafeAutoBackup Service\"",
                    UseShellExecute = true,
                    Verb = "runas" // Run as administrator
                };
                Process.Start(psi)?.WaitForExit();

                await Task.Delay(2000);

                psi.Arguments = "start \"FailsafeAutoBackup Service\"";
                Process.Start(psi)?.WaitForExit();

                MessageBox.Show("Service restart initiated.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Reconnect
                await Task.Delay(3000);
                ConnectToService();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to restart service: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void OpenLogsFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var logsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "FailsafeAutoBackup", "Logs");

            if (Directory.Exists(logsPath))
            {
                Process.Start("explorer.exe", logsPath);
            }
            else
            {
                MessageBox.Show("Logs folder not found.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open logs folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenBackupFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var backupPath = string.IsNullOrWhiteSpace(_configuration.BackupFolderPath)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "FailsafeAutoBackup")
                : _configuration.BackupFolderPath;

            if (Directory.Exists(backupPath))
            {
                Process.Start("explorer.exe", backupPath);
            }
            else
            {
                MessageBox.Show("Backup folder not found.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open backup folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        ShowWindow();
        // Switch to settings tab (TabControl is named implicitly, we'd need to name it to access)
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        _statusTimer.Stop();
        _ipcClient.Dispose();
        _trayIcon.Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    private void MinimizeToTray_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
        ShowInTaskbar = false;
        Hide();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        MinimizeToTray_Click(sender, e);
    }

    private void BrowseBackupFolder_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            BackupFolderTextBox.Text = dialog.SelectedPath;
        }
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _configuration.BackupIntervalMinutes = int.Parse(BackupIntervalTextBox.Text);
            _configuration.BackupFolderPath = BackupFolderTextBox.Text;
            _configuration.EnableWordBackup = EnableWordCheckBox.IsChecked ?? true;
            _configuration.EnablePdfBackup = EnablePdfCheckBox.IsChecked ?? true;
            _configuration.CreateDesktopShortcut = CreateShortcutCheckBox.IsChecked ?? true;

            // TODO: Send configuration update to service via IPC

            MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    protected override void OnStateChanged(EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            ShowInTaskbar = false;
            Hide();
        }

        base.OnStateChanged(e);
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Prevent closing, minimize to tray instead
        e.Cancel = true;
        WindowState = WindowState.Minimized;
        ShowInTaskbar = false;
        Hide();
    }
}
