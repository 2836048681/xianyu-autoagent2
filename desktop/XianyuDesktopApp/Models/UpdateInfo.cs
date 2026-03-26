using System;

namespace XianyuDesktopApp.Models;

public sealed class UpdateInfo
{
    public string CurrentVersion { get; init; } = string.Empty;

    public string LatestVersion { get; init; } = string.Empty;

    public DateTimeOffset? PublishedAt { get; init; }

    public string ReleaseNotes { get; init; } = string.Empty;

    public string DownloadUrl { get; init; } = string.Empty;

    public bool HasUpdate =>
        !string.IsNullOrWhiteSpace(LatestVersion) &&
        !string.Equals(CurrentVersion, LatestVersion, StringComparison.OrdinalIgnoreCase);
}
