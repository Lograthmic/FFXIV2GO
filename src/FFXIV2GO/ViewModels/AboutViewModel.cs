using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using FFXIV2GO.Services;

namespace FFXIV2GO.ViewModels;

/// <summary>
/// 关于页：应用版本、仓库链接、环境信息。
/// </summary>
public sealed class AboutViewModel : LocalizedViewModel
{
    public string Title => LocalizationService.Instance["About.Title"];
    public string Description => LocalizationService.Instance["About.Desc"];

    public string VersionText { get; }
    public string DeploymentRootDisplay => DeploymentRoot.Path;
    public string ConfigFileDisplay => DeploymentRoot.ConfigFile;

    public IRelayCommand OpenGitHubCommand { get; }

    private const string RepositoryUrl = "https://github.com/Lograthmic/FFXIV2GO";

    public AboutViewModel(MainViewModel main)
    {
        VersionText = main.VersionText;
        OpenGitHubCommand = new RelayCommand(() =>
            Process.Start(new ProcessStartInfo { FileName = RepositoryUrl, UseShellExecute = true }));
    }
}
