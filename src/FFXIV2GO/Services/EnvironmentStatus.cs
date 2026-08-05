using System.IO;

namespace FFXIV2GO.Services;

/// <summary>
/// 环境状态：判断部署根是否已完成初始化。
/// </summary>
public static class EnvironmentStatus
{
    /// <summary>inst 或 conf 目录下有任意文件即视为已初始化。</summary>
    public static bool IsInitialized(string? root = null)
    {
        root ??= DeploymentRoot.Path;
        return HasAnyFile(Path.Combine(root, "inst")) || HasAnyFile(Path.Combine(root, "conf"));
    }

    /// <summary>清空 conf/inst/apps 三个目录（按“重新初始化以新环境安装”语义，全部清空）。</summary>
    public static void Reset(string? root = null)
    {
        root ??= DeploymentRoot.Path;
        foreach (var dir in new[] { "conf", "inst", "apps" })
        {
            var path = Path.Combine(root, dir);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            Directory.CreateDirectory(path);
        }
    }

    private static bool HasAnyFile(string dir)
    {
        if (!Directory.Exists(dir))
        {
            return false;
        }

        return Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories).Any();
    }
}
