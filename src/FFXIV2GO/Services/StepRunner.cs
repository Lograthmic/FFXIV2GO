using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FFXIV2GO.Services;

public enum StepState
{
    Pending,
    Running,
    Completed,
    Failed,
    Skipped
}

/// <summary>
/// 单个步骤项：标题（本地化键）、状态、详情、进度（0~100）。
/// </summary>
public sealed partial class StepItem : LocalizedViewModel
{
    public StepItem(string titleKey, Func<StepContext, Task>? execute = null)
    {
        TitleKey = titleKey;
        Execute = execute;
    }

    public string TitleKey { get; }
    public Func<StepContext, Task>? Execute { get; }

    public string Title => LocalizationService.Instance[TitleKey];

    [ObservableProperty]
    private StepState _state = StepState.Pending;

    [ObservableProperty]
    private string _detail = string.Empty;

    [ObservableProperty]
    private double _progress;

    public void Reset()
    {
        State = StepState.Pending;
        Detail = string.Empty;
        Progress = 0;
    }
}

/// <summary>
/// 步骤执行上下文：日志与进度都直接反映到所属步骤。
/// </summary>
public sealed class StepContext
{
    public required StepItem Step { get; init; }
    public CancellationToken CancellationToken { get; init; }

    public IProgress<double> Progress { get; set; } = null!;

    public void Log(string message) => Step.Detail = message;
}

/// <summary>
/// 向导基类：顺序执行步骤列表，推送状态/日志/进度，支持取消。
/// </summary>
public abstract partial class WizardViewModelBase : LocalizedViewModel
{
    public ObservableCollection<StepItem> Steps { get; } = [];

    public abstract string TitleKey { get; }
    public abstract string DescriptionKey { get; }

    public string Title => LocalizationService.Instance[TitleKey];
    public string Description => LocalizationService.Instance[DescriptionKey];

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _statusText = string.Empty;

    protected bool IsCancelled;

    /// <summary>置为 true 时，后续步骤标记为跳过并结束本次运行（不视为取消）。</summary>
    protected bool StopExecution { get; set; }

    public IAsyncRelayCommand RunCommand { get; }
    public IRelayCommand CancelCommand { get; }

    /// <summary>页面导航回调（供向导跳转到其他页面）。</summary>
    protected Action<string>? Navigate { get; }

    protected WizardViewModelBase(Action<string>? navigate = null)
    {
        Navigate = navigate;
        RunCommand = new AsyncRelayCommand(RunAsync, () => !IsRunning);
        CancelCommand = new RelayCommand(() => IsCancelled = true, () => IsRunning);
        BuildSteps();
    }

    protected abstract void BuildSteps();

    /// <summary>运行前钩子：返回 false 则取消本次运行（用于确认对话框）。</summary>
    protected virtual Task<bool> OnBeforeRunAsync() => Task.FromResult(true);

    protected async Task RunAsync()
    {
        if (!await OnBeforeRunAsync())
        {
            return;
        }

        IsCancelled = false;
        StopExecution = false;
        IsRunning = true;
        RunCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();

        StatusText = LocalizationService.Instance["Wizard.Running"];
        foreach (var step in Steps)
            step.Reset();

        foreach (var step in Steps)
        {
            if (IsCancelled || StopExecution)
            {
                step.State = StepState.Skipped;
                continue;
            }

            if (step.Execute is null)
            {
                step.State = StepState.Skipped;
                continue;
            }

            step.State = StepState.Running;
            LogService.Debug($"步骤开始: {step.TitleKey}");
            var ctx = new StepContext
            {
                Step = step,
                CancellationToken = CancellationToken.None
            };
            ctx.Progress = new Progress<double>(v => step.Progress = Math.Clamp(v * 100, 0, 100));

            try
            {
                await step.Execute(ctx);
                step.State = StepState.Completed;
                LogService.Debug($"步骤完成: {step.TitleKey}");
            }
            catch (OperationCanceledException)
            {
                step.State = StepState.Skipped;
                step.Detail = LocalizationService.Instance["Wizard.Cancelled"];
                LogService.Warn($"步骤已取消: {step.TitleKey}");
                break;
            }
            catch (Exception ex)
            {
                step.State = StepState.Failed;
                step.Detail = ex.Message;
                StatusText = ex.Message;
                LogService.Error($"步骤失败: {step.TitleKey} - {ex.Message}");
                break;
            }
        }

        IsRunning = false;
        RunCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
        StatusText = LocalizationService.Instance[IsCancelled ? "Wizard.Cancelled" : "Wizard.Finished"];
    }
}
