using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FFXIV2GO.Services;

/// <summary>
/// Penumbra.json 读写：读取/修改 ModDirectory（mod 目录）。
/// </summary>
public static class PenumbraConfigService
{
    public static string? GetModDirectory(string jsonPath)
    {
        if (!File.Exists(jsonPath)) return null;

        var json = JsonNode.Parse(File.ReadAllText(jsonPath));
        return json?["ModDirectory"]?.GetValue<string>();
    }

    public static bool SetModDirectory(string jsonPath, string modDirectory)
    {
        if (!File.Exists(jsonPath)) return false;

        var json = JsonNode.Parse(File.ReadAllText(jsonPath));
        if (json is not JsonObject obj) return false;

        obj["ModDirectory"] = modDirectory;
        File.WriteAllText(jsonPath, obj.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        return true;
    }
}
