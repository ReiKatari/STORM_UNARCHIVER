using Microsoft.Win32;

namespace StormUnarchiver.Helpers;

public static class AutoStartHelper
{
    private const string RegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "StormUnarchiver";

    public static void SetAutoStart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
            if (key == null) return;

            if (enable)
            {
                var exePath = Environment.ProcessPath ?? "";
                if (!string.IsNullOrEmpty(exePath))
                    key.SetValue(AppName, $"\"{exePath}\" --minimized");
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch { /* Ignore registry errors */ }
    }

    public static bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, false);
            return key?.GetValue(AppName) != null;
        }
        catch { return false; }
    }
}
