using CommunityToolkit.Mvvm.Input;
using FFXIV2GO.Services;
using Wpf.Ui.Controls;

namespace FFXIV2GO.ViewModels;

public sealed class DashboardViewModel : LocalizedViewModel
{
    public IReadOnlyList<DashboardCard> Cards { get; }

    public IRelayCommand<DashboardCard> OpenCommand { get; }

    public string DeploymentRootDisplay => DeploymentRoot.Path;
    public string ConfigFileDisplay => DeploymentRoot.ConfigFile;
    public string VersionText { get; }

    public DashboardViewModel(MainViewModel main)
    {
        OpenCommand = new RelayCommand<DashboardCard>(card =>
        {
            if (card is not null) main.Navigate(card.TargetKey);
        });
        VersionText = main.VersionText;

        Cards =
        [
            new DashboardCard { TitleKey = "Card.Init.Title", DescKey = "Card.Init.Desc", Symbol = SymbolRegular.Archive24, TargetKey = "init" },
            new DashboardCard { TitleKey = "Card.Setup.Title", DescKey = "Card.Setup.Desc", Symbol = SymbolRegular.PlugConnected24, TargetKey = "setup" },
            new DashboardCard { TitleKey = "Card.Uninstall.Title", DescKey = "Card.Uninstall.Desc", Symbol = SymbolRegular.Delete24, TargetKey = "uninstall" },
            new DashboardCard { TitleKey = "Card.Clean.Title", DescKey = "Card.Clean.Desc", Symbol = SymbolRegular.Broom24, TargetKey = "clean" }
        ];
    }
}
