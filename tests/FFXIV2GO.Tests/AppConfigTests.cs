using System.IO;
using System.Text;
using FFXIV2GO.Services;

namespace FFXIV2GO.Tests;

public class AppConfigTests
{
    private static string TempFile() =>
        Path.Combine(Path.GetTempPath(), $"ffxiv2go-test-{Guid.NewGuid():N}.ini");

    [Fact]
    public void SaveTo_Then_LoadFrom_RoundTrips()
    {
        var file = TempFile();
        try
        {
            var config = new AppConfig
            {
                FfxivPath = @"D:\FFXIV",
                Language = "zh-CN",
                Theme = "Dark",
                LogLevel = "Debug",
                AskOnClose = false,
                CloseAction = "Exit",
                AutoLaunchApps = "XIVLauncherCN, Everything"
            };
            config.SaveTo(file);

            var loaded = AppConfig.LoadFrom(file);
            Assert.Equal(@"D:\FFXIV", loaded.FfxivPath);
            Assert.Equal("zh-CN", loaded.Language);
            Assert.Equal("Dark", loaded.Theme);
            Assert.Equal("Debug", loaded.LogLevel);
            Assert.False(loaded.AskOnClose);
            Assert.Equal("Exit", loaded.CloseAction);
            Assert.Equal("XIVLauncherCN, Everything", loaded.AutoLaunchApps);
        }
        finally
        {
            if (File.Exists(file)) File.Delete(file);
        }
    }

    [Fact]
    public void LoadFrom_MissingFile_ReturnsDefaults()
    {
        var loaded = AppConfig.LoadFrom(TempFile());
        Assert.Equal(string.Empty, loaded.FfxivPath);
        Assert.Equal("System", loaded.Language);
        Assert.Equal("System", loaded.Theme);
    }

    [Theory]
    [InlineData(null, "System")]
    [InlineData("", "System")]
    [InlineData("zh-CN", "zh-CN")]
    [InlineData("en", "en")]
    [InlineData("fr", "System")]
    public void NormalizeLanguage_ReturnsValid(string? value, string expected)
        => Assert.Equal(expected, AppConfig.NormalizeLanguage(value));

    [Theory]
    [InlineData(null, "System")]
    [InlineData("", "System")]
    [InlineData("Light", "Light")]
    [InlineData("Dark", "Dark")]
    [InlineData("Blue", "System")]
    public void NormalizeTheme_ReturnsValid(string? value, string expected)
        => Assert.Equal(expected, AppConfig.NormalizeTheme(value));

    [Theory]
    [InlineData(null, "Info")]
    [InlineData("", "Info")]
    [InlineData("Debug", "Debug")]
    [InlineData("Info", "Info")]
    [InlineData("Warn", "Warn")]
    [InlineData("Error", "Error")]
    [InlineData("verbose", "Info")]
    public void NormalizeLogLevel_ReturnsValid(string? value, string expected)
        => Assert.Equal(expected, AppConfig.NormalizeLogLevel(value));

    [Fact]
    public void LoadFrom_MissingAskOnClose_DefaultsToTrue()
    {
        var file = Path.Combine(Path.GetTempPath(), $"ffxiv2go-test-{Guid.NewGuid():N}.ini");
        try
        {
            File.WriteAllText(file, "Language=en\n", Encoding.UTF8);
            var config = AppConfig.LoadFrom(file);
            Assert.True(config.AskOnClose);
            Assert.Equal("MinimizeToTray", config.CloseAction);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Theory]
    [InlineData("zh-CN", "zh-CN")]
    [InlineData("en", "en")]
    public void ResolveCulture_Explicit(string lang, string expected)
        => Assert.Equal(expected, AppConfig.ResolveCulture(lang));

    [Theory]
    [InlineData("XIVLauncherCN, Everything", new[] { "XIVLauncherCN", "Everything" })]
    [InlineData(" X , Y ,", new[] { "X", "Y" })]
    [InlineData("", new string[0])]
    [InlineData("   ", new string[0])]
    public void SplitAutoLaunchApps_SplitsAndTrims(string raw, string[] expected)
    {
        var config = new AppConfig { AutoLaunchApps = raw };
        Assert.Equal(expected, config.SplitAutoLaunchApps());
    }

    [Fact]
    public void LoadFrom_SkipsCommentsAndBlankLines()
    {
        var file = TempFile();
        try
        {
            File.WriteAllText(file,
                "# comment\r\n\r\nPATH_FFXIV=D:\\Games\\FFXIV\r\nLanguage=en\r\n");
            var loaded = AppConfig.LoadFrom(file);
            Assert.Equal(@"D:\Games\FFXIV", loaded.FfxivPath);
            Assert.Equal("en", loaded.Language);
        }
        finally
        {
            if (File.Exists(file)) File.Delete(file);
        }
    }
}
