using System.IO;
using FFXIV2GO.Services;
using Microsoft.Win32;

namespace FFXIV2GO.ViewModels;

public sealed class UninstallViewModel : WizardViewModelBase
{
    public override string TitleKey => "Uninstall.Title";
    public override string DescriptionKey => "Uninstall.Desc";
    public override bool ShowStatus => true;
    public override StatusState Status => EnvironmentStatus.UninstallState();

    private string _gameFolder = string.Empty;

    protected override void BuildSteps()
    {
        Steps.Add(new StepItem("Uninstall.Step1", StepReadPath));
        Steps.Add(new StepItem("Uninstall.Step2", StepValidateJunction));
        Steps.Add(new StepItem("Uninstall.Step3", StepRemoveGameJunction));
        Steps.Add(new StepItem("Uninstall.Step4", StepRestoreGame));
        Steps.Add(new StepItem("Uninstall.Step5", StepRemoveLauncherJunction));
        Steps.Add(new StepItem("Uninstall.Step6", StepRestoreLauncher));
        Steps.Add(new StepItem("Uninstall.Step7", StepRemoveCaches));
        Steps.Add(new StepItem("Uninstall.Step8", StepRemoveShortcuts));
    }

    private static string L(string key) => LocalizationService.Instance[key];

    private Task StepReadPath(StepContext ctx)
    {
        var path = AppConfig.Load().FfxivPath.Trim();

        if (string.IsNullOrEmpty(path))
        {
            var dialog = new OpenFolderDialog { Title = L("Setup.InputPathTitle") };
            if (dialog.ShowDialog() != true)
            {
                throw new OperationCanceledException(L("Common.Cancelled"));
            }

            path = dialog.FolderName;
        }

        _gameFolder = Path.Combine(path, "game", "My Games", DeploymentRoot.GameConfigFolderName);
        return Task.CompletedTask;
    }

    private Task StepValidateJunction(StepContext ctx)
    {
        if (!Directory.Exists(_gameFolder))
        {
            throw new InvalidOperationException(L("Uninstall.JunctionMissing"));
        }

        return Task.CompletedTask;
    }

    private Task StepRemoveGameJunction(StepContext ctx)
    {
        JunctionService.Delete(_gameFolder);
        ctx.Log(L("Uninstall.RemoveGameJunctionDone"));
        return Task.CompletedTask;
    }

    private Task StepRestoreGame(StepContext ctx)
    {
        var old = _gameFolder + ".old";
        if (Directory.Exists(old))
        {
            Directory.Move(old, _gameFolder);
            ctx.Log(L("Uninstall.RestoreGameDone"));
        }
        else
        {
            ctx.Log(L("Uninstall.NoBackup"));
        }

        return Task.CompletedTask;
    }

    private Task StepRemoveLauncherJunction(StepContext ctx)
    {
        var appdata = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XIVLauncherCN");

        if (Directory.Exists(appdata))
        {
            JunctionService.Delete(appdata);
            ctx.Log(L("Uninstall.RemoveLauncherJunctionDone"));
        }

        return Task.CompletedTask;
    }

    private Task StepRestoreLauncher(StepContext ctx)
    {
        var dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var old = Path.Combine(dir, "XIVLauncherCN.old");

        if (Directory.Exists(old))
        {
            Directory.Move(old, Path.Combine(dir, "XIVLauncherCN"));
            ctx.Log(L("Uninstall.RestoreLauncherDone"));
        }
        else
        {
            ctx.Log(L("Uninstall.NoBackup"));
        }

        return Task.CompletedTask;
    }

    private Task StepRemoveCaches(StepContext ctx)
    {
        if (Directory.Exists(DesktopHelper.CachesFolder))
        {
            Directory.Delete(DesktopHelper.CachesFolder, true);
            ctx.Log(L("Uninstall.RemoveCachesDone"));
        }

        return Task.CompletedTask;
    }

    private Task StepRemoveShortcuts(StepContext ctx)
    {
        var desktop = DesktopHelper.GetDesktopPath();
        var manifest = AppManifestService.Load();
        ShortcutService.RemoveCreatedShortcuts(desktop, manifest);
        ctx.Log(L("Uninstall.ShortcutsRemoved"));
        return Task.CompletedTask;
    }
}
