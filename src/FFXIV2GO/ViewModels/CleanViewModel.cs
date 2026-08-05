using System.IO;
using System.Windows;
using FFXIV2GO.Services;

namespace FFXIV2GO.ViewModels;

public sealed class CleanViewModel : WizardViewModelBase
{
    public override string TitleKey => "Clean.Title";
    public override string DescriptionKey => "Clean.Desc";

    private int _count;
    private long _totalSize;
    private long _freed;

    protected override void BuildSteps()
    {
        Steps.Add(new StepItem("Clean.Step1", StepScan));
        Steps.Add(new StepItem("Clean.Step2", StepConfirmAndDelete));
        Steps.Add(new StepItem("Clean.Step3", StepReport));
    }

    private static string L(string key) => LocalizationService.Instance[key];

    private async Task StepScan(StepContext ctx)
    {
        ctx.Log(L("Clean.Scanning"));

        (_count, _totalSize) = await Task.Run(() => DiskService.ScanCleanableFiles(DeploymentRoot.Path));

        if (_count == 0)
        {
            ctx.Log(L("Clean.Nothing"));
            StopExecution = true;
            return;
        }

        ctx.Log(string.Format(L("Clean.ScanResult"), _count, DiskService.ConvertSize(_totalSize)));
    }

    private async Task StepConfirmAndDelete(StepContext ctx)
    {
        var confirm = MessageBox.Show(
            string.Format(L("Clean.ConfirmDelete"), _count, DiskService.ConvertSize(_totalSize)),
            "FFXIV2GO", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
        {
            throw new OperationCanceledException(L("Common.Cancelled"));
        }

        ctx.Log(L("Clean.Deleting"));

        var progress = new Progress<(string File, long Freed)>(p =>
        {
            ctx.Progress.Report(Math.Min(1.0, (double)p.Freed / Math.Max(1, _totalSize)));
            ctx.Log(Path.GetFileName(p.File));
        });

        _freed = await Task.Run(() => DiskService.DeleteCleanableFilesAsync(DeploymentRoot.Path, progress, ctx.CancellationToken));
    }

    private Task StepReport(StepContext ctx)
    {
        ctx.Log(string.Format(L("Clean.Freed"), DiskService.ConvertSize(_freed)));
        return Task.CompletedTask;
    }
}
