namespace StormUnarchiver.Models;

public enum LogLevel
{
    Info,
    Success,
    Warning,
    Error
}

public class LogEntry
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public LogLevel Level { get; init; } = LogLevel.Info;
    public string Message { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;

    public string Icon => Level switch
    {
        LogLevel.Success => "\uE73E",  // Checkmark
        LogLevel.Warning => "\uE7BA",  // Warning
        LogLevel.Error => "\uE711",    // Cancel
        _ => "\uE946"                   // Info
    };

    public string TimeString => Timestamp.ToString("HH:mm:ss");
}
