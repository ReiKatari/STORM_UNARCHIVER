using System.Text.Json;

namespace StormUnarchiver.Models;

public class AppSettings
{
    public List<FolderPairData> FolderPairs { get; set; } = new();
    public bool MinimizeToTray { get; set; } = true;
    public bool DeleteArchiveAfterExtract { get; set; } = true;
    public bool AutoStartWithWindows { get; set; } = false;
    public bool IncludeSubfolders { get; set; } = false;
    public bool PreserveArchiveStructure { get; set; } = false;
    public bool ShowNotifications { get; set; } = true;
    public int ProcessingDelaySec { get; set; } = 2;

    // #2 — Archive password (global)
    public string ArchivePassword { get; set; } = "";

    // #3 — Extension filter (comma-separated, e.g. ".rar,.zip")
    public string ExtensionFilter { get; set; } = "";

    // #4 — Exclude mask (comma-separated, e.g. "*_part*,*.tmp")
    public string ExcludeMask { get; set; } = "";

    // #17 — Max parallel extractions
    public int MaxParallelExtractions { get; set; } = 1;

    // #18 — Retry count on failure
    public int RetryCount { get; set; } = 3;
    public int RetryDelaySec { get; set; } = 5;

    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "StormUnarchiver", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { /* Return default settings on error */ }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { /* Silently fail on save error */ }
    }
}
