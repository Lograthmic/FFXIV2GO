using System.Collections.Concurrent;
using System.IO;

namespace FFXIV2GO.Services;

/// <summary>
/// 磁盘文件操作：清理 log/old 文件、空间统计、大小换算。
/// 扫描范围仅限部署根目录，避免误删网盘同盘符下的其他文件。
/// </summary>
public static class DiskService
{
    private static readonly string[] CleanupExtensions = [".log", ".old"];

    public static bool IsCleanableFile(string path) =>
        CleanupExtensions.Contains(System.IO.Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    /// <summary>扫描部署根下所有可清理文件，返回 (文件数, 总字节数)。</summary>
    public static (int Count, long Size) ScanCleanableFiles(string root)
    {
        int count = 0;
        long size = 0;

        foreach (var dir in EnumerateDirectoriesSafe(root))
        {
            foreach (var file in EnumerateFilesSafe(dir))
            {
                if (!IsCleanableFile(file)) continue;
                count++;
                size += new FileInfo(file).Length;
            }
        }

        return (count, size);
    }

    /// <summary>
    /// 删除可清理文件，逐文件回调进度；返回实际释放字节数。
    /// </summary>
    public static async Task<long> DeleteCleanableFilesAsync(
        string root,
        IProgress<(string File, long Freed)>? progress,
        CancellationToken ct)
    {
        long freed = 0;

        foreach (var dir in EnumerateDirectoriesSafe(root))
        {
            foreach (var file in EnumerateFilesSafe(dir))
            {
                ct.ThrowIfCancellationRequested();
                if (!IsCleanableFile(file)) continue;

                long size;
                try
                {
                    size = new FileInfo(file).Length;
                    File.Delete(file);
                    freed += size;
                    progress?.Report((file, size));
                }
                catch
                {
                    // 文件可能被占用或正被删除，跳过
                }

                await Task.Yield();
            }
        }

        return freed;
    }

    public static string ConvertSize(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F2} GB",
        >= 1_048_576 => $"{bytes / 1_048_576.0:F2} MB",
        >= 1024 => $"{bytes / 1024.0:F2} KB",
        _ => $"{bytes} B"
    };

    private static IEnumerable<string> EnumerateDirectoriesSafe(string root)
    {
        var queue = new ConcurrentQueue<string>();
        queue.Enqueue(root);
        var visited = new ConcurrentDictionary<string, byte>();

        while (queue.TryDequeue(out var dir))
        {
            if (!visited.TryAdd(dir, 0)) continue;

            IEnumerable<string> subDirs;
            try
            {
                var info = new DirectoryInfo(dir);
                // 跳过目录联接，防止跟随进入 conf/apps 挂载目标
                if ((info.Attributes & FileAttributes.ReparsePoint) != 0 && dir != root)
                    continue;
                subDirs = Directory.EnumerateDirectories(dir);
            }
            catch
            {
                continue;
            }

            foreach (var sub in subDirs)
                queue.Enqueue(sub);

            yield return dir;
        }
    }

    private static IEnumerable<string> EnumerateFilesSafe(string dir)
    {
        try
        {
            return Directory.EnumerateFiles(dir);
        }
        catch
        {
            return [];
        }
    }
}
