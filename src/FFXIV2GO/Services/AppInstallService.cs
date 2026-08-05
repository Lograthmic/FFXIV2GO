using System.IO;

namespace FFXIV2GO.Services;

/// <summary>
/// 应用安装/卸载：解析地址 → 下载 → 按类型解压/保存；卸载即删除应用目标文件夹（所有应用均为 portable）。
/// </summary>
public static class AppInstallService
{
    /// <summary>
    /// 安装单个应用。返回：
    /// - archive：解压完成，返回 null；
    /// - portable/installer：返回保存的文件路径（供调用方处理 promptExtract 提示）。
    /// </summary>
    public static async Task<string?> InstallAsync(
        AppEntry app,
        IProgress<double>? progress = null,
        Action<string>? log = null,
        CancellationToken ct = default,
        string? appsRoot = null,
        string? instRoot = null)
    {
        appsRoot ??= DeploymentRoot.Apps;
        instRoot ??= DeploymentRoot.Inst;

        var url = await ResolveUrlAsync(app, log);
        var targetDir = Path.Combine(appsRoot, app.Target);
        var downloadBase = string.Equals(app.DownloadFolder, "inst", StringComparison.OrdinalIgnoreCase)
            ? instRoot
            : appsRoot;

        LogService.Info($"安装应用: {app.Name} (类型={app.Type}, 目标={targetDir})");

        switch (app.Type)
        {
            case AppType.Archive:
            {
                // 下载到系统临时目录，直接从临时目录解压到目标（大文件不落 U盘/网盘）
                log?.Invoke(string.Format(LocalizationService.Instance["Init.Downloading"], app.Name));
                var archive = await DownloadService.DownloadToTempAsync(url, app.FileName, progress, ct);

                log?.Invoke(string.Format(LocalizationService.Instance["Init.Extracting"], app.Name));
                await Task.Run(() => ArchiveService.Extract7z(archive, targetDir, progress), ct);

                try
                {
                    File.Delete(archive);
                }
                catch
                {
                    // 临时文件清理失败忽略
                }

                return null;
            }

            case AppType.Portable:
            case AppType.Installer:
            {
                log?.Invoke(string.Format(LocalizationService.Instance["Init.Downloading"], app.Name));
                Directory.CreateDirectory(targetDir);
                var filePath = Path.Combine(downloadBase, app.FileName);
                var temp = await DownloadService.DownloadToTempAsync(url, app.FileName, progress, ct);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                File.Move(temp, filePath, overwrite: true);
                return filePath;
            }

            default:
                throw new InvalidOperationException($"未知的应用类型: {app.Type}");
        }
    }

    public static bool IsInstalled(AppEntry app, string? appsRoot = null)
    {
        appsRoot ??= DeploymentRoot.Apps;
        var targetDir = Path.Combine(appsRoot, app.Target);
        if (!Directory.Exists(targetDir))
        {
            return false;
        }

        return Directory.EnumerateFileSystemEntries(targetDir).Any();
    }

    /// <summary>卸载 = 删除应用目标文件夹（递归）。删除失败（如应用正在运行）时抛出友好提示。</summary>
    public static void Uninstall(AppEntry app, string? appsRoot = null)
    {
        appsRoot ??= DeploymentRoot.Apps;
        var targetDir = Path.Combine(appsRoot, app.Target);
        if (!Directory.Exists(targetDir))
        {
            return;
        }

        LogService.Info($"卸载应用: {app.Name}，删除文件夹: {targetDir}");

        try
        {
            Directory.Delete(targetDir, true);
            LogService.Info($"卸载完成: {app.Name}");
        }
        catch (IOException)
        {
            LogService.Error($"卸载失败(文件被占用): {app.Name}");
            throw new IOException(string.Format(
                LocalizationService.Instance["AppManager.DeleteFailed"], app.Name));
        }
        catch (UnauthorizedAccessException)
        {
            LogService.Error($"卸载失败(无权限): {app.Name}");
            throw new UnauthorizedAccessException(string.Format(
                LocalizationService.Instance["AppManager.DeleteFailed"], app.Name));
        }
    }

    /// <summary>检测应用目标目录中的 exe 是否有进程正在运行。</summary>
    public static bool IsRunning(AppEntry app, string? appsRoot = null)
    {
        appsRoot ??= DeploymentRoot.Apps;
        var targetDir = Path.Combine(appsRoot, app.Target);
        if (!Directory.Exists(targetDir))
        {
            return false;
        }

        var exeNames = Directory.EnumerateFiles(targetDir, "*.exe", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (exeNames.Count == 0)
        {
            return false;
        }

        foreach (var process in System.Diagnostics.Process.GetProcesses())
        {
            try
            {
                if (!string.IsNullOrEmpty(process.ProcessName) && exeNames.Contains(process.ProcessName))
                {
                    return true;
                }
            }
            catch
            {
                // 无权限访问的进程（其他用户）跳过
            }
            finally
            {
                process.Dispose();
            }
        }

        return false;
    }

    public static async Task<string> ResolveUrlAsync(AppEntry app, Action<string>? log = null)
    {
        if (!string.IsNullOrEmpty(app.Url))
        {
            return app.Url;
        }

        if (app.GithubLatest is null || string.IsNullOrEmpty(app.GithubLatest.Repo))
        {
            throw new InvalidOperationException(LocalizationService.Instance["Init.ManifestMissing"]);
        }

        log?.Invoke(string.Format(LocalizationService.Instance["Init.Resolving"], app.Name));
        var url = await GithubLatestResolver.ResolveAsync(
            app.GithubLatest.Repo, app.GithubLatest.AssetPattern);
        if (url is null)
        {
            throw new InvalidOperationException(string.Format(
                LocalizationService.Instance["Init.GithubNoMatch"],
                app.GithubLatest.Repo, app.GithubLatest.AssetPattern));
        }

        return url;
    }
}
