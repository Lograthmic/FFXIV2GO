using System.Diagnostics;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFXIV2GO.Services;
using Wpf.Ui.Controls;

namespace FFXIV2GO.ViewModels;

/// <summary>
/// 关于页：应用版本、检查更新、仓库链接、环境信息。
/// </summary>
public sealed partial class AboutViewModel : LocalizedViewModel
{
    public string Title => LocalizationService.Instance["About.Title"];
    public string Description => LocalizationService.Instance["About.Desc"];

    public string VersionText { get; }
    public string CurrentVersion { get; }
    public string DeploymentRootDisplay => DeploymentRoot.Path;
    public string ConfigFileDisplay => DeploymentRoot.ConfigFile;

    [ObservableProperty]
    private bool _isChecking;

    [ObservableProperty]
    private bool _hasResult;

    [ObservableProperty]
    private bool _hasUpdate;

    [ObservableProperty]
    private string _latestVersion = string.Empty;

    [ObservableProperty]
    private string _resultMessage = string.Empty;

    [ObservableProperty]
    private InfoBarSeverity _severity = InfoBarSeverity.Informational;

    public IRelayCommand OpenGitHubCommand { get; }
    public IAsyncRelayCommand CheckUpdateCommand { get; }
    public IRelayCommand DownloadUpdateCommand { get; }

    private const string Repository = "Lograthmic/FFXIV2GO";
    private const string RepositoryUrl = "https://github.com/Lograthmic/FFXIV2GO";

    public AboutViewModel(MainViewModel main)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        CurrentVersion = version?.ToString(3) ?? "1.1.0";
        VersionText = main.VersionText;

        OpenGitHubCommand = new RelayCommand(() => OpenUrl(RepositoryUrl));
        DownloadUpdateCommand = new RelayCommand(() => OpenUrl($"{RepositoryUrl}/releases/latest"));
        CheckUpdateCommand = new AsyncRelayCommand(CheckUpdateAsync);
    }

    private async Task CheckUpdateAsync()
    {
        IsChecking = true;
        HasResult = false;
        HasUpdate = false;

        try
        {
            var latest = await GithubLatestResolver.GetLatestVersionAsync(Repository);
            if (string.IsNullOrEmpty(latest))
            {
                SetResult(InfoBarSeverity.Informational, L("About.UpdateUnknown"));
                return;
            }

            LatestVersion = latest;
            if (IsNewer(CurrentVersion, latest))
            {
                HasUpdate = true;
                SetResult(InfoBarSeverity.Success, string.Format(L("About.UpdateAvailable"), latest));
            }
            else
            {
                SetResult(InfoBarSeverity.Informational, L("About.UpToDate"));
            }
        }
        catch (Exception ex)
        {
            LogService.Error($"检查更新失败: {ex.Message}");
            SetResult(InfoBarSeverity.Error, L("About.UpdateFailed"));
        }
        finally
        {
            IsChecking = false;
        }
    }

    private void SetResult(InfoBarSeverity severity, string message)
    {
        Severity = severity;
        ResultMessage = message;
        HasResult = true;
    }

    private static bool IsNewer(string current, string latest)
    {
        if (!Version.TryParse(GithubLatestResolver.CleanVersion(current), out var currentVersion) ||
            !Version.TryParse(GithubLatestResolver.CleanVersion(latest), out var latestVersion))
        {
            return false;
        }

        return latestVersion > currentVersion;
    }

    private static void OpenUrl(string url)
        => Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });

    private static string L(string key) => LocalizationService.Instance[key];
}
