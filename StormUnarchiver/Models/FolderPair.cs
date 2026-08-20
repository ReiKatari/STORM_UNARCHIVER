using CommunityToolkit.Mvvm.ComponentModel;

namespace StormUnarchiver.Models;

public partial class FolderPair : ObservableObject
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];

    private string _watchFolder = string.Empty;
    private string _outputFolder = string.Empty;

    public string WatchFolder
    {
        get => _watchFolder;
        set
        {
            if (SetProperty(ref _watchFolder, value))
            {
                OnPropertyChanged(nameof(WatchDisplay));
                OnPropertyChanged(nameof(IsWatchSet));
                OnPropertyChanged(nameof(IsConfigured));
            }
        }
    }

    public string OutputFolder
    {
        get => _outputFolder;
        set
        {
            if (SetProperty(ref _outputFolder, value))
            {
                OnPropertyChanged(nameof(OutputDisplay));
                OnPropertyChanged(nameof(IsOutputSet));
                OnPropertyChanged(nameof(IsConfigured));
            }
        }
    }

    public string WatchDisplay => string.IsNullOrEmpty(WatchFolder)
        ? "Перетащите или выберите папку" : WatchFolder;

    public string OutputDisplay => string.IsNullOrEmpty(OutputFolder)
        ? "Перетащите или выберите папку" : OutputFolder;

    public bool IsWatchSet => !string.IsNullOrEmpty(WatchFolder);
    public bool IsOutputSet => !string.IsNullOrEmpty(OutputFolder);
    public bool IsConfigured => IsWatchSet && IsOutputSet;
}

public class FolderPairData
{
    public string Id { get; set; } = "";
    public string WatchFolder { get; set; } = "";
    public string OutputFolder { get; set; } = "";

    public FolderPair ToModel() => new()
    {
        Id = string.IsNullOrEmpty(Id) ? Guid.NewGuid().ToString("N")[..8] : Id,
        WatchFolder = WatchFolder,
        OutputFolder = OutputFolder
    };

    public static FolderPairData FromModel(FolderPair p) => new()
    {
        Id = p.Id,
        WatchFolder = p.WatchFolder,
        OutputFolder = p.OutputFolder
    };
}
