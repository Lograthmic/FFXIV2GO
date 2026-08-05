using System.IO;
using System.IO.Compression;
using FFXIV2GO.Services;

namespace FFXIV2GO.Tests;

public class ArchiveServiceTests
{
    private static string TempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ffxiv2go-arc-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void Extract7z_ExtractsZipArchive()
    {
        var root = TempDir();
        try
        {
            var archivePath = Path.Combine(root, "test.zip");
            var dest = Path.Combine(root, "out");

            using (var zip = ZipFile.Open(archivePath, ZipArchiveMode.Create))
            {
                var entry = zip.CreateEntry("sub/hello.txt");
                using var writer = new StreamWriter(entry.Open());
                writer.Write("hello");
            }

            ArchiveService.Extract7z(archivePath, dest);

            Assert.True(File.Exists(Path.Combine(dest, "sub", "hello.txt")));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Extract7z_MissingArchive_Throws()
    {
        var root = TempDir();
        try
        {
            var missing = Path.Combine(root, "nope.7z");
            Assert.ThrowsAny<Exception>(() => ArchiveService.Extract7z(missing, Path.Combine(root, "out")));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
