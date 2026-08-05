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
}
