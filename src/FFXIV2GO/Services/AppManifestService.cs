using System.IO;
using System.Text.Json;

namespace FFXIV2GO.Services;

/// <summary>
/// 应用清单（apps.json）加载：优先读取 exe 旁的侧置文件，缺失则从内嵌默认值生成并写出。
/// 开发人员可直接编辑侧置文件来增删应用。
/// </summary>
public static class AppManifestService
{
    public const string FileName = "apps.json";
    private const string ResourceName = "FFXIV2GO.Resources.apps.json";

    public static string FilePath => Path.Combine(DeploymentRoot.Path, FileName);

    public static AppManifest Load()
    {
        string json;
        if (File.Exists(FilePath))
        {
            json = File.ReadAllText(FilePath);
        }
        else
        {
            json = LoadEmbeddedDefault();
            try
            {
                File.WriteAllText(FilePath, json);
            }
            catch
            {
                // 无法写入时仍使用默认内容
            }
        }

        return Deserialize(json);
    }

    public static AppManifest Deserialize(string json) =>
        JsonSerializer.Deserialize<AppManifest>(json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(
                    System.Text.Json.JsonNamingPolicy.CamelCase) }
            })
        ?? new AppManifest();

    private static string LoadEmbeddedDefault()
    {
        var assembly = typeof(AppManifestService).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"内嵌资源缺失: {ResourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
