using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using XianyuDesktopApp.Models;

namespace XianyuDesktopApp.Services;

public sealed class ReleaseUpdateService : IReleaseUpdateService
{
    private readonly string _owner;
    private readonly string _repo;
    private readonly HttpClient _httpClient = new();

    public ReleaseUpdateService(string owner, string repo)
    {
        _owner = owner;
        _repo = repo;
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("XianyuDesktopApp", "1.0"));
    }

    public async Task<UpdateInfo> CheckForUpdatesAsync()
    {
        var currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
        var url = $"https://api.github.com/repos/{_owner}/{_repo}/releases/latest";
        using var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        string downloadUrl = string.Empty;
        if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
        {
            downloadUrl = assets.EnumerateArray()
                .Select(asset => asset.TryGetProperty("browser_download_url", out var item) ? item.GetString() ?? string.Empty : string.Empty)
                .FirstOrDefault(asset => asset.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                ?? string.Empty;
        }

        return new UpdateInfo
        {
            CurrentVersion = currentVersion,
            LatestVersion = root.TryGetProperty("tag_name", out var tag) ? tag.GetString() ?? string.Empty : string.Empty,
            PublishedAt = root.TryGetProperty("published_at", out var publishedAt) && DateTimeOffset.TryParse(publishedAt.GetString(), out var parsed)
                ? parsed
                : null,
            ReleaseNotes = root.TryGetProperty("body", out var body) ? body.GetString() ?? string.Empty : string.Empty,
            DownloadUrl = downloadUrl,
        };
    }
}
