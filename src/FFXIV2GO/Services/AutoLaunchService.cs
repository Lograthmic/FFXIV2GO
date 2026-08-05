using System.Diagnostics;
using System.IO;

namespace FFXIV2GO.Services;

/// <summary>
/// 安装完成后自动启动配置中的应用（部署根 apps 目录内的便携应用）。
/// </summary>
public static class AutoLaunchService
{
    public static void LaunchConfiguredApps()
    {
        var names = AppConfig.Load().SplitAutoLaunchApps();
        if (names.Length == 0)
        {
            return;
        }

        var manifest = AppManifestService.Load();
        foreach (var name in names)
        {
            var app = manifest.Apps.FirstOrDefault(a =>
                string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));
            if (app is null)
            {
                LogService.Warn($"自动启动: 清单中不存在应用 {name}");
                continue;
            }

            var dir = Path.Combine(DeploymentRoot.Apps, app.Target);
            var exe = ShortcutService.FindMainExecutable(dir, app.Name);
            if (exe is null)
            {
                LogService.Warn($"自动启动: 未找到 {name} 的可执行文件");
                continue;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exe,
                    WorkingDirectory = Path.GetDirectoryName(exe),
                    UseShellExecute = false
                });
                LogService.Info($"已自动启动应用: {name} -> {exe}");
            }
            catch (Exception ex)
            {
                LogService.Error($"自动启动失败: {name} - {ex.Message}");
            }
        }
    }
}
