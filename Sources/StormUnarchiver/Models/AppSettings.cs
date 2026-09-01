using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using StormUnarchiver.Services;

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

    // Archive password (global)
    public string ArchivePassword { get; set; } = "";

    // Extension filter (comma-separated, e.g. ".rar,.zip")
    public string ExtensionFilter { get; set; } = "";

    // Exclude mask (comma-separated, e.g. "*_part*,*.tmp")
    public string ExcludeMask { get; set; } = "";

    // Max parallel extractions
    public int MaxParallelExtractions { get; set; } = 1;

    // Retry count on failure
    public int RetryCount { get; set; } = 3;
    public int RetryDelaySec { get; set; } = 5;

    // Password Dictionary & Wordlist
    public List<string> PasswordDictionary { get; set; } = new();
    public string PasswordDictionaryFilePath { get; set; } = "";
    public bool EnablePasswordDictionary { get; set; } = true;

    // Recursive Nested Unpack
    public bool RecursiveUnpack { get; set; } = false;
    public int MaxRecursionDepth { get; set; } = 3;

    // I/O and CPU Throttling / Eco Mode
    public bool LowPriorityMode { get; set; } = false;
    public int ExtractionThrottleMs { get; set; } = 0;

    // STORM Soft Theme and Localization
    public ThemeType SelectedTheme { get; set; } = ThemeType.StormDark;
    public string SelectedLanguage { get; set; } = "ru";

    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "StormUnarchiver", "settings.json");

    private static readonly string LegacySettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "StormUnarchiver", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            // Migrate legacy settings if found
            if (!File.Exists(SettingsPath) && File.Exists(LegacySettingsPath))
            {
                var legacyJson = File.ReadAllText(LegacySettingsPath);
                var migrated = JsonSerializer.Deserialize<AppSettings>(legacyJson);
                if (migrated != null)
                {
                    migrated.Save();
                    return migrated;
                }
            }

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
