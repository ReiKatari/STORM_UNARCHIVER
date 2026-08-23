using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using StormUnarchiver.Helpers;
using StormUnarchiver.Models;
using StormUnarchiver.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace StormUnarchiver;

public sealed partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private readonly TrayIconManager _trayIcon;
    private readonly List<LogEntry> _allLogEntries = new();
    private readonly ObservableCollection<LogEntry> _filteredLogEntries = new();
    private readonly ObservableCollection<FolderPair> _folderPairs = new();
    private readonly Dictionary<string, ArchiveWatcherService> _watchers = new();
    private readonly ObservableCollection<FormatItem> _includeFormats = new();
    private readonly ObservableCollection<FormatItem> _excludeFormats = new();
    private bool _isUpdatingFormats;
    private bool _isMonitoring;
    private int _processedCount;
    private int _errorCount;
    private bool _forceClose;
    private string _activeLogFilter = "All";
    private string _logSearchQuery = string.Empty;

    public MainWindow()
    {
        this.InitializeComponent();

        // Window setup
        Title = "STORM UNARCHIVER 1.0.0";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        SetWindowSize(880, 900);
        CenterOnScreen();

        // Set Window & Taskbar Icon
        try
        {
            var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
            if (System.IO.File.Exists(iconPath))
            {
                GetAppWindow()?.SetIcon(iconPath);
            }
        }
        catch { /* ignore icon setting errors */ }

        // Load settings
        _settings = AppSettings.Load();

        // Tray icon
        _trayIcon = new TrayIconManager(ShowWindow, ForceExit);
        _trayIcon.Initialize();

        // Bind collections
        LogListView.ItemsSource = _filteredLogEntries;
        PairsListControl.ItemsSource = _folderPairs;

        // Init format lists
        var allFormats = new[] { 
            ".zip", ".rar", ".7z", ".tar", ".gz", ".iso", ".apk", ".cab", ".bz2", ".xz", ".tgz", ".lz", ".lzma", ".lzh", ".wim", ".arc", ".arj", ".z", ".sit", ".pea",
            ".alz", ".ace", ".bz", ".cbz", ".cbr", ".cpio", ".deb", ".dmg", ".egg", ".epa", ".gz2", ".ha", ".jar", ".lha", ".lzo", ".pak", ".part", ".pif", ".pkg", ".rpm",
            ".s7z", ".sea", ".sfx", ".sitx", ".sqx", ".tar.gz", ".tar.bz2", ".tar.xz", ".tar.Z", ".tbz", ".tbz2", ".tlz", ".txz", ".uha", ".war", ".xar", ".zoo", ".a", ".ar", ".lz4",
            ".zst", ".zstd", ".xpi", ".ipk", ".ipg", ".appx", ".msix", ".msi", ".exe", ".bin", ".vhd", ".vhdx", ".vmdk", ".qcow2", ".img", ".mdf", ".nrg", ".b5t", ".b6t", ".bwt",
            ".ccd", ".cdi", ".cue", ".isz", ".mds", ".mdx", ".toast", ".vcd", ".wua", ".xvd", ".zim", ".001", ".z01", ".r00", ".r01", ".s00", ".s01", ".lzm", ".lzx", ".taz"
        };
        foreach (var f in allFormats)
        {
            _includeFormats.Add(new FormatItem { Name = f });
            _excludeFormats.Add(new FormatItem { Name = f });
        }
        IncludeFormatsList.ItemsSource = _includeFormats;
        ExcludeFormatsList.ItemsSource = _excludeFormats;

        // Restore saved pairs
        RestoreSavedPairs();


        // Delete archive checkbox
        DeleteArchiveCheckBox.IsChecked = _settings.DeleteArchiveAfterExtract;
        DeleteArchiveCheckBox.Checked += (_, _) => { _settings.DeleteArchiveAfterExtract = true; _settings.Save(); };
        DeleteArchiveCheckBox.Unchecked += (_, _) => { _settings.DeleteArchiveAfterExtract = false; _settings.Save(); };

        // Autostart checkbox
        AutoStartCheckBox.IsChecked = _settings.AutoStartWithWindows;
        AutoStartCheckBox.Checked += (_, _) => { _settings.AutoStartWithWindows = true; _settings.Save(); Helpers.AutoStartHelper.SetAutoStart(true); };
        AutoStartCheckBox.Unchecked += (_, _) => { _settings.AutoStartWithWindows = false; _settings.Save(); Helpers.AutoStartHelper.SetAutoStart(false); };

        // Subfolders checkbox
        SubfoldersCheckBox.IsChecked = _settings.IncludeSubfolders;
        SubfoldersCheckBox.Checked += (_, _) => { _settings.IncludeSubfolders = true; _settings.Save(); };
        SubfoldersCheckBox.Unchecked += (_, _) => { _settings.IncludeSubfolders = false; _settings.Save(); };

        // Preserve structure checkbox
        PreserveStructureCheckBox.IsChecked = _settings.PreserveArchiveStructure;
        PreserveStructureCheckBox.Checked += (_, _) => { _settings.PreserveArchiveStructure = true; _settings.Save(); };
        PreserveStructureCheckBox.Unchecked += (_, _) => { _settings.PreserveArchiveStructure = false; _settings.Save(); };

        // Notifications checkbox
        NotificationsCheckBox.IsChecked = _settings.ShowNotifications;
        NotificationsCheckBox.Checked += (_, _) => { _settings.ShowNotifications = true; _settings.Save(); };
        NotificationsCheckBox.Unchecked += (_, _) => { _settings.ShowNotifications = false; _settings.Save(); };

        // Delay combo
        InitDelayCombo();

        // Password field
        PasswordBox.Password = _settings.ArchivePassword;
        PasswordPlainBox.Text = _settings.ArchivePassword;
        PasswordBox.PasswordChanged += (_, _) =>
        {
            if (PasswordPlainBox.Text != PasswordBox.Password)
                PasswordPlainBox.Text = PasswordBox.Password;
            _settings.ArchivePassword = PasswordBox.Password;
            _settings.Save();
        };
        PasswordPlainBox.TextChanged += (_, _) =>
        {
            if (PasswordBox.Password != PasswordPlainBox.Text)
                PasswordBox.Password = PasswordPlainBox.Text;
            _settings.ArchivePassword = PasswordPlainBox.Text;
            _settings.Save();
        };

        // Extension filter
        ExtensionFilterBox.Text = _settings.ExtensionFilter;

        // Exclude mask
        ExcludeMaskBox.Text = _settings.ExcludeMask;

        // Parallel combo
        InitComboFromTag(ParallelComboBox, _settings.MaxParallelExtractions, new[] { 1, 2, 3, 4 });
        ParallelComboBox.SelectionChanged += (_, _) =>
        {
            if (GetComboTag(ParallelComboBox) is int v) { _settings.MaxParallelExtractions = v; _settings.Save(); }
        };

        // Retry combo
        InitComboFromTag(RetryComboBox, _settings.RetryCount, new[] { 0, 1, 2, 3, 5 });
        RetryComboBox.SelectionChanged += (_, _) =>
        {
            if (GetComboTag(RetryComboBox) is int v) { _settings.RetryCount = v; _settings.Save(); }
        };

        // Recursive unpack checkbox
        RecursiveUnpackCheckBox.IsChecked = _settings.RecursiveUnpack;
        RecursiveUnpackCheckBox.Checked += (_, _) => { _settings.RecursiveUnpack = true; _settings.Save(); };
        RecursiveUnpackCheckBox.Unchecked += (_, _) => { _settings.RecursiveUnpack = false; _settings.Save(); };

        // Low priority / Eco mode checkbox
        LowPriorityCheckBox.IsChecked = _settings.LowPriorityMode;
        LowPriorityCheckBox.Checked += (_, _) => { _settings.LowPriorityMode = true; _settings.Save(); };
        LowPriorityCheckBox.Unchecked += (_, _) => { _settings.LowPriorityMode = false; _settings.Save(); };

        // Password Dictionary box
        PasswordDictionaryBox.Text = string.Join(Environment.NewLine, _settings.PasswordDictionary);
        PasswordDictionaryBox.TextChanged += (_, _) =>
        {
            var lines = PasswordDictionaryBox.Text
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct()
                .ToList();
            _settings.PasswordDictionary = lines;
            _settings.Save();
            UpdatePasswordDictStatus();
        };
        UpdatePasswordDictStatus();

        // Close handling
        this.Closed += MainWindow_Closed;

        AddLog(LogLevel.Info, "STORM UNARCHIVER 1.0.0 запущен и готов к работе");
    }

    // ===== WINDOW MANAGEMENT =====

    private void SetWindowSize(int w, int h)
    {
        var aw = GetAppWindow();
        aw?.Resize(new Windows.Graphics.SizeInt32(w, h));
    }

    private void CenterOnScreen()
    {
        var aw = GetAppWindow();
        if (aw != null)
        {
            var d = DisplayArea.GetFromWindowId(aw.Id, DisplayAreaFallback.Primary);
            aw.Move(new Windows.Graphics.PointInt32(
                (d.WorkArea.Width - aw.Size.Width) / 2,
                (d.WorkArea.Height - aw.Size.Height) / 2));
        }
    }

    private AppWindow? GetAppWindow()
    {
        var hWnd = WindowNative.GetWindowHandle(this);
        return AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hWnd));
    }

    private void ShowWindow()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var aw = GetAppWindow();
            aw?.Show();
            (aw?.Presenter as OverlappedPresenter)?.Restore();
            this.Activate();
        });
    }

    private void ForceExit()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            _forceClose = true;
            StopAllMonitoring();
            _trayIcon.Dispose();
            this.Close();
        });
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        if (!_forceClose)
        {
            args.Handled = true;
            GetAppWindow()?.Hide();
            if (_isMonitoring)
                _trayIcon.ShowBalloon("STORM UNARCHIVER 1.0.0", "Программа свёрнута. Мониторинг продолжается.");
        }
        else
        {
            StopAllMonitoring();
            _trayIcon.Dispose();
            _settings.Save();
        }
    }

    // ===== FOLDER PAIRS MANAGEMENT =====

    private void RestoreSavedPairs()
    {
        foreach (var data in _settings.FolderPairs)
        {
            _folderPairs.Add(data.ToModel());
        }
        UpdatePairsEmptyState();
    }

    private void SavePairs()
    {
        _settings.FolderPairs = _folderPairs.Select(FolderPairData.FromModel).ToList();
        _settings.Save();
    }

    private void UpdatePairsEmptyState()
    {
        var count = _folderPairs.Count;
        EmptyPairsState.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
        PairsCountBadge.Text = $"{count} {GetPairDeclension(count)}";
    }

    private static string GetPairDeclension(int n)
    {
        int rem100 = n % 100;
        int rem10 = n % 10;
        if (rem100 >= 11 && rem100 <= 19) return "пар";
        if (rem10 == 1) return "пара";
        if (rem10 >= 2 && rem10 <= 4) return "пары";
        return "пар";
    }

    private void AddPair_Click(object sender, RoutedEventArgs e)
    {
        var pair = new FolderPair();
        _folderPairs.Add(pair);
        SavePairs();
        UpdatePairsEmptyState();
        AddLog(LogLevel.Info, "Добавлена новая пара папок");
    }

    private void RemovePair_Click(object sender, RoutedEventArgs e)
    {
        var pair = GetPairFromSender(sender);
        if (pair == null) return;

        if (_watchers.TryGetValue(pair.Id, out var w))
        {
            w.Dispose();
            _watchers.Remove(pair.Id);
        }

        _folderPairs.Remove(pair);
        SavePairs();
        UpdatePairsEmptyState();
        AddLog(LogLevel.Info, "Пара папок удалена");

        if (_isMonitoring && _watchers.Count == 0)
            StopAllMonitoring();
    }

    // ===== BROWSE & OPEN FOLDERS =====

    private async void BrowseWatch_Click(object sender, RoutedEventArgs e)
    {
        var pair = GetPairFromSender(sender);
        if (pair == null) return;
        var path = await PickFolderAsync();
        if (path != null)
        {
            pair.WatchFolder = path;
            SavePairs();
            AddLog(LogLevel.Info, $"Папка-источник: {path}");
        }
    }

    private async void BrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        var pair = GetPairFromSender(sender);
        if (pair == null) return;
        var path = await PickFolderAsync();
        if (path != null)
        {
            pair.OutputFolder = path;
            SavePairs();
            AddLog(LogLevel.Info, $"Папка назначения: {path}");
        }
    }

    private void OpenWatchFolder_Click(object sender, RoutedEventArgs e)
    {
        if (GetPairFromSender(sender) is FolderPair pair && !string.IsNullOrEmpty(pair.WatchFolder))
        {
            OpenInExplorer(pair.WatchFolder);
        }
    }

    private void OpenOutputFolder_Click(object sender, RoutedEventArgs e)
    {
        if (GetPairFromSender(sender) is FolderPair pair && !string.IsNullOrEmpty(pair.OutputFolder))
        {
            OpenInExplorer(pair.OutputFolder);
        }
    }

    private static void OpenInExplorer(string path)
    {
        try
        {
            if (System.IO.Directory.Exists(path) || System.IO.File.Exists(path))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{path}\"",
                    UseShellExecute = true
                });
            }
        }
        catch { /* ignore */ }
    }

    private async Task<string?> PickFolderAsync()
    {
        var picker = new FolderPicker();
        picker.SuggestedStartLocation = PickerLocationId.Desktop;
        picker.FileTypeFilter.Add("*");
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }

    private static FolderPair? GetPairFromSender(object sender)
    {
        return (sender as FrameworkElement)?.DataContext as FolderPair;
    }

    // ===== DRAG AND DROP =====

    private void PairWatch_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Link;
        e.DragUIOverride.Caption = "Папка-источник";
        e.DragUIOverride.IsCaptionVisible = true;
    }

    private async void PairWatch_Drop(object sender, DragEventArgs e)
    {
        var pair = GetPairFromSender(sender);
        if (pair == null) return;
        var folder = await GetDroppedFolder(e);
        if (folder != null)
        {
            pair.WatchFolder = folder;
            SavePairs();
            AddLog(LogLevel.Info, $"Папка-источник: {folder}");
        }
    }

    private void PairOutput_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Link;
        e.DragUIOverride.Caption = "Папка назначения";
        e.DragUIOverride.IsCaptionVisible = true;
    }

    private async void PairOutput_Drop(object sender, DragEventArgs e)
    {
        var pair = GetPairFromSender(sender);
        if (pair == null) return;
        var folder = await GetDroppedFolder(e);
        if (folder != null)
        {
            pair.OutputFolder = folder;
            SavePairs();
            AddLog(LogLevel.Info, $"Папка назначения: {folder}");
        }
    }

    private static async Task<string?> GetDroppedFolder(DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            return items.OfType<StorageFolder>().FirstOrDefault()?.Path;
        }
        return null;
    }

    // ===== MONITORING CONTROL =====

    private void ToggleMonitor_Click(object sender, RoutedEventArgs e)
    {
        if (_isMonitoring) StopAllMonitoring();
        else StartAllMonitoring();
    }

    private void StartAllMonitoring()
    {
        var configured = _folderPairs.Where(p => p.IsConfigured).ToList();
        if (configured.Count == 0)
        {
            AddLog(LogLevel.Warning, "Нет полностью настроенных пар папок для мониторинга");
            return;
        }

        foreach (var pair in configured)
        {
            if (_watchers.ContainsKey(pair.Id)) continue;

            try
            {
                var watcher = new ArchiveWatcherService(
                    pair.WatchFolder, pair.OutputFolder,
                    onDetected: (f) => DispatcherQueue.TryEnqueue(() =>
                        AddLog(LogLevel.Info, $"Обнаружен архив: {f}")),
                    onProcessed: (f, ok, detail) => DispatcherQueue.TryEnqueue(() =>
                    {
                        if (ok)
                        {
                            _processedCount++;
                            ProcessedCount.Text = _processedCount.ToString();
                            AddLog(LogLevel.Success, $"{f} {detail}");
                            if (_settings.ShowNotifications)
                                _trayIcon.ShowBalloon("Распаковано", $"{f} {detail}");
                        }
                        else
                        {
                            _errorCount++;
                            ErrorCount.Text = _errorCount.ToString();
                            AddLog(LogLevel.Error, $"{f} — {detail}");
                            if (_settings.ShowNotifications)
                                _trayIcon.ShowBalloon("Ошибка распаковки", $"{f} — {detail}");
                        }
                    }),
                    getDeleteArchive: () => _settings.DeleteArchiveAfterExtract,
                    getPreserveStructure: () => _settings.PreserveArchiveStructure,
                    getDelaySec: () => _settings.ProcessingDelaySec,
                    getIncludeSubfolders: () => _settings.IncludeSubfolders,
                    getPassword: () => _settings.ArchivePassword,
                    getPasswordDictionary: GetCandidatePasswords,
                    getExtensionFilter: () => _settings.ExtensionFilter,
                    getExcludeMask: () => _settings.ExcludeMask,
                    getRetryCount: () => _settings.RetryCount,
                    getRetryDelaySec: () => _settings.RetryDelaySec,
                    getMaxParallel: () => _settings.MaxParallelExtractions,
                    getRecursiveUnpack: () => _settings.RecursiveUnpack,
                    getMaxRecursionDepth: () => _settings.MaxRecursionDepth,
                    getLowPriorityMode: () => _settings.LowPriorityMode,
                    getExtractionThrottleMs: () => _settings.ExtractionThrottleMs,
                    onProgress: (msg) => DispatcherQueue.TryEnqueue(() =>
                    {
                        ProgressStatusText.Text = msg;
                        ProgressStatusText.Visibility = string.IsNullOrEmpty(msg)
                            ? Visibility.Collapsed : Visibility.Visible;
                    }));

                watcher.Start();
                _watchers[pair.Id] = watcher;
                AddLog(LogLevel.Success, $"Запущен мониторинг: {pair.WatchFolder}");
            }
            catch (Exception ex)
            {
                AddLog(LogLevel.Error, $"Ошибка запуска: {ex.Message}");
            }
        }

        if (_watchers.Count > 0)
        {
            _isMonitoring = true;
            ToggleIcon.Glyph = "\uE71A";
            ToggleText.Text = "Остановить мониторинг";
            ToggleMonitorButton.Background = new SolidColorBrush(ColorHelper.FromArgb(255, 220, 60, 60));
            StatusDot.Fill = (SolidColorBrush)Application.Current.Resources["StormSuccessBrush"];
            StatusText.Text = $"Мониторинг активен ({_watchers.Count} {_watchers.Count switch { 1 => "папка", >= 2 and <= 4 => "папки", _ => "папок" }})";
            StatusText.Foreground = (SolidColorBrush)Application.Current.Resources["StormSuccessBrush"];
            AnimateStatusPulse();
            _trayIcon.SetMonitoringState(Helpers.TrayState.Active);
        }
    }

    // ===== PASSWORD DICTIONARY & HELPERS =====

    private IEnumerable<string> GetCandidatePasswords()
    {
        var list = new HashSet<string>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(_settings.ArchivePassword))
            list.Add(_settings.ArchivePassword);

        if (_settings.EnablePasswordDictionary)
        {
            foreach (var p in _settings.PasswordDictionary)
            {
                if (!string.IsNullOrWhiteSpace(p)) list.Add(p.Trim());
            }

            if (!string.IsNullOrWhiteSpace(_settings.PasswordDictionaryFilePath) && File.Exists(_settings.PasswordDictionaryFilePath))
            {
                try
                {
                    foreach (var line in File.ReadLines(_settings.PasswordDictionaryFilePath).Take(500))
                    {
                        if (!string.IsNullOrWhiteSpace(line)) list.Add(line.Trim());
                    }
                }
                catch { }
            }
        }
        return list;
    }

    private async void PickPasswordFile_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.SuggestedStartLocation = PickerLocationId.Desktop;
        picker.FileTypeFilter.Add(".txt");
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            _settings.PasswordDictionaryFilePath = file.Path;
            try
            {
                var lines = await FileIO.ReadLinesAsync(file);
                var count = 0;
                foreach (var l in lines)
                {
                    if (!string.IsNullOrWhiteSpace(l) && !_settings.PasswordDictionary.Contains(l.Trim()))
                    {
                        _settings.PasswordDictionary.Add(l.Trim());
                        count++;
                    }
                }
                PasswordDictionaryBox.Text = string.Join(Environment.NewLine, _settings.PasswordDictionary);
                _settings.Save();
                UpdatePasswordDictStatus();
                AddLog(LogLevel.Success, $"Загружен словарь паролей ({count} новых): {file.Name}");
            }
            catch (Exception ex)
            {
                AddLog(LogLevel.Error, $"Ошибка чтения словаря: {ex.Message}");
            }
        }
    }

    private void ClearPasswordDict_Click(object sender, RoutedEventArgs e)
    {
        _settings.PasswordDictionary.Clear();
        _settings.PasswordDictionaryFilePath = "";
        _settings.Save();
        PasswordDictionaryBox.Text = "";
        UpdatePasswordDictStatus();
        AddLog(LogLevel.Info, "Словарь паролей очищен");
    }

    private void UpdatePasswordDictStatus()
    {
        var count = _settings.PasswordDictionary.Count;
        PasswordDictStatusText.Text = count > 0 ? $"В словаре: {count} паролей" : "Словарь пуст";
    }

    private void StopAllMonitoring()
    {
        foreach (var w in _watchers.Values) w.Dispose();
        _watchers.Clear();
        _isMonitoring = false;

        DispatcherQueue.TryEnqueue(() =>
        {
            ToggleIcon.Glyph = "\uE768";
            ToggleText.Text = "Начать мониторинг";
            ToggleMonitorButton.Background = (SolidColorBrush)Application.Current.Resources["StormAccentDarkBrush"];
            StatusDot.Fill = (SolidColorBrush)Application.Current.Resources["StormTextDimBrush"];
            StatusText.Text = "Ожидание запуска";
            StatusText.Foreground = (SolidColorBrush)Application.Current.Resources["StormTextDimBrush"];
            AddLog(LogLevel.Info, "Мониторинг всех папок остановлен");
            _trayIcon.SetMonitoringState(Helpers.TrayState.Idle);
        });
    }

    // ===== LOG MANAGEMENT & FILTERING =====

    private void AddLog(LogLevel level, string message)
    {
        var entry = new LogEntry { Level = level, Message = message };
        _allLogEntries.Insert(0, entry);
        while (_allLogEntries.Count > 300) _allLogEntries.RemoveAt(_allLogEntries.Count - 1);
        ApplyLogFilter();
    }

    private void ApplyLogFilter()
    {
        _filteredLogEntries.Clear();
        var q = _allLogEntries.AsEnumerable();

        if (_activeLogFilter == "Success")
            q = q.Where(e => e.Level == LogLevel.Success);
        else if (_activeLogFilter == "Error")
            q = q.Where(e => e.Level == LogLevel.Error);
        else if (_activeLogFilter == "Info")
            q = q.Where(e => e.Level == LogLevel.Info || e.Level == LogLevel.Warning);

        if (!string.IsNullOrWhiteSpace(_logSearchQuery))
        {
            q = q.Where(e => e.Message.Contains(_logSearchQuery, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in q)
        {
            _filteredLogEntries.Add(item);
        }

        EmptyLogState.Visibility = _filteredLogEntries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void LogFilter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton tb)
        {
            LogFilterAllBtn.IsChecked = tb == LogFilterAllBtn;
            LogFilterSuccessBtn.IsChecked = tb == LogFilterSuccessBtn;
            LogFilterErrorBtn.IsChecked = tb == LogFilterErrorBtn;
            LogFilterInfoBtn.IsChecked = tb == LogFilterInfoBtn;

            if (tb == LogFilterSuccessBtn) _activeLogFilter = "Success";
            else if (tb == LogFilterErrorBtn) _activeLogFilter = "Error";
            else if (tb == LogFilterInfoBtn) _activeLogFilter = "Info";
            else _activeLogFilter = "All";

            ApplyLogFilter();
        }
    }

    private void LogSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _logSearchQuery = LogSearchBox.Text ?? string.Empty;
        ApplyLogFilter();
    }

    private void ClearLog_Click(object sender, RoutedEventArgs e)
    {
        _allLogEntries.Clear();
        _filteredLogEntries.Clear();
        _processedCount = 0; _errorCount = 0;
        ProcessedCount.Text = "0"; ErrorCount.Text = "0";
        EmptyLogState.Visibility = Visibility.Visible;
    }

    private async void ExportLog_Click(object sender, RoutedEventArgs e)
    {
        if (_allLogEntries.Count == 0) return;

        var picker = new Windows.Storage.Pickers.FileSavePicker();
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
        picker.FileTypeChoices.Add("Текстовый файл", new List<string> { ".txt" });
        picker.FileTypeChoices.Add("CSV", new List<string> { ".csv" });
        picker.SuggestedFileName = $"storm_log_{DateTime.Now:yyyyMMdd_HHmmss}";
        WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this));

        var file = await picker.PickSaveFileAsync();
        if (file != null)
        {
            var lines = _allLogEntries.Select(e2 =>
                $"{e2.TimeString}\t{e2.Level}\t{e2.Message}");
            await Windows.Storage.FileIO.WriteLinesAsync(file, lines);
            AddLog(LogLevel.Info, $"Журнал экспортирован: {file.Path}");
        }
    }

    // ===== PASSWORD REVEAL TOGGLE =====

    private void RevealPassword_Click(object sender, RoutedEventArgs e)
    {
        bool isRevealed = RevealPasswordButton.IsChecked == true;
        if (isRevealed)
        {
            PasswordPlainBox.Text = PasswordBox.Password;
            PasswordPlainBox.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Collapsed;
            RevealPasswordIcon.Glyph = "\uE890"; // open eye
        }
        else
        {
            PasswordBox.Password = PasswordPlainBox.Text;
            PasswordBox.Visibility = Visibility.Visible;
            PasswordPlainBox.Visibility = Visibility.Collapsed;
            RevealPasswordIcon.Glyph = "\uE7B3"; // closed eye
        }
    }

    // ===== GITHUB & ACTION LINKS =====

    private void GitHub_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/ReiKatari/STORM_UNARCHIVER",
                UseShellExecute = true
            });
        }
        catch { /* ignore */ }
    }

    // ===== PRESETS =====

    private void PresetAll_Click(object sender, RoutedEventArgs e)
    {
        IncludeSelectAll_Click(sender, e);
    }

    private void PresetCommon_Click(object sender, RoutedEventArgs e)
    {
        var common = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".zip", ".rar", ".7z", ".tar", ".gz", ".tar.gz", ".tar.bz2", ".tar.xz", ".bz2", ".xz" };
        if (_isUpdatingFormats) return;
        _isUpdatingFormats = true;
        foreach (var f in _includeFormats) f.IsSelected = common.Contains(f.Name);
        ExtensionFilterBox.Text = string.Join(", ", _includeFormats.Where(f => f.IsSelected).Select(f => f.Name));
        _settings.ExtensionFilter = ExtensionFilterBox.Text;
        _settings.Save();
        _isUpdatingFormats = false;
    }

    private void PresetDiskImages_Click(object sender, RoutedEventArgs e)
    {
        var diskImages = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".iso", ".img", ".vhd", ".vhdx", ".vmdk", ".qcow2", ".dmg", ".wim", ".esd" };
        if (_isUpdatingFormats) return;
        _isUpdatingFormats = true;
        foreach (var f in _includeFormats) f.IsSelected = diskImages.Contains(f.Name);
        ExtensionFilterBox.Text = string.Join(", ", _includeFormats.Where(f => f.IsSelected).Select(f => f.Name));
        _settings.ExtensionFilter = ExtensionFilterBox.Text;
        _settings.Save();
        _isUpdatingFormats = false;
    }

    private void PresetReset_Click(object sender, RoutedEventArgs e)
    {
        IncludeDeselectAll_Click(sender, e);
        ExcludeDeselectAll_Click(sender, e);
    }

    // ===== COMBO HELPERS =====

    private void InitDelayCombo()
    {
        var delayValues = new[] { 1, 2, 3, 5, 10, 15, 30 };
        var idx = Array.IndexOf(delayValues, _settings.ProcessingDelaySec);
        DelayComboBox.SelectedIndex = idx >= 0 ? idx : 1;

        DelayComboBox.SelectionChanged += (_, _) =>
        {
            if (GetComboTag(DelayComboBox) is int val)
            {
                _settings.ProcessingDelaySec = val;
                _settings.Save();
            }
        };
    }

    private static void InitComboFromTag(Microsoft.UI.Xaml.Controls.ComboBox combo, int value, int[] values)
    {
        var idx = Array.IndexOf(values, value);
        combo.SelectedIndex = idx >= 0 ? idx : 0;
    }

    private static int? GetComboTag(Microsoft.UI.Xaml.Controls.ComboBox combo)
    {
        if (combo.SelectedItem is Microsoft.UI.Xaml.Controls.ComboBoxItem item
            && item.Tag is string tag && int.TryParse(tag, out var val))
            return val;
        return null;
    }

    // ===== ANIMATIONS =====

    private void AnimateStatusPulse()
    {
        var sb = new Storyboard();
        var anim = new DoubleAnimation
        {
            From = 1.0, To = 0.3,
            Duration = new Duration(TimeSpan.FromMilliseconds(800)),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase()
        };
        Storyboard.SetTarget(anim, StatusDot);
        Storyboard.SetTargetProperty(anim, "Opacity");
        sb.Children.Add(anim);
        try { sb.Begin(); } catch { }
    }

    // ===== FORMAT PICKERS =====

    private void IncludeFormat_Changed(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingFormats) return;
        var selected = _includeFormats.Where(f => f.IsSelected).Select(f => f.Name);
        _isUpdatingFormats = true;
        ExtensionFilterBox.Text = string.Join(", ", selected);
        _settings.ExtensionFilter = ExtensionFilterBox.Text;
        _settings.Save();
        _isUpdatingFormats = false;
    }

    private void ExcludeFormat_Changed(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingFormats) return;
        var selected = _excludeFormats.Where(f => f.IsSelected).Select(f => f.Name);
        _isUpdatingFormats = true;
        ExcludeMaskBox.Text = string.Join(", ", selected);
        _settings.ExcludeMask = ExcludeMaskBox.Text;
        _settings.Save();
        _isUpdatingFormats = false;
    }

    private void ExtensionFilterBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingFormats) return;
        _isUpdatingFormats = true;
        var text = ExtensionFilterBox.Text ?? "";
        var parts = text.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var f in _includeFormats)
        {
            f.IsSelected = parts.Contains(f.Name);
        }
        _settings.ExtensionFilter = ExtensionFilterBox.Text ?? string.Empty;
        _settings.Save();
        _isUpdatingFormats = false;
    }

    private void ExcludeMaskBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingFormats) return;
        _isUpdatingFormats = true;
        var text = ExcludeMaskBox.Text ?? "";
        var parts = text.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var f in _excludeFormats)
        {
            f.IsSelected = parts.Contains(f.Name);
        }
        _settings.ExcludeMask = ExcludeMaskBox.Text ?? string.Empty;
        _settings.Save();
        _isUpdatingFormats = false;
    }

    private void IncludeSelectAll_Click(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingFormats) return;
        _isUpdatingFormats = true;
        foreach (var f in _includeFormats) f.IsSelected = true;
        ExtensionFilterBox.Text = string.Join(", ", _includeFormats.Select(f => f.Name));
        _settings.ExtensionFilter = ExtensionFilterBox.Text;
        _settings.Save();
        _isUpdatingFormats = false;
    }

    private void IncludeDeselectAll_Click(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingFormats) return;
        _isUpdatingFormats = true;
        foreach (var f in _includeFormats) f.IsSelected = false;
        ExtensionFilterBox.Text = "";
        _settings.ExtensionFilter = ExtensionFilterBox.Text;
        _settings.Save();
        _isUpdatingFormats = false;
    }

    private void ExcludeSelectAll_Click(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingFormats) return;
        _isUpdatingFormats = true;
        foreach (var f in _excludeFormats) f.IsSelected = true;
        ExcludeMaskBox.Text = string.Join(", ", _excludeFormats.Select(f => f.Name));
        _settings.ExcludeMask = ExcludeMaskBox.Text;
        _settings.Save();
        _isUpdatingFormats = false;
    }

    private void ExcludeDeselectAll_Click(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingFormats) return;
        _isUpdatingFormats = true;
        foreach (var f in _excludeFormats) f.IsSelected = false;
        ExcludeMaskBox.Text = "";
        _settings.ExcludeMask = ExcludeMaskBox.Text;
        _settings.Save();
        _isUpdatingFormats = false;
    }
}

public class FormatItem : System.ComponentModel.INotifyPropertyChanged
{
    private bool _isSelected;
    public string Name { get; set; } = string.Empty;
    public bool IsSelected 
    { 
        get => _isSelected; 
        set 
        { 
            if (_isSelected != value) 
            { 
                _isSelected = value; 
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsSelected))); 
            } 
        } 
    }
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
}
