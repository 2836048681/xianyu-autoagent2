namespace XianyuDesktopApp.Models;

public sealed class LoginResult
{
    public bool Success { get; init; }

    public string CookieString { get; init; } = string.Empty;

    public string ErrorMessage { get; init; } = string.Empty;
}
