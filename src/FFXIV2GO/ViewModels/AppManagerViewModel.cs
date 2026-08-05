using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using FFXIV2GO.Services;

namespace FFXIV2GO.ViewModels;

/// <summary>
/// 应用管理页：随时可安装/卸载 apps 目录中的便携应用（卸载即删除应用文件夹）。
/// </summary>
public sealed partial class AppManagerViewModel : LocalizedViewModel
{
    public ObservableCollection<AppItem> Items { get; } = [];

    public string Title => LocalizationService.Instance["AppManager.Title"];
    public string Description => LocalizationService.Instance["AppManager.Description"];

    public IAsyncRelayCommand<AppItem> InstallCommand { get; }
    public IAsyncRelayCommand<AppItem> UninstallCommand { get; }
    public IRelayCommand RefreshCommand { get; }

    private readonly AppManifest _manifest;

    public AppManagerViewModel()
    {
        _manifest = AppManifestService.Load();

        foreach (var app in _manifest.Apps)
        {
            Items.Add(new AppItem(app));
        }

        RefreshCommand = new RelayCommand(Refresh);
        InstallCommand = new AsyncRelayCommand<AppItem>(InstallAsync, item => item is not null && !item.IsBusy);
        UninstallCommand = new AsyncRelayCommand<AppItem>(UninstallAsync, item => item is not null && !item.IsBusy);

        Refresh();
    }

    private static string L(string key) => LocalizationService.Instance[key];

    private void Refresh()
    {
        foreach (var item in Items)
        {
            item.IsInstalled = AppInstallService.IsInstalled(item.Entry);
        }
    }

    private async Task InstallAsync(AppItem? item)
    {
        if (item is null)
        {
            return;
        }

        item.IsBusy = true;
        item.Progress = 0;
        item.Status = L("AppManager.Status.Installing");
        NotifyCommands();

        try
        {
            var filePath = await AppInstallService.InstallAsync(
                item.Entry,
                new Progress<double>(v => item.Progress = v),
                msg => item.Status = msg);

            if (item.Entry.PromptExtract && filePath is not null)
            {
                var targetDir = Path.Combine(DeploymentRoot.Apps, item.Entry.Target);
                var prompt = string.Format(L("Init.PromptExtract"), filePath, targetDir);
                MessageBox.Show(prompt, "FFXIV2GO", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            item.IsInstalled = AppInstallService.IsInstalled(item.Entry);
            item.Status = item.IsInstalled
                ? L("AppManager.Status.Installed")
                : L("AppManager.Status.NotInstalled");
        }
        catch (Exception ex)
        {
            item.Status = ex.Message;
        }
        finally
        {
            item.IsBusy = false;
            item.Progress = 0;
            NotifyCommands();
        }
    }

    private async Task UninstallAsync(AppItem? item)
    {
        if (item is null || !item.IsInstalled)
        {
            return;
        }

        var confirm = MessageBox.Show(
            string.Format(L("AppManager.ConfirmUninstall"), item.Name),
            "FFXIV2GO", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        if (AppInstallService.IsRunning(item.Entry))
        {
            MessageBox.Show(
                string.Format(L("AppManager.RunningWarning"), item.Name),
                "FFXIV2GO", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        item.IsBusy = true;
        item.Status = L("AppManager.Status.Uninstalling");
        NotifyCommands();

        try
        {
            await Task.Run(() => AppInstallService.Uninstall(item.Entry));
            item.IsInstalled = false;
            item.Status = L("AppManager.Status.NotInstalled");
        }
        catch (Exception ex)
        {
            item.Status = ex.Message;
        }
        finally
        {
            item.IsBusy = false;
            NotifyCommands();
        }
    }

    private void NotifyCommands()
    {
        InstallCommand.NotifyCanExecuteChanged();
        UninstallCommand.NotifyCanExecuteChanged();
    }
}
