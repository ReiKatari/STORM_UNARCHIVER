using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using StormUnarchiver.Models;
using StormUnarchiver.Services;

namespace StormUnarchiver;

public partial class App : Application
{
    public static Window? MainWindow { get; private set; }
    private static Mutex? _singleInstanceMutex;

    private const string MutexName = @"Global\STORM_UNARCHIVER_SingleInstanceMutex";

    [DllImport("user32.dll", EntryPoint = "MessageBoxW", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_RESTORE = 9;

    public App()
    {
        // Single Instance Mode
        bool createdNew = false;
        try
        {
            _singleInstanceMutex = new Mutex(true, MutexName, out createdNew);
        }
        catch
        {
            createdNew = false;
        }

        if (!createdNew)
        {
            ActivateExistingInstance();
            Environment.Exit(0);
            return;
        }

        this.InitializeComponent();
        this.UnhandledException += App_UnhandledException;
    }

    private static void ActivateExistingInstance()
    {
        try
        {
            IntPtr hWnd = FindWindow(null, "STORM UNARCHIVER 1.0.0");
            if (hWnd == IntPtr.Zero)
            {
                var currentProc = Process.GetCurrentProcess();
                var procs = Process.GetProcessesByName(currentProc.ProcessName);
                foreach (var p in procs)
                {
                    if (p.Id != currentProc.Id && p.MainWindowHandle != IntPtr.Zero)
                    {
                        hWnd = p.MainWindowHandle;
                        break;
                    }
                }
            }

            if (hWnd != IntPtr.Zero)
            {
                ShowWindow(hWnd, SW_RESTORE);
                SetForegroundWindow(hWnd);
            }

            var settings = AppSettings.Load();
            LocalizationManager.Instance.SetLanguage(settings.SelectedLanguage);

            string title = LocalizationManager.Instance.GetString("SingleInstanceTitle");
            string msg = LocalizationManager.Instance.GetString("SingleInstanceMsg");
            MessageBox(IntPtr.Zero, msg, title, 0x40 | 0x10000);
        }
        catch { }
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        var logPath = Path.Combine(AppContext.BaseDirectory, "crash.log");
        File.AppendAllText(logPath, $"{DateTime.Now}: UNHANDLED: {e.Exception}\n{e.Message}\n");
        MessageBox(IntPtr.Zero, $"XAML UNHANDLED EXCEPTION:\n\n{e.Message}\n\n{e.Exception}", "STORM UNARCHIVER Error", 0x10);
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            var settings = AppSettings.Load();
            LocalizationManager.Instance.SetLanguage(settings.SelectedLanguage);
            ThemeManager.Instance.ApplyTheme(settings.SelectedTheme);

            MainWindow = new MainWindow();
            MainWindow.Activate();
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "crash.log");
            File.AppendAllText(logPath, $"{DateTime.Now}: ONLAUNCHED ERROR: {ex}\n");
            MessageBox(IntPtr.Zero, $"ONLAUNCHED ERROR:\n\n{ex}", "STORM UNARCHIVER Launch Error", 0x10);
            throw;
        }
    }
}
