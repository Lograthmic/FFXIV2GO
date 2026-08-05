using System.IO;
using System.Windows;
using FFXIV2GO.Services;
using FFXIV2GO.Views;
using Microsoft.Win32;

namespace FFXIV2GO.ViewModels;

public sealed class InitViewModel : WizardViewModelBase
{
    public override string TitleKey => "Init.Title";
    public override string DescriptionKey => "Init.Desc";

    private string _ffxivPath = string.Empty;
    private bool _skipMods;
    private IReadOnlyList<AppEntry> _selectedApps = [];

    protected override void BuildSteps()
    {
        Steps.Add(new StepItem("Init.Step1", StepCheckGitHub));
        Steps.Add(new StepItem("Init.Step2", StepCreateDirs));
        Steps.Add(new StepItem("Init.Step3", StepSelectPath));
        Steps.Add(new StepItem("Init.Step4", StepCopyGame));
        Steps.Add(new StepItem("Init.Step5", StepBackupLauncher));
        Steps.Add(new StepItem("Init.Step6", StepBackupMods));
        Steps.Add(new StepItem("Init.Step7", StepDownloadRuntimes));
        Steps.Add(new StepItem("Init.Step8", StepSelectApps));
        Steps.Add(new StepItem("Init.Step9", StepDownloadApps));
    }

    protected override Task<bool> OnBeforeRunAsync()
    {
        if (!EnvironmentStatus.IsInitialized())
        {
            return Task.FromResult(true);
        }

        var choice = MessageBox.Show(
            L("Init.AlreadyInitialized"), "FFXIV2GO", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (choice != MessageBoxResult.Yes)
        {
            return Task.FromResult(false);
        }

        EnvironmentStatus.Reset();
        return Task.FromResult(true);
    }

    private static string L(string key) => LocalizationService.Instance[key];

    private static async Task StepCheckGitHub(StepContext ctx)
    {
        if (!await DownloadService.CheckGitHubAsync(ctx.CancellationToken))
        {
            throw new InvalidOperationException(L("Init.GitHubFail"));
        }
    }

    private static Task StepCreateDirs(StepContext ctx)
    {
        Directory.CreateDirectory(DeploymentRoot.Inst);
        Directory.CreateDirectory(DeploymentRoot.Conf);
        Directory.CreateDirectory(DeploymentRoot.Apps);
        return Task.CompletedTask;
    }

    private Task StepSelectPath(StepContext ctx)
    {
        var dialog = new OpenFolderDialog { Title = L("Init.SelectFfxivTitle") };
        if (dialog.ShowDialog() != true)
        {
            throw new OperationCanceledException(L("Common.Cancelled"));
        }

        _ffxivPath = dialog.FolderName;
        var gameFolder = Path.Combine(_ffxivPath, "game", "My Games", DeploymentRoot.GameConfigFolderName);
        if (!Directory.Exists(gameFolder))
        {
            throw new InvalidOperationException(L("Init.PathInvalid"));
        }

        return Task.CompletedTask;
    }

    private async Task StepCopyGame(StepContext ctx)
    {
        ctx.Log(L("Init.CopyGame"));
        var source = Path.Combine(_ffxivPath, "game", "My Games", DeploymentRoot.GameConfigFolderName);
        await Task.Run(() => FileSystemService.CopyDirectory(source, DeploymentRoot.GameConfig));
        ctx.Log(L("Init.GameDone"));
    }

    private async Task StepBackupLauncher(StepContext ctx)
    {
        var appdata = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XIVLauncherCN");

        if (!Directory.Exists(appdata))
        {
            var choice = MessageBox.Show(
                L("Init.NoLauncher"), "FFXIV2GO", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (choice != MessageBoxResult.Yes)
            {
                throw new OperationCanceledException(L("Common.Cancelled"));
            }

            Directory.CreateDirectory(DeploymentRoot.LauncherConfig);
            _skipMods = true;
            return;
        }

        await Task.Run(() => FileSystemService.CopyDirectory(appdata, DeploymentRoot.LauncherConfig));
        ctx.Log(L("Init.LauncherDone"));
    }

    private async Task StepBackupMods(StepContext ctx)
    {
        if (_skipMods)
        {
            return;
        }

        var modDir = PenumbraConfigService.GetModDirectory(DeploymentRoot.PenumbraJson);
        if (string.IsNullOrEmpty(modDir) || !Directory.Exists(modDir))
        {
            ctx.Log(L("Init.NoPenumbra"));
            return;
        }

        ctx.Log(L("Init.CopyMods"));
        await Task.Run(() => FileSystemService.CopyDirectory(modDir, DeploymentRoot.Mods));
        ctx.Log(L("Init.ModsDone"));
    }

    private static async Task StepDownloadRuntimes(StepContext ctx)
    {
        ctx.Log(L("Init.DlVc"));
        await DownloadService.DownloadAsync(DownloadService.VcRedistUrl, DeploymentRoot.VcRedist, ctx.Progress, ctx.CancellationToken);

        ctx.Log(L("Init.DlDotnet"));
        await DownloadService.DownloadAsync(DownloadService.DotNetRuntimeUrl, DeploymentRoot.DotNetRuntime, ctx.Progress, ctx.CancellationToken);
    }

    private Task StepSelectApps(StepContext ctx)
    {
        var manifest = AppManifestService.Load();
        if (manifest.Apps.Count == 0)
        {
            throw new InvalidOperationException(L("Init.ManifestMissing"));
        }

        var dialog = new AppSelectWindow(manifest);
        if (dialog.ShowDialog() != true)
        {
            throw new OperationCanceledException(L("Common.Cancelled"));
        }

        _selectedApps = dialog.Selected;
        return Task.CompletedTask;
    }

    private async Task StepDownloadApps(StepContext ctx)
    {
        if (_selectedApps.Count == 0)
        {
            ctx.Log(L("Init.NoAppsSelected"));
            return;
        }

        foreach (var app in _selectedApps)
        {
            var filePath = await AppInstallService.InstallAsync(
                app, ctx.Progress, msg => ctx.Log(msg), ctx.CancellationToken);

            if (app.PromptExtract && filePath is not null)
            {
                var targetDir = Path.Combine(DeploymentRoot.Apps, app.Target);
                var prompt = string.Format(L("Init.PromptExtract"), filePath, targetDir);
                MessageBox.Show(prompt, "FFXIV2GO", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (app.Type == AppType.Installer && filePath is not null)
            {
                ctx.Log(string.Format(L("Init.InstallerHint"), app.Name));
            }

            ctx.Log(string.Format(L("Init.AppDone"), app.Name));
        }
    }
}
