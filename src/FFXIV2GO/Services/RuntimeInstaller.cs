using System.Diagnostics;

namespace FFXIV2GO.Services;

/// <summary>
/// 静默安装运行库（VC++ / .NET Desktop Runtime）。
/// </summary>
public static class RuntimeInstaller
{
    public static async Task<int> InstallAsync(string installerPath, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = installerPath,
            UseShellExecute = true
        };
        psi.ArgumentList.Add("/install");
        psi.ArgumentList.Add("/quiet");
        psi.ArgumentList.Add("/norestart");

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"无法启动安装程序: {installerPath}");

        await process.WaitForExitAsync(ct);
        return process.ExitCode;
    }
}
