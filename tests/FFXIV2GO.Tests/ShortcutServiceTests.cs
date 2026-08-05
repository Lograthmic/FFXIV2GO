using System.IO;
using FFXIV2GO.Services;

namespace FFXIV2GO.Tests;

public class ShortcutServiceTests
{
    private static string TempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ffxiv2go-lnk-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void FindMainExecutable_PrefersRealExeOverInstaller()
    {
        var dir = TempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "ACT_Installer.exe"), "");
            File.WriteAllText(Path.Combine(dir, "CafeACT.exe"), "");

            var exe = ShortcutService.FindMainExecutable(dir);
            Assert.Equal("CafeACT.exe", Path.GetFileName(exe));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void FindMainExecutable_FallsBackToInstallerOnly()
    {
        var dir = TempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "setup.exe"), "");

            var exe = ShortcutService.FindMainExecutable(dir);
            Assert.Equal("setup.exe", Path.GetFileName(exe));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void FindMainExecutable_NestedDirectory()
    {
        var dir = TempDir();
        try
        {
            var sub = Path.Combine(dir, "app");
            Directory.CreateDirectory(sub);
            File.WriteAllText(Path.Combine(sub, "main.exe"), "");

            var exe = ShortcutService.FindMainExecutable(dir);
            Assert.Equal("main.exe", Path.GetFileName(exe));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void FindMainExecutable_None_ReturnsNull()
    {
        var dir = TempDir();
        try
        {
            Assert.Null(ShortcutService.FindMainExecutable(dir));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void CreateLink_WritesLnkFile()
    {
        var dir = TempDir();
        try
        {
            var link = Path.Combine(dir, "Test.lnk");
            var ok = ShortcutService.CreateLink(link, @"C:\Windows\System32\notepad.exe", dir);

            Assert.True(ok);
            Assert.True(File.Exists(link));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void FindMainExecutable_PrefersNameMatchedExeOverUpdater()
    {
        var dir = TempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "update.exe"), "");
            File.WriteAllText(Path.Combine(dir, "XIVLauncher.exe"), "");
            File.WriteAllText(Path.Combine(dir, "XIVLauncherCN.exe"), "");

            var exe = ShortcutService.FindMainExecutable(dir, "XIVLauncherCN");
            Assert.Equal("XIVLauncherCN.exe", Path.GetFileName(exe));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void FindMainExecutable_PreferredNameWithSuffixMatches()
    {
        var dir = TempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "clash-party.exe"), "");

            var exe = ShortcutService.FindMainExecutable(dir, "Clash Party");
            Assert.Equal("clash-party.exe", Path.GetFileName(exe));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }

    [Theory]
    [InlineData("XIVLauncherCN (Soil):", "XIVLauncherCN (Soil)_")]
    [InlineData("FFXIV2GO", "FFXIV2GO")]
    [InlineData("Clash Party", "Clash Party")]
    public void SanitizeName_RemovesInvalidChars(string input, string expected)
        => Assert.Equal(expected, ShortcutService.SanitizeName(input));
}
