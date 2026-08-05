using System.IO;

namespace FFXIV2GO.Services;

/// <summary>
/// 桌面路径定位（兼容 Desktop / OneDrive\Desktop / OneDrive\桌面）。
/// </summary>
public static class DesktopHelper
{
    public static string GetDesktopPath()
    {
        var user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        foreach (var candidate in new[]
                 {
                     Path.Combine(user, "Desktop"),
                     Path.Combine(user, "OneDrive", "Desktop"),
                     Path.Combine(user, "OneDrive", "桌面")
                 })
        {
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        return Path.Combine(user, "Desktop");
    }

    public static string CachesFolder => Path.Combine(GetDesktopPath(), "caches");
}
