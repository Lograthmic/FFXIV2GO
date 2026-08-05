namespace FFXIV2GO.Services;

/// <summary>
/// 部署根目录 = exe 所在目录（任意文件夹，不要求盘符根目录）。
/// 所有安装包、配置、软件的路径均以部署根为基准。
/// </summary>
public static class DeploymentRoot
{
    public static string Path { get; } =
        System.IO.Path.GetDirectoryName(Environment.ProcessPath ?? AppContext.BaseDirectory)
        ?? AppContext.BaseDirectory;

    public static string Conf => System.IO.Path.Combine(Path, "conf");
    public static string Inst => System.IO.Path.Combine(Path, "inst");
    public static string Apps => System.IO.Path.Combine(Path, "apps");

    public static string GameConfig => System.IO.Path.Combine(Conf, "FINAL FANTASY XIV - A Realm Reborn");
    public static string LauncherConfig => System.IO.Path.Combine(Conf, "XIVLauncherCN");
    public static string Mods => System.IO.Path.Combine(Conf, "mods");

    public static string PenumbraJson => System.IO.Path.Combine(LauncherConfig, "pluginConfigs", "Penumbra.json");

    public static string ConfigFile => System.IO.Path.Combine(Path, "config.ini");

    public static string VcRedist => System.IO.Path.Combine(Inst, "VC_redist.x64.exe");
    public static string DotNetRuntime => System.IO.Path.Combine(Inst, "windowsdesktop-runtime-10.0-win-x64.exe");

    public static string GameConfigFolderName => "FINAL FANTASY XIV - A Realm Reborn";
    public static string GameConfigOldName => "FINAL FANTASY XIV - A Realm Reborn.old";
}
