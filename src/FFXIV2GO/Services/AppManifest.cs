namespace FFXIV2GO.Services;

public enum AppType
{
    Archive,
    Portable,
    Installer
}

/// <summary>应用清单根。</summary>
public sealed class AppManifest
{
    public List<string> AtLeastOne { get; set; } = [];
    public List<AppEntry> Apps { get; set; } = [];
}

/// <summary>单个可下载应用。</summary>
public sealed class AppEntry
{
    public string Name { get; set; } = string.Empty;
    public string? Group { get; set; }
    public string FileName { get; set; } = string.Empty;
    public AppType Type { get; set; } = AppType.Portable;
    /// <summary>apps 目录下的目标文件夹名。</summary>
    public string Target { get; set; } = string.Empty;
    /// <summary>下载文件存放位置："apps"（默认）或 "inst"。</summary>
    public string DownloadFolder { get; set; } = "apps";
    /// <summary>下载后提示用户手动运行/解压（如自解压安装包）。</summary>
    public bool PromptExtract { get; set; }
    /// <summary>固定下载地址（与 GithubLatest 二选一）。</summary>
    public string? Url { get; set; }
    public GithubLatestRef? GithubLatest { get; set; }
}

/// <summary>从 GitHub 最新 Release 解析下载地址。</summary>
public sealed class GithubLatestRef
{
    public string Repo { get; set; } = string.Empty;
    /// <summary>资产名正则。</summary>
    public string AssetPattern { get; set; } = string.Empty;
}
