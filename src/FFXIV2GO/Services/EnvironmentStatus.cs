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

    /// <summary>当前设备是否已安装：本机游戏配置目录已挂载为本部署根的目录联接。</summary>
    public static bool IsInstalled(string? ffxivPath = null)
    {
        var path = (ffxivPath ?? AppConfig.Load().FfxivPath).Trim();
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        var gameFolder = Path.Combine(path, "game", "My Games", DeploymentRoot.GameConfigFolderName);
        return JunctionService.IsJunction(gameFolder);
    }

    /// <summary>卸载状态指示：未初始化=红错号，已初始化未安装=绿对号，已安装=灰点点。</summary>
    public static StatusState UninstallState(string? root = null)
    {
        if (!IsInitialized(root))
        {
            return StatusState.Fail;
        }

        if (!IsInstalled(root))
        {
            return StatusState.Ok;
        }

        return StatusState.Neutral;
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
