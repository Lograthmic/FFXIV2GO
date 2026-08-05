using System.IO;
using FFXIV2GO.Services;

namespace FFXIV2GO.Tests;

public class DiskServiceTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(2048, "2.00 KB")]
    [InlineData(1048576, "1.00 MB")]
    [InlineData(1073741824, "1.00 GB")]
    public void ConvertSize_Formats(long bytes, string expected)
        => Assert.Equal(expected, DiskService.ConvertSize(bytes));

    [Theory]
    [InlineData(@"C:\a\b.log", true)]
    [InlineData(@"C:\a\b.old", true)]
    [InlineData(@"C:\a\b.OLD", true)]
    [InlineData(@"C:\a\b.txt", false)]
    [InlineData(@"C:\a\b.exe", false)]
    public void IsCleanableFile_MatchesExtensions(string path, bool expected)
        => Assert.Equal(expected, DiskService.IsCleanableFile(path));

    [Fact]
    public void ScanCleanableFiles_CountsLogAndOldOnly()
    {
        var root = CreateTempTree(out var log, out var old, out var txt);
        try
        {
            var (count, size) = DiskService.ScanCleanableFiles(root);
            Assert.Equal(2, count);
            Assert.True(size > 0);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task DeleteCleanableFilesAsync_DeletesAndReportsFreed()
    {
        var root = CreateTempTree(out var log, out var old, out var txt);
        try
        {
            long before = new FileInfo(log).Length + new FileInfo(old).Length;

            long freed = await DiskService.DeleteCleanableFilesAsync(root, null, CancellationToken.None);

            Assert.Equal(before, freed);
            Assert.False(File.Exists(log));
            Assert.False(File.Exists(old));
            Assert.True(File.Exists(txt));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static string CreateTempTree(out string log, out string old, out string txt)
    {
        var root = Path.Combine(Path.GetTempPath(), $"ffxiv2go-disk-{Guid.NewGuid():N}");
        var sub = Path.Combine(root, "sub");
        Directory.CreateDirectory(sub);

        log = Path.Combine(sub, "app.log");
        old = Path.Combine(sub, "backup.old");
        txt = Path.Combine(sub, "keep.txt");

        File.WriteAllText(log, "log-content-here");
        File.WriteAllText(old, "old-content-here");
        File.WriteAllText(txt, "txt-content-here");
        return root;
    }
}
