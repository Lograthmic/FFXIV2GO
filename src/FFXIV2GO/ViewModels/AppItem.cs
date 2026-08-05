using CommunityToolkit.Mvvm.ComponentModel;
using FFXIV2GO.Services;

namespace FFXIV2GO.ViewModels;

/// <summary>
/// 应用管理列表项：单个可安装/卸载的便携应用。
/// </summary>
public sealed partial class AppItem : ObservableObject
{
    public AppEntry Entry { get; }

    public string Name => Entry.Name;
    public string Target => Entry.Target;

    [ObservableProperty]
    private bool _isInstalled;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private double _progress;

    public bool CanInstall => !IsInstalled && !IsBusy;
    public bool CanUninstall => IsInstalled && !IsBusy;

    partial void OnIsInstalledChanged(bool value)
    {
        OnPropertyChanged(nameof(CanInstall));
        OnPropertyChanged(nameof(CanUninstall));
    }

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanInstall));
        OnPropertyChanged(nameof(CanUninstall));
    }

    public AppItem(AppEntry entry)
    {
        Entry = entry;
    }
}
