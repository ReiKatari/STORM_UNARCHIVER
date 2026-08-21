using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace StormUnarchiver;

public partial class App : Application
{
    public static Window? MainWindow { get; private set; }

    [DllImport("user32.dll", EntryPoint = "MessageBoxW", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    [DllImport("Microsoft.ui.xaml.dll")]
    private static extern void XamlCheckProcessRequirements();

    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            XamlCheckProcessRequirements();
            WinRT.ComWrappersSupport.InitializeComWrappers();
            Application.Start((p) =>
            {
                var context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(
                    Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
                System.Threading.SynchronizationContext.SetSynchronizationContext(context);
                new App();
            });
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "crash.log");
            File.WriteAllText(logPath, $"{DateTime.Now}: FATAL MAIN: {ex}\n");
            MessageBox(IntPtr.Zero, $"FATAL ERROR:\n\n{ex}", "STORM UNARCHIVER Fatal Error", 0x10);
            throw;
        }
    }

    public App()
    {
        this.InitializeComponent();
        this.UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        var logPath = Path.Combine(AppContext.BaseDirectory, "crash.log");
        File.AppendAllText(logPath, $"{DateTime.Now}: UNHANDLED: {e.Exception}\n{e.Message}\n");
        MessageBox(IntPtr.Zero, $"XAML UNHANDLED EXCEPTION:\n\n{e.Message}\n\n{e.Exception}", "STORM UNARCHIVER Error", 0x10);
        // Do NOT set e.Handled = true if we crashed during initialization so the process terminates cleanly
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
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
