using System.IO;
using System.Text.RegularExpressions;

namespace FFXIV2GO.Services;

/// <summary>
/// 桌面快捷方式创建/删除：WScript.Shell COM 生成 .lnk。
/// </summary>
public static class ShortcutService
{
    public static string SelfShortcutName => "FFXIV2GO";
    public static string FolderShortcutName => LocalizationService.Instance["Setup.FolderShortcut"];

    public static bool CreateLink(string linkPath, string targetPath, string? workingDirectory = null)
    {
        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null)
            {
                return false;
            }

            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic link = shell.CreateShortcut(linkPath);
            link.TargetPath = targetPath;
            if (!string.IsNullOrEmpty(workingDirectory))
            {
                link.WorkingDirectory = workingDirectory;
            }

            link.Save();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool DeleteLink(string linkPath)
    {
        try
        {
            if (File.Exists(linkPath))
            {
                File.Delete(linkPath);
                return true;
            }
        }
        catch
        {
            // 忽略删除失败
        }

        return false;
    }

    public static string SanitizeName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        return name.Trim();
    }

    /// <summary>
    /// 查找目录中的主可执行文件。
    /// 优先级：非安装器/非更新器 → 文件名与首选名（如应用名）匹配 → 顶层优先。
    /// 例如 XIVLauncherCN 目录中更新器 update.exe 会被排除，优先 XIVLauncherCN.exe。
    /// </summary>
    public static string? FindMainExecutable(string dir, string? preferredName = null)
    {
        if (!Directory.Exists(dir))
        {
            return null;
        }

        var token = NormalizeToken(preferredName);

        var top = Directory.EnumerateFiles(dir, "*.exe", SearchOption.TopDirectoryOnly)
            .OrderBy(p => Rank(p, token))
            .ThenBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (top.Count > 0)
        {
            return top[0];
        }

        var sub = Directory.EnumerateFiles(dir, "*.exe", SearchOption.AllDirectories)
            .OrderBy(p => Rank(p, token))
            .ThenBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return sub.Count > 0 ? sub[0] : null;
    }

    /// <summary>分值越低越优先：排除安装器/更新器，名字匹配首选名时降权。</summary>
    private static int Rank(string path, string? token)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        if (IsUpdaterLike(name))
        {
            return 100;
        }

        int score = IsInstallerLike(name) ? 50 : 0;
        if (ContainsToken(name, token))
        {
            score -= 10;
        }

        return score;
    }

    private static bool IsInstallerLike(string name) =>
        Regex.IsMatch(name, @"(installer|setup|install)", RegexOptions.IgnoreCase);

    private static bool IsUpdaterLike(string name) =>
        Regex.IsMatch(name, @"^(update|updater)$", RegexOptions.IgnoreCase);

    /// <summary>提取首选名的核心标识（去括号后缀、去非字母数字、小写），如 "XIVLauncherCN (Soil)" → "xivlaunchercn"。</summary>
    private static string? NormalizeToken(string? preferredName)
    {
        if (string.IsNullOrWhiteSpace(preferredName))
        {
            return null;
        }

        var baseName = preferredName;
        var paren = preferredName.IndexOf('(');
        if (paren >= 0)
        {
            baseName = preferredName[..paren];
        }

        return Regex.Replace(baseName, "[^a-zA-Z0-9]", "").ToLowerInvariant();
    }

    private static bool ContainsToken(string exeName, string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        var normalized = Regex.Replace(exeName, "[^a-zA-Z0-9]", "").ToLowerInvariant();
        return normalized.Contains(token);
    }

    /// <summary>为清单中已安装到 apps 的应用创建桌面快捷方式，返回创建数量。</summary>
    public static int CreateAppShortcuts(AppManifest manifest, string desktop, Action<string>? log = null)
    {
        int created = 0;

        foreach (var app in manifest.Apps)
        {
            var targetDir = Path.Combine(DeploymentRoot.Apps, app.Target);
            var exe = FindMainExecutable(targetDir, app.Name);
            if (exe is null)
            {
                log?.Invoke(string.Format(LocalizationService.Instance["Setup.ShortcutSkipped"], app.Target));
                continue;
            }

            var link = Path.Combine(desktop, SanitizeName(app.Name) + ".lnk");
            if (CreateLink(link, exe, Path.GetDirectoryName(exe)))
            {
                created++;
                log?.Invoke(string.Format(LocalizationService.Instance["Setup.ShortcutCreated"], app.Name));
            }
        }

        return created;
    }

    public static bool CreateSelfShortcut(string desktop)
    {
        var exe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exe))
        {
            return false;
        }

        return CreateLink(Path.Combine(desktop, SelfShortcutName + ".lnk"), exe, DeploymentRoot.Path);
    }

    public static bool CreateFolderShortcut(string desktop, string linkName)
    {
        return CreateLink(Path.Combine(desktop, SanitizeName(linkName) + ".lnk"),
            DeploymentRoot.Path, DeploymentRoot.Path);
    }

    /// <summary>删除安装阶段创建的桌面快捷方式，返回删除数量。</summary>
    public static int RemoveCreatedShortcuts(string desktop, AppManifest manifest)
    {
        int removed = 0;

        foreach (var app in manifest.Apps)
        {
            if (DeleteLink(Path.Combine(desktop, SanitizeName(app.Name) + ".lnk")))
            {
                removed++;
            }
        }

        if (DeleteLink(Path.Combine(desktop, SelfShortcutName + ".lnk")))
        {
            removed++;
        }

        // 文件夹快捷方式可能以任一语言创建，尝试删除两种名称
        if (DeleteLink(Path.Combine(desktop, SanitizeName("FFXIV2GO 目录") + ".lnk")))
        {
            removed++;
        }

        if (DeleteLink(Path.Combine(desktop, SanitizeName("FFXIV2GO Folder") + ".lnk")))
        {
            removed++;
        }

        return removed;
    }
}
