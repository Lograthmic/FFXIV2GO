using System.IO;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace FFXIV2GO.Services;

/// <summary>
/// 压缩包解压（SharpCompress，原生支持 7z/zip）。
/// - 7z（多为 solid）：用 ExtractAllEntries 顺序流式解压，避免逐条 WriteToDirectory 反复定位导致的极慢；
/// - 其他（zip 等）：逐条解压。带详细日志与进度。
/// </summary>
public static class ArchiveService
{
    public static void Extract7z(string archivePath, string destinationDir, IProgress<double>? progress = null)
    {
        Directory.CreateDirectory(destinationDir);

        var fileSize = new FileInfo(archivePath).Length;
        LogService.Info($"开始解压: {archivePath} ({fileSize} 字节) -> {destinationDir}");

        using var archive = ArchiveFactory.OpenArchive(archivePath, new ReaderOptions());
        var total = archive.Entries.Count();
        LogService.Info($"压缩包格式: {archive.Type}, 总条目: {total}");

        var options = new ExtractionOptions
        {
            ExtractFullPath = true,
            Overwrite = true
        };

        int processed = 0;
        int fileCount = 0;

        void ProcessEntry(string key, long size, Action write)
        {
            processed++;
            try
            {
                LogService.Debug($"解压条目: {key} ({size} 字节)");
                write();
                fileCount++;
            }
            catch (Exception ex)
            {
                LogService.Error($"解压条目失败: {key} - {ex}");
                throw;
            }

            progress?.Report(total > 0 ? (double)processed / total : 0);
        }

        if (archive.Type == ArchiveType.SevenZip)
        {
            using var reader = archive.ExtractAllEntries();
            while (reader.MoveToNextEntry())
            {
                if (reader.Entry.IsDirectory)
                {
                    processed++;
                    continue;
                }

                var key = reader.Entry.Key ?? string.Empty;
                var size = reader.Entry.Size;
                ProcessEntry(key, size, () => reader.WriteEntryToDirectory(destinationDir, options));
            }
        }
        else
        {
            foreach (var entry in archive.Entries)
            {
                if (entry.IsDirectory)
                {
                    processed++;
                    continue;
                }

                var key = entry.Key ?? string.Empty;
                var size = entry.Size;
                ProcessEntry(key, size, () => entry.WriteToDirectory(destinationDir, options));
            }
        }

        LogService.Info($"解压完成: 共解压 {fileCount} 个文件, {archivePath} -> {destinationDir}");
    }
}
