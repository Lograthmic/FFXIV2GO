using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFXIV2GO.Services;
using Microsoft.Win32;

namespace FFXIV2GO.ViewModels;

public sealed partial class SettingsViewModel : LocalizedViewModel
{
    private readonly AppConfig _config;

    [ObservableProperty]
    private string _ffxivPath;

    [ObservableProperty]
    private string _selectedLanguage;

    [ObservableProperty]
    private string _selectedTheme;

    [ObservableProperty]
    private string _selectedLogLevel;

    [ObservableProperty]
    private bool _askOnClose;

    [ObservableProperty]
    private bool _isSaved;

    public string DeploymentRootDisplay => DeploymentRoot.Path;
    public string ConfigFileDisplay => DeploymentRoot.ConfigFile;
    public string LogFileDisplay => LogService.LogFilePath;

    public IReadOnlyList<OptionItem> LanguageOptions { get; }
    public IReadOnlyList<OptionItem> ThemeOptions { get; }
    public IReadOnlyList<OptionItem> LogLevelOptions { get; }

    public IRelayCommand BrowseCommand { get; }
    public IRelayCommand SaveCommand { get; }
    public IRelayCommand OpenLogCommand { get; }
    public IRelayCommand OpenLogFolderCommand { get; }

    public SettingsViewModel()
    {
        _config = AppConfig.Load();
        _ffxivPath = _config.FfxivPath;
        _selectedLanguage = _config.Language;
        _selectedTheme = _config.Theme;
        _selectedLogLevel = _config.LogLevel;
        _askOnClose = _config.AskOnClose;

        LanguageOptions =
        [
            new OptionItem { Value = "System", LabelKey = "Settings.Language.System" },
            new OptionItem { Value = "zh-CN", LabelKey = "Settings.Language.zh-CN" },
            new OptionItem { Value = "en", LabelKey = "Settings.Language.en" }
        ];

        ThemeOptions =
        [
            new OptionItem { Value = "System", LabelKey = "Settings.Theme.System" },
            new OptionItem { Value = "Light", LabelKey = "Settings.Theme.Light" },
            new OptionItem { Value = "Dark", LabelKey = "Settings.Theme.Dark" }
        ];

        LogLevelOptions =
        [
            new OptionItem { Value = "Debug", LabelKey = "Settings.LogLevel.Debug" },
            new OptionItem { Value = "Info", LabelKey = "Settings.LogLevel.Info" },
            new OptionItem { Value = "Warn", LabelKey = "Settings.LogLevel.Warn" },
            new OptionItem { Value = "Error", LabelKey = "Settings.LogLevel.Error" }
        ];

        BrowseCommand = new RelayCommand(Browse);
        SaveCommand = new RelayCommand(Save);
        OpenLogCommand = new RelayCommand(OpenLog);
        OpenLogFolderCommand = new RelayCommand(OpenLogFolder);
    }

    private static void OpenLog()
    {
        var file = LogService.LogFilePath;
        if (!File.Exists(file))
        {
            File.WriteAllText(file, string.Empty);
        }

        Process.Start(new ProcessStartInfo { FileName = file, UseShellExecute = true });
    }

    private static void OpenLogFolder()
    {
        var file = LogService.LogFilePath;
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{file}\"",
            UseShellExecute = true
        });
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        LocalizationService.Instance.SetLanguage(value);
        IsSaved = false;
    }

    partial void OnSelectedThemeChanged(string value)
    {
        ThemeService.Apply(value);
        IsSaved = false;
    }

    partial void OnSelectedLogLevelChanged(string value)
    {
        LogService.ApplyLevel(value);
        IsSaved = false;
    }

    private void Browse()
    {
        var dialog = new OpenFolderDialog
        {
            Title = LocalizationService.Instance["Settings.FfxivPath"]
        };
        if (dialog.ShowDialog() == true)
        {
            FfxivPath = dialog.FolderName;
            IsSaved = false;
        }
    }

    private void Save()
    {
        _config.FfxivPath = FfxivPath.Trim();
        _config.Language = SelectedLanguage;
        _config.Theme = SelectedTheme;
        _config.LogLevel = SelectedLogLevel;
        _config.AskOnClose = AskOnClose;
        _config.Save();
        IsSaved = true;
    }
}
