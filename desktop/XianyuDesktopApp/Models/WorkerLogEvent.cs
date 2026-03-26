using System;

namespace XianyuDesktopApp.Models;

public sealed class WorkerLogEvent
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    public string AccountName { get; init; } = string.Empty;

    public string Level { get; init; } = "INFO";

    public string Message { get; init; } = string.Empty;

    public string RawText { get; init; } = string.Empty;
}
