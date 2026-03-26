using CommunityToolkit.Mvvm.ComponentModel;

namespace XianyuDesktopApp.Models;

public partial class AccountProfile : ObservableObject
{
    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string apiKey = string.Empty;

    [ObservableProperty]
    private string modelBaseUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1";

    [ObservableProperty]
    private string modelName = "qwen-max";

    [ObservableProperty]
    private string cookieString = string.Empty;

    [ObservableProperty]
    private string accountDir = string.Empty;

    [ObservableProperty]
    private string promptsDir = string.Empty;

    [ObservableProperty]
    private string logsDir = string.Empty;

    [ObservableProperty]
    private bool isRunning;

    public string StatusText => IsRunning ? "Running" : "Stopped";

    public string CookieStatusText => string.IsNullOrWhiteSpace(CookieString) ? "Cookie missing" : "Cookie ready";

    partial void OnIsRunningChanged(bool value) => OnPropertyChanged(nameof(StatusText));

    partial void OnCookieStringChanged(string value) => OnPropertyChanged(nameof(CookieStatusText));

    public AccountProfile Clone() =>
        new()
        {
            Name = Name,
            ApiKey = ApiKey,
            ModelBaseUrl = ModelBaseUrl,
            ModelName = ModelName,
            CookieString = CookieString,
            AccountDir = AccountDir,
            PromptsDir = PromptsDir,
            LogsDir = LogsDir,
            IsRunning = IsRunning
        };
}
