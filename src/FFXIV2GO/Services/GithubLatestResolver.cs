using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace FFXIV2GO.Services;

/// <summary>
/// 解析 GitHub 最新 Release 中匹配的资产下载地址。
/// </summary>
public static class GithubLatestResolver
{
    private static readonly HttpClient Client = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = true,
        AutomaticDecompression = System.Net.DecompressionMethods.All
    })
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public static string BuildApiUrl(string repo) =>
        $"https://api.github.com/repos/{repo}/releases/latest";

    /// <summary>按资产名正则匹配，返回首个匹配资产的 browser_download_url；无匹配返回 null。</summary>
    public static async Task<string?> ResolveAsync(string repo, string assetPattern, CancellationToken ct = default)
    {
        LogService.Debug($"解析最新 Release: {repo}，规则: {assetPattern}");

        using var request = new HttpRequestMessage(HttpMethod.Get, BuildApiUrl(repo));
        request.Headers.TryAddWithoutValidation("Accept", "application/vnd.github+json");
        request.Headers.TryAddWithoutValidation("User-Agent", "FFXIV2GO");

        using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var url = MatchAsset(json, assetPattern);
        LogService.Debug($"解析结果: {repo} -> {(url ?? "未匹配")}");
        return url;
    }

    public static string? MatchAsset(string releaseJson, string assetPattern)
    {
        var node = JsonNode.Parse(releaseJson);
        if (node?["assets"] is not JsonArray assets)
        {
            return null;
        }

        var regex = new Regex(assetPattern, RegexOptions.IgnoreCase);
        foreach (var asset in assets)
        {
            var name = asset?["name"]?.GetValue<string>();
            var url = asset?["browser_download_url"]?.GetValue<string>();
            if (name is not null && url is not null && regex.IsMatch(name))
            {
                return url;
            }
        }

        return null;
    }

    /// <summary>获取仓库最新版本的版本号（整理后的数字版本，如 1.1.0）；无法确定返回 null。</summary>
    /// <remarks>
    /// 拉取 releases 列表并优先解析带版本号的 tag（v*）；存量无版本 tag（如历史 “latest”）
    /// 则回退到 name/body 中提取版本号。列表实现同时兼容两种形态，保持稳定。
    /// </remarks>
    public static async Task<string?> GetLatestVersionAsync(string repo, CancellationToken ct = default)
    {
        LogService.Debug($"获取最新 Release 版本: {repo}");

        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{repo}/releases?per_page=10");
        request.Headers.TryAddWithoutValidation("Accept", "application/vnd.github+json");
        request.Headers.TryAddWithoutValidation("User-Agent", "FFXIV2GO");

        using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var version = ResolveLatestVersion(json);
        LogService.Debug($"最新版本: {version ?? "未知"}");
        return version;
    }

    /// <summary>从 releases 列表 JSON（新→旧）中解析最新版本号；跳过 draft/prerelease。</summary>
    public static string? ResolveLatestVersion(string releasesJson)
    {
        if (JsonNode.Parse(releasesJson) is not JsonArray releases)
        {
            return null;
        }

        var regex = new Regex(@"\d+\.\d+(?:\.\d+){0,2}", RegexOptions.IgnoreCase);
        foreach (var release in releases)
        {
            if (release is null ||
                release["draft"]?.GetValue<bool>() == true ||
                release["prerelease"]?.GetValue<bool>() == true)
            {
                continue;
            }

            var version = CleanVersion(release["tag_name"]?.GetValue<string>());
            if (version is not null)
            {
                return version;
            }

            foreach (var field in new[] { "name", "body" })
            {
                var text = release[field]?.GetValue<string>();
                if (text is null)
                {
                    continue;
                }

                var match = regex.Match(text);
                if (match.Success)
                {
                    return match.Value;
                }
            }
        }

        return null;
    }

    /// <summary>把 Release 标签整理成版本号：去掉前导 v，截取数字与点部分（忽略预发布后缀）。</summary>
    public static string? CleanVersion(string? tag)
    {
        if (string.IsNullOrEmpty(tag))
        {
            return null;
        }

        var s = tag.TrimStart('v', 'V');
        int end = 0;
        while (end < s.Length && (char.IsDigit(s[end]) || s[end] == '.'))
        {
            end++;
        }

        return end == 0 ? null : s[..end];
    }
}
