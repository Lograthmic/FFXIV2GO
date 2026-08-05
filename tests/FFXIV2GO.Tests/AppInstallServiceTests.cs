using System.IO;
using FFXIV2GO.Services;

namespace FFXIV2GO.Tests;

public class AppInstallServiceTests
{
    private static string TempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ffxiv2go-apps-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        return root;
    }

    private static AppEntry Entry(string target) => new()
    {
        Name = "Test",
        Target = target,
        Type = AppType.Portable
    };

    [Fact]
    public void IsInstalled_WithFiles_ReturnsTrue()
    {
        var root = TempRoot();
        try
        {
            var dir = Path.Combine(root, "App");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "app.exe"), "");

            Assert.True(AppInstallService.IsInstalled(Entry("App"), root));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void IsInstalled_EmptyFolder_ReturnsFalse()
    {
        var root = TempRoot();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "App"));

            Assert.False(AppInstallService.IsInstalled(Entry("App"), root));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void IsInstalled_MissingFolder_ReturnsFalse()
    {
        var root = TempRoot();
        try
        {
            Assert.False(AppInstallService.IsInstalled(Entry("App"), root));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Uninstall_DeletesTargetFolder()
    {
        var root = TempRoot();
        try
        {
            var dir = Path.Combine(root, "App");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "app.exe"), "");

            AppInstallService.Uninstall(Entry("App"), root);

            Assert.False(Directory.Exists(dir));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void IsRunning_NoMatchingProcess_ReturnsFalse()
    {
        var root = TempRoot();
        try
        {
            var dir = Path.Combine(root, "App");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "definitely-not-running-app.exe"), "");

            Assert.False(AppInstallService.IsRunning(Entry("App"), root));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Uninstall_LockedFile_ThrowsFriendlyMessage()
    {
        var root = TempRoot();
        try
        {
            var dir = Path.Combine(root, "App");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, "app.exe");
            File.WriteAllText(file, "");

            using (var lockStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                var ex = Assert.Throws<IOException>(() => AppInstallService.Uninstall(Entry("App"), root));
                Assert.Contains("Failed to delete", ex.Message);
            }

            Assert.True(Directory.Exists(dir));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
