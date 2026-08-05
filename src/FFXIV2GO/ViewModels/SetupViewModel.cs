using System.IO;
using System.Windows;
using FFXIV2GO.Services;
using Microsoft.Win32;

namespace FFXIV2GO.ViewModels;

public sealed class SetupViewModel : WizardViewModelBase
{
    public override string TitleKey => "Setup.Title";
    public override string DescriptionKey => "Setup.Desc";

    private string _ffxivPath = string.Empty;
    private string _gameFolder = string.Empty;

    public SetupViewModel(Action<string>? navigate = null) : base(navigate)
    {
    }

    protected override void BuildSteps()
    {
        Steps.Add(new StepItem("Setup.Step1", StepReadPath));
        Steps.Add(new StepItem("Setup.Step2", StepValidateGame));
        Steps.Add(new StepItem("Setup.Step3", StepBackupGame));
        Steps.Add(new StepItem("Setup.Step4", StepJunctionGame));
        Steps.Add(new StepItem("Setup.Step5", StepBackupLauncher));
        Steps.Add(new StepItem("Setup.Step6", StepJunctionLauncher));
        Steps.Add(new StepItem("Setup.Step7", StepCreateCaches));
        Steps.Add(new StepItem("Setup.Step8", StepUpdatePenumbra));
        Steps.Add(new StepItem("Setup.Step9", StepInstallRuntimes));
        Steps.Add(new StepItem("Setup.Step10", StepCreateShortcuts));
    }

    protected override Task<bool> OnBeforeRunAsync()
    {
        if (!EnvironmentStatus.IsInitialized())
        {
            var choice = MessageBox.Show(
                L("Setup.NotInitialized"), "FFXIV2GO", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (choice == MessageBoxResult.Yes)
            {
                Navigate?.Invoke("init");
            }

            return Task.FromResult(false);
        }

        var confirm = MessageBox.Show(
            L("Setup.ConfirmMount"), "FFXIV2GO", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        return Task.FromResult(confirm == MessageBoxResult.Yes);
    }

    private static string L(string key) => LocalizationService.Instance[key];

    private Task StepReadPath(StepContext ctx)
    {
        var config = AppConfig.Load();
        _ffxivPath = config.FfxivPath.Trim();

        if (string.IsNullOrEmpty(_ffxivPath))
        {
            var dialog = new OpenFolderDialog { Title = L("Setup.InputPathTitle") };
            if (dialog.ShowDialog() != true)
            {
                throw new OperationCanceledException(L("Common.Cancelled"));
            }

            _ffxivPath = dialog.FolderName;
            config.FfxivPath = _ffxivPath;
            config.Save();
        }

        return Task.CompletedTask;
    }

    private Task StepValidateGame(StepContext ctx)
    {
        _gameFolder = Path.Combine(_ffxivPath, "game", "My Games", DeploymentRoot.GameConfigFolderName);
        if (!Directory.Exists(_gameFolder))
        {
            throw new InvalidOperationException(L("Setup.GameInvalid"));
        }

        return Task.CompletedTask;
    }

    private Task StepBackupGame(StepContext ctx)
    {
        var old = _gameFolder + ".old";
        if (Directory.Exists(old))
        {
            Directory.Delete(old, true);
        }

        Directory.Move(_gameFolder, old);
        ctx.Log(L("Setup.BackupGameDone"));
        return Task.CompletedTask;
    }

    private Task StepJunctionGame(StepContext ctx)
    {
        if (!Directory.Exists(DeploymentRoot.GameConfig))
        {
            throw new DirectoryNotFoundException(DeploymentRoot.GameConfig);
        }

        try
        {
            JunctionService.Create(_gameFolder, DeploymentRoot.GameConfig);
        }
        catch
        {
            // 回滚：恢复原目录
            var old = _gameFolder + ".old";
            if (Directory.Exists(old))
            {
                Directory.Move(old, _gameFolder);
            }

            throw;
        }

        ctx.Log(L("Setup.JunctionGameDone"));
        return Task.CompletedTask;
    }

    private Task StepBackupLauncher(StepContext ctx)
    {
        var appdata = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XIVLauncherCN");
        var old = appdata + ".old";

        if (Directory.Exists(old))
        {
            Directory.Delete(old, true);
        }

        if (Directory.Exists(appdata))
        {
            Directory.Move(appdata, old);
        }

        ctx.Log(L("Setup.BackupLauncherDone"));
        return Task.CompletedTask;
    }

    private Task StepJunctionLauncher(StepContext ctx)
    {
        if (!Directory.Exists(DeploymentRoot.LauncherConfig))
        {
            throw new DirectoryNotFoundException(DeploymentRoot.LauncherConfig);
        }

        var appdata = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XIVLauncherCN");
        JunctionService.Create(appdata, DeploymentRoot.LauncherConfig);
        ctx.Log(L("Setup.JunctionLauncherDone"));
        return Task.CompletedTask;
    }

    private Task StepCreateCaches(StepContext ctx)
    {
        Directory.CreateDirectory(DesktopHelper.CachesFolder);
        ctx.Log(L("Setup.CachesDone"));
        return Task.CompletedTask;
    }

    private Task StepUpdatePenumbra(StepContext ctx)
    {
        if (!File.Exists(DeploymentRoot.PenumbraJson))
        {
            ctx.Log(L("Setup.PenumbraSkipped"));
            return Task.CompletedTask;
        }

        PenumbraConfigService.SetModDirectory(DeploymentRoot.PenumbraJson, DeploymentRoot.Mods);
        ctx.Log(L("Setup.PenumbraUpdated"));
        return Task.CompletedTask;
    }

    private static async Task StepInstallRuntimes(StepContext ctx)
    {
        bool any = false;

        if (File.Exists(DeploymentRoot.VcRedist))
        {
            any = true;
            ctx.Log(L("Setup.InstallVc"));
            await RuntimeInstaller.InstallAsync(DeploymentRoot.VcRedist, ctx.CancellationToken);
        }

        if (File.Exists(DeploymentRoot.DotNetRuntime))
        {
            any = true;
            ctx.Log(L("Setup.InstallDotnet"));
            await RuntimeInstaller.InstallAsync(DeploymentRoot.DotNetRuntime, ctx.CancellationToken);
        }

        ctx.Log(any ? L("Setup.RuntimeDone") : L("Setup.RuntimeMissing"));
    }

    private static Task StepCreateShortcuts(StepContext ctx)
    {
        var desktop = DesktopHelper.GetDesktopPath();
        var manifest = AppManifestService.Load();

        var appCount = ShortcutService.CreateAppShortcuts(manifest, desktop, msg => ctx.Log(msg));
        ShortcutService.CreateSelfShortcut(desktop);
        ShortcutService.CreateFolderShortcut(desktop, ShortcutService.FolderShortcutName);

        ctx.Log(string.Format(L("Setup.ShortcutsDone"), appCount + 2));
        return Task.CompletedTask;
    }
}
