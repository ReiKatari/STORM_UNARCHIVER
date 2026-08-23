using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace StormUnarchiver.Installer;

internal static class Program
{
    public const string Version = "0.2.0";
    public const string AppName = "STORM UNARCHIVER " + Version;
    public const string Publisher = "STORM TEAM";
    public const string GitHubUrl = "https://github.com/ReiKatari/STORM_UNARCHIVER";

    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        if (args.Length > 0 && (args[0].Equals("/uninstall", StringComparison.OrdinalIgnoreCase) ||
                                args[0].Equals("--uninstall", StringComparison.OrdinalIgnoreCase) ||
                                args[0].Equals("-u", StringComparison.OrdinalIgnoreCase)))
        {
            RunUninstall();
            return;
        }

        Application.Run(new InstallerForm());
    }

    private static void RunUninstall()
    {
        var result = MessageBox.Show(
            "Вы действительно хотите полностью удалить STORM UNARCHIVER и все его компоненты?",
            "Удаление STORM UNARCHIVER",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes) return;

        try
        {
            // Terminate running instances
            foreach (var proc in Process.GetProcessesByName("StormUnarchiver"))
            {
                try { proc.Kill(); proc.WaitForExit(3000); } catch { }
            }

            // Remove Desktop shortcut
            var desktopShortcut = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                $"{AppName}.lnk");
            if (File.Exists(desktopShortcut)) File.Delete(desktopShortcut);

            // Remove Start Menu shortcut
            var startMenuDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                AppName);
            if (Directory.Exists(startMenuDir)) Directory.Delete(startMenuDir, true);

            // Remove Registry Uninstall key
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree($@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{AppName}", false);
            }
            catch { }

            // Schedule deletion of install directory via cmd
            var installDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
            var cleanupCmd = $"/c timeout /t 2 /nobreak > NUL & rmdir /s /q \"{installDir}\"";
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = cleanupCmd,
                CreateNoWindow = true,
                UseShellExecute = false
            });

            MessageBox.Show(
                "STORM UNARCHIVER был успешно удален с вашего компьютера.",
                "Удаление завершено",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Ошибка при удалении: {ex.Message}",
                "Ошибка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
