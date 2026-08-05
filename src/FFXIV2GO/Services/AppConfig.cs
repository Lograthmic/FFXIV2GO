using System.Globalization;
using System.IO;
using System.Text;

namespace FFXIV2GO.Services;

/// <summary>
/// config.ini 读写：FFXIV 安装路径、语言、主题。
/// 位于部署根目录，与 exe 同目录。
/// </summary>
public sealed class AppConfig
{
    public string FfxivPath { get; set; } = string.Empty;
    public string Language { get; set; } = "System";
    public string Theme { get; set; } = "System";
    public string LogLevel { get; set; } = "Info";

    public static AppConfig Load() => LoadFrom(DeploymentRoot.ConfigFile);

    public static AppConfig LoadFrom(string file)
    {
        var config = new AppConfig();
        if (!File.Exists(file)) return config;

        foreach (var rawLine in File.ReadAllLines(file, Encoding.UTF8))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var eq = line.IndexOf('=');
            if (eq <= 0) continue;

            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].Trim();

            switch (key)
            {
                case "PATH_FFXIV":
                    config.FfxivPath = value;
                    break;
                case "Language":
                    config.Language = value;
                    break;
                case "Theme":
                    config.Theme = value;
                    break;
                case "LogLevel":
                    config.LogLevel = value;
                    break;
            }
        }

        return config;
    }

    public void Save() => SaveTo(DeploymentRoot.ConfigFile);

    public void SaveTo(string file)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"PATH_FFXIV={FfxivPath}");
        sb.AppendLine($"Language={Language}");
        sb.AppendLine($"Theme={Theme}");
        sb.AppendLine($"LogLevel={LogLevel}");
        File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
    }

    public static string[] Languages { get; } = ["System", "zh-CN", "en"];
    public static string[] Themes { get; } = ["System", "Light", "Dark"];
    public static string[] LogLevels { get; } = ["Debug", "Info", "Warn", "Error"];

    public static string NormalizeLanguage(string? value) =>
        value is not null && Languages.Contains(value) ? value : "System";

    public static string NormalizeTheme(string? value) =>
        value is not null && Themes.Contains(value) ? value : "System";

    public static string NormalizeLogLevel(string? value) =>
        value is not null && LogLevels.Contains(value) ? value : "Info";

    public static string ResolveCulture(string language) => language switch
    {
        "zh-CN" => "zh-CN",
        "en" => "en",
        _ => CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
            ? "zh-CN"
            : "en"
    };
}
