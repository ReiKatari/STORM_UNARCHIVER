using System.Text.RegularExpressions;

namespace StormUnarchiver.Services;

public class ArchiveWatcherService : IDisposable
{
    private FileSystemWatcher? _watcher;
    private readonly string _watchFolder;
    private readonly string _outputFolder;
    private readonly Action<string, bool, string?> _onProcessed;
    private readonly Action<string> _onDetected;
    private readonly Action<string>? _onProgress; // #8 progress callback
    private readonly Func<bool> _getDeleteArchive;
    private readonly Func<bool> _getPreserveStructure;
    private readonly Func<int> _getDelaySec;
    private readonly Func<bool> _getIncludeSubfolders;
    private readonly Func<string> _getPassword;
    private readonly Func<string> _getExtensionFilter;
    private readonly Func<string> _getExcludeMask;
    private readonly Func<int> _getRetryCount;
    private readonly Func<int> _getRetryDelaySec;
    private SemaphoreSlim _processingLock;
    private readonly Func<int> _getMaxParallel;
    private readonly HashSet<string> _processingFiles = new();
    private bool _isRunning;

    public bool IsRunning => _isRunning;

    public ArchiveWatcherService(
        string watchFolder,
        string outputFolder,
        Action<string> onDetected,
        Action<string, bool, string?> onProcessed,
        Func<bool> getDeleteArchive,
        Func<bool> getPreserveStructure,
        Func<int> getDelaySec,
        Func<bool> getIncludeSubfolders,
        Func<string> getPassword,
        Func<string> getExtensionFilter,
        Func<string> getExcludeMask,
        Func<int> getRetryCount,
        Func<int> getRetryDelaySec,
        Func<int> getMaxParallel,
        Action<string>? onProgress = null)
    {
        _watchFolder = watchFolder;
        _outputFolder = outputFolder;
        _onDetected = onDetected;
        _onProcessed = onProcessed;
        _getDeleteArchive = getDeleteArchive;
        _getPreserveStructure = getPreserveStructure;
        _getDelaySec = getDelaySec;
        _getIncludeSubfolders = getIncludeSubfolders;
        _getPassword = getPassword;
        _getExtensionFilter = getExtensionFilter;
        _getExcludeMask = getExcludeMask;
        _getRetryCount = getRetryCount;
        _getRetryDelaySec = getRetryDelaySec;
        _getMaxParallel = getMaxParallel;
        _onProgress = onProgress;
        _processingLock = new SemaphoreSlim(Math.Clamp(getMaxParallel(), 1, 8));
    }

    public void Start()
    {
        if (_isRunning) return;

        if (!Directory.Exists(_watchFolder))
            Directory.CreateDirectory(_watchFolder);

        // Update semaphore to match current setting
        var maxP = Math.Clamp(_getMaxParallel(), 1, 8);
        _processingLock = new SemaphoreSlim(maxP, maxP);

        // Process existing archives first
        ProcessExistingArchives();

        _watcher = new FileSystemWatcher(_watchFolder)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            IncludeSubdirectories = _getIncludeSubfolders(),
            EnableRaisingEvents = true
        };

        _watcher.Created += OnFileCreated;
        _watcher.Renamed += OnFileRenamed;
        _isRunning = true;
    }

    public void Stop()
    {
        if (!_isRunning) return;

        _watcher?.Dispose();
        _watcher = null;
        _isRunning = false;
    }

    // ===== FILTERING =====

    private bool ShouldProcess(string filePath)
    {
        if (!ArchiveExtractorService.IsArchive(filePath))
            return false;

        var fileName = Path.GetFileName(filePath);

        // #3 — Extension filter
        var extFilter = _getExtensionFilter();
        if (!string.IsNullOrWhiteSpace(extFilter))
        {
            var allowed = extFilter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var ext = Path.GetExtension(filePath);
            if (allowed.Length > 0 && !allowed.Any(a =>
                a.Equals(ext, StringComparison.OrdinalIgnoreCase) ||
                a.Equals(ext.TrimStart('.'), StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        // #4 — Exclude mask
        var excludeMask = _getExcludeMask();
        if (!string.IsNullOrWhiteSpace(excludeMask))
        {
            var patterns = excludeMask.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var pattern in patterns)
            {
                if (MatchesWildcard(fileName, pattern))
                    return false;
            }
        }

        return true;
    }

    private static bool MatchesWildcard(string input, string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase);
    }

    // ===== PROCESSING =====

    private void ProcessExistingArchives()
    {
        try
        {
            var searchOption = _getIncludeSubfolders()
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            foreach (var file in Directory.GetFiles(_watchFolder, "*", searchOption))
            {
                if (ShouldProcess(file))
                    _ = ProcessArchiveAsync(file);
            }
        }
        catch { /* ignore */ }
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        if (ShouldProcess(e.FullPath))
            _ = ProcessArchiveAsync(e.FullPath);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        if (ShouldProcess(e.FullPath))
            _ = ProcessArchiveAsync(e.FullPath);
    }

    private async Task ProcessArchiveAsync(string filePath)
    {
        var fileName = Path.GetFileName(filePath);

        // Prevent duplicate processing
        lock (_processingFiles)
        {
            if (!_processingFiles.Add(filePath))
                return;
        }

        try
        {
            _onDetected(fileName);

            // Configurable delay to ensure file is fully written
            var delaySec = Math.Clamp(_getDelaySec(), 1, 30);
            await Task.Delay(delaySec * 1000);

            await _processingLock.WaitAsync();
            try
            {
                var retryCount = Math.Clamp(_getRetryCount(), 0, 10);
                var retryDelay = Math.Clamp(_getRetryDelaySec(), 1, 60);
                var attempt = 0;
                var success = false;

                while (attempt <= retryCount && !success)
                {
                    if (attempt > 0)
                    {
                        _onProgress?.Invoke($"Повтор {attempt}/{retryCount}: {fileName}");
                        await Task.Delay(retryDelay * 1000);
                    }

                    _onProgress?.Invoke($"Распаковка: {fileName}");

                    var preserveStructure = _getPreserveStructure();
                    var password = _getPassword();
                    var (ok, files, error) = await Task.Run(() =>
                        ArchiveExtractorService.ExtractAndMove(
                            filePath, _outputFolder,
                            _getDeleteArchive(), preserveStructure,
                            string.IsNullOrEmpty(password) ? null : password));

                    if (ok)
                    {
                        var filesStr = string.Join(", ", files);
                        _onProcessed(fileName, true, $"→ {filesStr}");
                        _onProgress?.Invoke("");
                        success = true;
                    }
                    else
                    {
                        attempt++;
                        if (attempt > retryCount)
                        {
                            _onProcessed(fileName, false, $"{error} (после {retryCount} попыток)");
                            _onProgress?.Invoke("");
                        }
                    }
                }
            }
            finally
            {
                _processingLock.Release();
            }
        }
        finally
        {
            lock (_processingFiles)
            {
                _processingFiles.Remove(filePath);
            }
        }
    }

    public void Dispose()
    {
        Stop();
        _processingLock.Dispose();
    }
}
