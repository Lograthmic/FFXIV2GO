using System.Reflection;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFXIV2GO.Services;
using Wpf.Ui.Controls;

namespace FFXIV2GO.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private object _currentPage;

    [ObservableProperty]
    private bool _fabIsPlay = true;

    [ObservableProperty]
    private bool _isToastOpen;

    [ObservableProperty]
    private string _toastMessage = string.Empty;

    [ObservableProperty]
    private InfoBarSeverity _toastSeverity = InfoBarSeverity.Success;

    private CancellationTokenSource? _toastCts;

    private readonly SettingsViewModel _settings;

    public IReadOnlyList<NavItem> NavItems { get; }

    public string DeploymentRootDisplay => DeploymentRoot.Path;

    public string VersionText { get; }

    public IRelayCommand BackCommand { get; }
    public IAsyncRelayCommand FabCommand { get; }

    public string FabTooltip => !EnvironmentStatus.IsInitialized()
        ? LocalizationService.Instance["Fab.TooltipInit"]
        : !EnvironmentStatus.IsInstalled()
            ? LocalizationService.Instance["Fab.TooltipSetup"]
            : LocalizationService.Instance["Fab.TooltipUninstall"];

    public MainViewModel()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText = $"{LocalizationService.Instance["App.Version"]} {version?.ToString(3) ?? "2.0.0"}";

        var dashboard = new DashboardViewModel(this);

        NavItems =
        [
            new NavItem { Key = "dashboard", TitleKey = "Nav.Dashboard", Symbol = SymbolRegular.Home24 },
            new NavItem { Key = "init", TitleKey = "Nav.Init", Symbol = SymbolRegular.Archive24 },
            new NavItem { Key = "setup", TitleKey = "Nav.Setup", Symbol = SymbolRegular.PlugConnected24 },
            new NavItem { Key = "uninstall", TitleKey = "Nav.Uninstall", Symbol = SymbolRegular.Delete24 },
            new NavItem { Key = "clean", TitleKey = "Nav.Clean", Symbol = SymbolRegular.Broom24 },
            new NavItem { Key = "apps", TitleKey = "Nav.Apps", Symbol = SymbolRegular.AppStore24 },
            new NavItem { Key = "settings", TitleKey = "Nav.Settings", Symbol = SymbolRegular.Settings24 },
            new NavItem { Key = "about", TitleKey = "Nav.About", Symbol = SymbolRegular.Info24 }
        ];

        BackCommand = new RelayCommand(() => Navigate("dashboard"));
        FabCommand = new AsyncRelayCommand(OnFabAsync);
        _currentPage = dashboard;
        _settings = new SettingsViewModel();
        RefreshFabState();
    }

    public void Navigate(string key) =>
        CurrentPage = key switch
        {
            "init" => new InitViewModel(),
            "setup" => new SetupViewModel(Navigate),
            "uninstall" => new UninstallViewModel(),
            "clean" => new CleanViewModel(),
            "apps" => new AppManagerViewModel(),
            "settings" => _settings,
            "about" => new AboutViewModel(this),
            _ => new DashboardViewModel(this)
        };

    partial void OnCurrentPageChanged(object value)
    {
        OnPropertyChanged(nameof(FabTooltip));
        RefreshFabState();
    }

    private void RefreshFabState()
    {
        FabIsPlay = !EnvironmentStatus.IsInstalled();
        OnPropertyChanged(nameof(FabTooltip));
    }

    private async Task OnFabAsync()
    {
        if (!EnvironmentStatus.IsInitialized())
        {
            System.Windows.MessageBox.Show(L("Fab.NotInitialized"), "FFXIV2GO",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            Navigate("init");
            return;
        }

        if (!EnvironmentStatus.IsInstalled())
        {
            await RunSetupAsync();
        }
        else
        {
            await RunUninstallAsync();
        }

        RefreshFabState();
    }

    /// <summary>后台静默执行安装流程（不跳转页面），完成后 toast 通知。</summary>
    private async Task RunSetupAsync()
    {
        var setup = new SetupViewModel(Navigate);
        try
        {
            await setup.RunCommand.ExecuteAsync(null);
            if (setup.LastRunSucceeded)
            {
                AutoLaunchService.LaunchConfiguredApps();
                ShowToast(L("Fab.SetupDone"), InfoBarSeverity.Success);
            }
            else
            {
                ShowToast(L("Fab.SetupCancelled"), InfoBarSeverity.Informational);
            }
        }
        catch (Exception ex)
        {
            LogService.Error($"FAB 安装异常: {ex.Message}");
            ShowToast(L("Fab.Error") + ex.Message, InfoBarSeverity.Error);
        }
    }

    /// <summary>后台静默执行卸载流程（不跳转页面），完成后 toast 通知。</summary>
    private async Task RunUninstallAsync()
    {
        var uninstall = new UninstallViewModel();
        try
        {
            await uninstall.RunCommand.ExecuteAsync(null);
            if (uninstall.LastRunSucceeded)
            {
                ShowToast(L("Fab.UninstallDone"), InfoBarSeverity.Success);
            }
            else
            {
                ShowToast(L("Fab.UninstallCancelled"), InfoBarSeverity.Informational);
            }
        }
        catch (Exception ex)
        {
            LogService.Error($"FAB 卸载异常: {ex.Message}");
            ShowToast(L("Fab.Error") + ex.Message, InfoBarSeverity.Error);
        }
    }

    private void ShowToast(string message, InfoBarSeverity severity)
    {
        _toastCts?.Cancel();
        _toastCts = new CancellationTokenSource();
        ToastMessage = message;
        ToastSeverity = severity;
        IsToastOpen = true;
        _ = HideToastAfterDelay(_toastCts.Token);
    }

    private async Task HideToastAfterDelay(CancellationToken ct)
    {
        try
        {
            await Task.Delay(5000, ct);
        }
        catch
        {
            return;
        }

        IsToastOpen = false;
    }

    private static string L(string key) => LocalizationService.Instance[key];
}
