using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFXIV2GO.Services;
using Wpf.Ui.Controls;

namespace FFXIV2GO.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private object _currentPage;

    public IReadOnlyList<NavItem> NavItems { get; }

    public string DeploymentRootDisplay => DeploymentRoot.Path;

    public string VersionText { get; }

    public IRelayCommand BackCommand { get; }

    public MainViewModel()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText = $"{LocalizationService.Instance["App.Version"]} {version?.ToString(3) ?? "1.1.0"}";

        var dashboard = new DashboardViewModel(this);

        NavItems =
        [
            new NavItem { Key = "dashboard", TitleKey = "Nav.Dashboard", Symbol = SymbolRegular.Home24 },
            new NavItem { Key = "init", TitleKey = "Nav.Init", Symbol = SymbolRegular.Archive24 },
            new NavItem { Key = "setup", TitleKey = "Nav.Setup", Symbol = SymbolRegular.PlugConnected24 },
            new NavItem { Key = "uninstall", TitleKey = "Nav.Uninstall", Symbol = SymbolRegular.Delete24 },
            new NavItem { Key = "clean", TitleKey = "Nav.Clean", Symbol = SymbolRegular.Broom24 },
            new NavItem { Key = "apps", TitleKey = "Nav.Apps", Symbol = SymbolRegular.AppStore24 },
            new NavItem { Key = "settings", TitleKey = "Nav.Settings", Symbol = SymbolRegular.Settings24 }
        ];

        BackCommand = new RelayCommand(() => Navigate("dashboard"));
        _currentPage = dashboard;
    }

    public void Navigate(string key) =>
        CurrentPage = key switch
        {
            "init" => new InitViewModel(),
            "setup" => new SetupViewModel(Navigate),
            "uninstall" => new UninstallViewModel(),
            "clean" => new CleanViewModel(),
            "apps" => new AppManagerViewModel(),
            "settings" => new SettingsViewModel(),
            _ => new DashboardViewModel(this)
        };
}
