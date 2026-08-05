using System.IO;
using FFXIV2GO.Services;

namespace FFXIV2GO.Tests;

public class AppManifestTests
{
    [Fact]
    public void Deserialize_LoadsAllKnownApps()
    {
        const string json = """
        {
          "atLeastOne": ["XIVLauncherCN"],
          "apps": [
            { "name": "XIVLauncherCN", "group": "XIVLauncherCN", "fileName": "XIVLauncherCN.7z", "type": "archive", "target": "XIVLauncherCN", "url": "https://example.com/a.7z" },
            { "name": "Soil", "group": "XIVLauncherCN", "fileName": "s.zip", "type": "archive", "target": "XIVLauncherCN-Soil", "githubLatest": { "repo": "AtmoOmen/FFXIVQuickLauncher", "assetPattern": "Portable\\.zip$" } },
            { "name": "ACT", "fileName": "act.exe", "type": "installer", "target": "ACT", "url": "https://example.com/act" },
            { "name": "Snipaste", "fileName": "s.zip", "type": "portable", "target": "Snipaste", "url": "https://example.com/s" }
          ]
        }
        """;

        var manifest = AppManifestService.Deserialize(json);

        Assert.Equal(new[] { "XIVLauncherCN" }, manifest.AtLeastOne);
        Assert.Equal(4, manifest.Apps.Count);
        Assert.Equal("XIVLauncherCN", manifest.Apps[0].Name);
        Assert.Equal(AppType.Archive, manifest.Apps[0].Type);
        Assert.Equal(AppType.Installer, manifest.Apps[2].Type);
        Assert.Equal(AppType.Portable, manifest.Apps[3].Type);
        Assert.Equal("AtmoOmen/FFXIVQuickLauncher", manifest.Apps[1].GithubLatest?.Repo);
        Assert.Equal(@"Portable\.zip$", manifest.Apps[1].GithubLatest?.AssetPattern);
        Assert.Null(manifest.Apps[0].GithubLatest);
        Assert.Null(manifest.Apps[2].Group);
    }

    [Fact]
    public void EmbeddedDefault_ContainsKnownApps()
    {
        var assembly = typeof(AppManifestService).Assembly;
        var resourceNames = assembly.GetManifestResourceNames();
        Assert.Contains("FFXIV2GO.Resources.apps.json", resourceNames);

        using var stream = assembly.GetManifestResourceStream("FFXIV2GO.Resources.apps.json");
        using var reader = new StreamReader(stream!);
        var manifest = AppManifestService.Deserialize(reader.ReadToEnd());

        Assert.Equal("XIVLauncherCN", Assert.Single(manifest.AtLeastOne));
        Assert.Contains(manifest.Apps, a => a.Name == "XIVLauncherCN");
        Assert.Contains(manifest.Apps, a => a.Name == "XIVLauncherCN (Soil)");
        Assert.Contains(manifest.Apps, a => a.Name == "Clash Party");
        Assert.Contains(manifest.Apps, a => a.Name == "Everything");

        var act = Assert.Single(manifest.Apps, a => a.Name == "FFCafe ACT");
        Assert.Equal(AppType.Installer, act.Type);
        Assert.True(act.PromptExtract);
        Assert.Equal("inst", act.DownloadFolder);
    }

    [Fact]
    public void Deserialize_EmptyJson_ReturnsDefaults()
    {
        var manifest = AppManifestService.Deserialize("{}");
        Assert.Empty(manifest.Apps);
        Assert.Empty(manifest.AtLeastOne);
    }
}

public class GithubLatestResolverTests
{
    [Theory]
    [InlineData("clash-party-windows-2.0.0-x64-portable.7z", "clash-party-windows-.*-x64-portable\\.7z$", true)]
    [InlineData("clash-party-windows-2.0.0-x64-setup.exe", "clash-party-windows-.*-x64-portable\\.7z$", false)]
    [InlineData("XIVLauncherCN-win-Portable.zip", "XIVLauncherCN-win-Portable\\.zip$", true)]
    [InlineData("XIVLauncherCN-win-Portable.7z", "XIVLauncherCN-win-Portable\\.zip$", false)]
    public void MatchAsset_MatchesByPattern(string name, string pattern, bool expected)
    {
        var json = $$"""
        {
          "assets": [
            { "name": "{{name}}", "browser_download_url": "https://github.com/example/releases/download/v1/{{name}}" }
          ]
        }
        """;

        var url = GithubLatestResolver.MatchAsset(json, pattern);
        Assert.Equal(expected, url is not null);
        if (expected)
        {
            Assert.EndsWith(name, url!);
        }
    }

    [Fact]
    public void MatchAsset_NoAssets_ReturnsNull()
    {
        Assert.Null(GithubLatestResolver.MatchAsset("{}", ".*"));
    }
}

public class EnvironmentStatusTests
{
    private static string TempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ffxiv2go-env-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        return root;
    }

    [Fact]
    public void IsInitialized_EmptyDirs_ReturnsFalse()
    {
        var root = TempRoot();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "conf"));
            Directory.CreateDirectory(Path.Combine(root, "inst"));
            Assert.False(EnvironmentStatus.IsInitialized(root));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void IsInitialized_ConfHasFile_ReturnsTrue()
    {
        var root = TempRoot();
        try
        {
            var conf = Path.Combine(root, "conf");
            Directory.CreateDirectory(conf);
            File.WriteAllText(Path.Combine(conf, "config.json"), "{}");
            Assert.True(EnvironmentStatus.IsInitialized(root));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void IsInitialized_InstHasFile_ReturnsTrue()
    {
        var root = TempRoot();
        try
        {
            var inst = Path.Combine(root, "inst");
            Directory.CreateDirectory(inst);
            File.WriteAllText(Path.Combine(inst, "dep.exe"), "");
            Assert.True(EnvironmentStatus.IsInitialized(root));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Reset_ClearsAllThreeDirs()
    {
        var root = TempRoot();
        try
        {
            foreach (var dir in new[] { "conf", "inst", "apps" })
            {
                var path = Path.Combine(root, dir);
                Directory.CreateDirectory(path);
                File.WriteAllText(Path.Combine(path, "file.txt"), "x");
            }

            EnvironmentStatus.Reset(root);

            foreach (var dir in new[] { "conf", "inst", "apps" })
            {
                var path = Path.Combine(root, dir);
                Assert.True(Directory.Exists(path));
                Assert.Empty(Directory.EnumerateFiles(path));
            }
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
