using System.IO;
using System.Net.Http;

namespace FFXIV2GO.Services;

/// <summary>
/// 下载服务：HttpClient 流式下载 + 进度回调。
/// 下载先写入系统临时目录（避免直接写 U盘/网盘导致读写性能问题），再由调用方移动到最终位置或直接从临时目录解压。
/// </summary>
public static class DownloadService
{
    private static readonly HttpClient Client = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = true,
        AutomaticDecompression = System.Net.DecompressionMethods.All
    })
    {
        Timeout = TimeSpan.FromMinutes(30)
    };

    public const string VcRedistUrl = "https://aka.ms/vc14/vc_redist.x64.exe";
    public const string DotNetRuntimeUrl = "https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe";

    public static string TempDownloadDir => Path.Combine(Path.GetTempPath(), "FFXIV2GO_dl");

    /// <summary>检测 GitHub 连通性。</summary>
    public static async Task<bool> CheckGitHubAsync(CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, "https://github.com");
            using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>下载到系统临时目录后移动到最终目标。</summary>
    public static async Task DownloadAsync(
        string url,
        string destination,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        var temp = await DownloadToTempAsync(url, Path.GetFileName(destination), progress, ct);

        var dir = Path.GetDirectoryName(destination);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.Move(temp, destination, overwrite: true);
        LogService.Info($"已移动到最终位置: {destination}");
    }

    /// <summary>
    /// 下载到系统临时目录，返回临时文件路径（避免直接写 U盘/网盘导致读写性能问题）。
    /// 下载失败时自动清理临时文件。
    /// </summary>
    public static async Task<string> DownloadToTempAsync(
        string url,
        string? fileName = null,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(TempDownloadDir);

        fileName = SanitizeFileName(string.IsNullOrEmpty(fileName)
            ? Path.GetFileName(new Uri(url).LocalPath) is { Length: > 0 } name ? name : "download"
            : fileName);
        var temp = Path.Combine(TempDownloadDir, $"{Guid.NewGuid():N}_{fileName}");
        LogService.Debug($"下载开始(临时目录): {url} -> {temp}");

        try
        {
            using var response = await Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var total = response.Content.Headers.ContentLength;

            await using var source = await response.Content.ReadAsStreamAsync(ct);
            await using var target = File.Create(temp);

            var buffer = new byte[81920];
            long written = 0;
            int read;

            while ((read = await source.ReadAsync(buffer, ct)) > 0)
            {
                await target.WriteAsync(buffer.AsMemory(0, read), ct);
                written += read;
                progress?.Report(total is > 0 ? (double)written / total.Value : 0);
            }

            progress?.Report(1.0);
            LogService.Info($"下载完成: {fileName} ({total?.ToString() ?? written.ToString()} 字节) 已写入临时目录");
            return temp;
        }
        catch (Exception ex)
        {
            try
            {
                if (File.Exists(temp))
                {
                    File.Delete(temp);
                }
            }
            catch
            {
                // 清理失败忽略
            }

            LogService.Error($"下载失败: {url} - {ex.Message}");
            throw;
        }
    }

    /// <summary>清理临时下载目录中的残留文件（应用启动时调用）。</summary>
    public static void CleanupTempDownloads()
    {
        try
        {
            if (!Directory.Exists(TempDownloadDir))
            {
                return;
            }

            foreach (var file in Directory.EnumerateFiles(TempDownloadDir))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // 忽略单个文件清理失败
                }
            }
        }
        catch
        {
            // 清理失败不阻断应用
        }
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        return name;
    }
}
