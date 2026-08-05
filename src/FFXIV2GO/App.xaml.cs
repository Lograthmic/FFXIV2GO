using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using FFXIV2GO.Services;
using FFXIV2GO.ViewModels;
using FFXIV2GO.Views;

namespace FFXIV2GO;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;

        DownloadService.CleanupTempDownloads();

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?";
        LogService.Info($"应用启动 v{version}，部署根: {DeploymentRoot.Path}，日志: {LogService.LogFilePath}");

        var config = AppConfig.Load();
        LocalizationService.Instance.SetLanguage(config.Language);
        LogService.ApplyLevel(config.LogLevel);
        LogService.Info($"配置加载完成，语言: {config.Language}，主题: {config.Theme}，日志级别: {config.LogLevel}");

        if (!File.Exists(DeploymentRoot.ConfigFile))
        {
            var firstRun = new FirstRunWindow();
            if (firstRun.ShowDialog() == true)
            {
                config.Language = firstRun.SelectedLanguage;
                config.Save();
                LocalizationService.Instance.SetLanguage(config.Language);
                LogService.Info($"首次运行选择语言: {config.Language}");
            }
        }

        var window = new MainWindow
        {
            DataContext = new MainViewModel()
        };
        MainWindow = window;
        ThemeService.Apply(config.Theme);
        window.Show();
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogService.Error($"未处理异常: {e.Exception}");

        try
        {
            var log = Path.Combine(Path.GetTempPath(), "ffxiv2go-crash.log");
            File.AppendAllText(log,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {e.Exception}\n{e.Exception.StackTrace}\n\n");
        }
        catch
        {
            // ignore logging failures
        }

        MessageBox.Show(e.Exception.Message, "FFXIV2GO", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
