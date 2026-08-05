using System.IO;

namespace FFXIV2GO.Services;

/// <summary>
/// 文件系统工具：目录递归复制（等价 xcopy /E /I /H /Y）。
/// </summary>
public static class FileSystemService
{
    public static void CopyDirectory(string source, string destination, bool overwrite = true)
    {
        if (!Directory.Exists(source))
            throw new DirectoryNotFoundException($"源目录不存在: {source}");

        Directory.CreateDirectory(destination);

        foreach (var dir in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, dir));
            Directory.CreateDirectory(target);
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, file));
            File.Copy(file, target, overwrite);
        }
    }
}
