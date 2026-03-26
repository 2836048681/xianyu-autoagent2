using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using XianyuDesktopApp.Models;
using XianyuDesktopApp.Services;

namespace XianyuDesktopApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IAccountStore _accountStore;
    private readonly IWorkerHost _workerHost;
    private readonly ILoginCookieProvider _loginCookieProvider;
    private readonly IReleaseUpdateService _releaseUpdateService;
    private readonly StringBuilder _logBuilder = new();

    [ObservableProperty]
    private AccountProfile editableAccount = new();

    [ObservableProperty]
    private string selectedAccountSummary = "请选择一个账号";

    [ObservableProperty]
    private string runtimeStatusText = "当前未运行";

    [ObservableProperty]
    private string logCaptionText = "此处展示当前账号日志";

    [ObservableProperty]
    private string logText = string.Empty;

    public ObservableCollection<AccountProfile> Accounts { get; } = [];

    public MainViewModel(ServiceContainer services)
    {
        _accountStore = services.AccountStore;
        _workerHost = services.WorkerHost;
        _loginCookieProvider = services.LoginCookieProvider;
        _releaseUpdateService = services.ReleaseUpdateService;

        _workerHost.LogReceived += WorkerHost_LogReceived;
        _workerHost.WorkerStateChanged += WorkerHost_WorkerStateChanged;
    }

    public async Task InitializeAsync()
    {
        Accounts.Clear();
        var accounts = await _accountStore.ListAccountsAsync();
        foreach (var account in accounts.Take(5))
        {
            account.IsRunning = _workerHost.IsRunning(account.Name);
            Accounts.Add(account);
        }

        if (Accounts.Count > 0)
        {
            SelectAccount(Accounts[0].Name);
        }
    }

    public void SelectAccount(string accountName)
    {
        var selected = Accounts.FirstOrDefault(account => account.Name == accountName);
        if (selected is null)
        {
            return;
        }

        EditableAccount = selected.Clone();
        SelectedAccountSummary = $"{selected.Name} | {selected.AccountDir}";
        RuntimeStatusText = selected.IsRunning ? $"账号 {selected.Name} 正在运行" : $"账号 {selected.Name} 已停止";
        LoadLogsForSelectedAccount();
    }

    public async Task AddAccountAsync(string name)
    {
        if (Accounts.Count >= 5)
        {
            return;
        }

        var profile = await _accountStore.CreateAccountAsync(name);
        Accounts.Add(profile);
        SelectAccount(profile.Name);
    }

    public async Task SaveSelectedAccountAsync()
    {
        var account = GetSelectedAccount();
        if (account is null)
        {
            return;
        }

        ApplyEditableAccount(account);
        await _accountStore.SaveAccountAsync(account);
        LoadLogsForSelectedAccount();
    }

    public async Task StartSelectedAccountAsync(XamlRoot xamlRoot)
    {
        var account = GetSelectedAccount();
        if (account is null)
        {
            return;
        }

        try
        {
            RuntimeStatusText = $"正在启动账号 {account.Name} ...";
            await SaveSelectedAccountAsync();
            await _workerHost.StartAsync(account);

            await Task.Delay(1200);
            if (!_workerHost.IsRunning(account.Name))
            {
                LoadLogsForSelectedAccount();
                await ShowInfoAsync(
                    xamlRoot,
                    "启动失败",
                    string.IsNullOrWhiteSpace(LogText)
                        ? "进程启动后立即退出，请检查账号配置、API_KEY、Cookie 和日志内容。"
                        : $"进程启动后立即退出。最近日志：\n\n{TrimReleaseNotes(LogText)}");
            }
        }
        catch (Exception ex)
        {
            RuntimeStatusText = $"账号 {account.Name} 启动失败";
            await ShowInfoAsync(xamlRoot, "启动失败", ex.Message);
        }
    }

    public async Task StopSelectedAccountAsync()
    {
        var account = GetSelectedAccount();
        if (account is null)
        {
            return;
        }

        await _workerHost.StopAsync(account.Name);
    }

    public Task StopAllAsync() => _workerHost.StopAllAsync();

    public async Task RunEmbeddedLoginAsync(XamlRoot xamlRoot)
    {
        var account = GetSelectedAccount();
        if (account is null)
        {
            return;
        }

        await SaveSelectedAccountAsync();
        var wasRunning = _workerHost.IsRunning(account.Name);
        var result = await _loginCookieProvider.LoginEmbeddedAsync(account, xamlRoot);
        await HandleLoginResultAsync(account, result, wasRunning, xamlRoot);
    }

    public async Task RunFallbackLoginAsync()
    {
        var account = GetSelectedAccount();
        if (account is null)
        {
            return;
        }

        await SaveSelectedAccountAsync();
        var wasRunning = _workerHost.IsRunning(account.Name);
        var result = await _loginCookieProvider.LoginFallbackAsync(account);
        await HandleLoginResultAsync(account, result, wasRunning, null);
    }

    public async Task CheckForUpdatesAsync(XamlRoot xamlRoot)
    {
        var info = await _releaseUpdateService.CheckForUpdatesAsync();
        var content = info.HasUpdate
            ? $"发现新版本：{info.LatestVersion}\n发布时间：{info.PublishedAt:yyyy-MM-dd HH:mm}\n\n{TrimReleaseNotes(info.ReleaseNotes)}"
            : $"当前已是最新版本：{info.CurrentVersion}";

        var dialog = new ContentDialog
        {
            Title = "检查更新",
            Content = new TextBlock { Text = content, TextWrapping = TextWrapping.WrapWholeWords, MaxWidth = 560 },
            PrimaryButtonText = info.HasUpdate && !string.IsNullOrWhiteSpace(info.DownloadUrl) ? "打开下载页面" : "确定",
            CloseButtonText = "关闭",
            XamlRoot = xamlRoot
        };
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && info.HasUpdate && !string.IsNullOrWhiteSpace(info.DownloadUrl))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = info.DownloadUrl,
                UseShellExecute = true
            });
        }
    }

    public void OpenPromptsFolder()
    {
        var account = GetSelectedAccount();
        if (account is not null)
        {
            _accountStore.OpenPromptsFolder(account);
        }
    }

    public void OpenAccountFolder()
    {
        var account = GetSelectedAccount();
        if (account is not null)
        {
            _accountStore.OpenAccountFolder(account);
        }
    }

    public void OpenLogsFolder()
    {
        var account = GetSelectedAccount();
        if (account is not null)
        {
            _accountStore.OpenLogsFolder(account);
        }
    }

    public void CopyLogsToClipboard()
    {
        var package = new DataPackage();
        package.SetText(LogText);
        Clipboard.SetContent(package);
    }

    public void ClearLogView()
    {
        LogText = string.Empty;
        _logBuilder.Clear();
    }

    private AccountProfile? GetSelectedAccount() =>
        Accounts.FirstOrDefault(account => account.Name == EditableAccount.Name);

    private void ApplyEditableAccount(AccountProfile account)
    {
        account.ApiKey = EditableAccount.ApiKey;
        account.ModelBaseUrl = EditableAccount.ModelBaseUrl;
        account.ModelName = EditableAccount.ModelName;
        account.CookieString = EditableAccount.CookieString;
    }

    private void WorkerHost_LogReceived(object? sender, WorkerLogEvent e)
    {
        var selected = GetSelectedAccount();
        if (selected is null || !string.Equals(selected.Name, e.AccountName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var line = $"[{e.Timestamp:yyyy-MM-dd HH:mm:ss}] [{e.Level}] {e.Message}";
        if (_logBuilder.Length > 0)
        {
            _logBuilder.AppendLine();
        }
        _logBuilder.Append(line);
        LogText = _logBuilder.ToString();
    }

    private void WorkerHost_WorkerStateChanged(object? sender, (string AccountName, bool IsRunning) state)
    {
        var account = Accounts.FirstOrDefault(item => item.Name == state.AccountName);
        if (account is null)
        {
            return;
        }

        account.IsRunning = state.IsRunning;
        if (EditableAccount.Name == state.AccountName)
        {
            EditableAccount.IsRunning = state.IsRunning;
            RuntimeStatusText = state.IsRunning
                ? $"账号 {state.AccountName} 正在运行"
                : $"账号 {state.AccountName} 已停止";
        }
    }

    private async Task HandleLoginResultAsync(AccountProfile account, LoginResult result, bool wasRunning, XamlRoot? xamlRoot)
    {
        if (!result.Success)
        {
            if (xamlRoot is not null)
            {
                await ShowInfoAsync(xamlRoot, "登录失败", string.IsNullOrWhiteSpace(result.ErrorMessage) ? "未能自动获取 Cookie。" : result.ErrorMessage);
            }
            return;
        }

        account.CookieString = result.CookieString;
        EditableAccount.CookieString = result.CookieString;
        await _accountStore.SaveAccountAsync(account);
        if (wasRunning)
        {
            await _workerHost.StartAsync(account);
        }
        LoadLogsForSelectedAccount();
    }

    private void LoadLogsForSelectedAccount()
    {
        _logBuilder.Clear();
        var account = GetSelectedAccount();
        if (account is null)
        {
            LogText = string.Empty;
            return;
        }

        var logPath = System.IO.Path.Combine(account.LogsDir, "current.log");
        LogCaptionText = $"当前账号：{account.Name} | {logPath}";
        if (!System.IO.File.Exists(logPath))
        {
            LogText = string.Empty;
            return;
        }

        _logBuilder.Append(System.IO.File.ReadAllText(logPath));
        LogText = _logBuilder.ToString();
    }

    private static string TrimReleaseNotes(string text)
    {
        const int maxLength = 480;
        if (string.IsNullOrWhiteSpace(text))
        {
            return "暂无可用内容。";
        }

        var normalized = text.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength] + "...";
    }

    private static async Task ShowInfoAsync(XamlRoot xamlRoot, string title, string content)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = new TextBlock
            {
                Text = content,
                TextWrapping = TextWrapping.WrapWholeWords,
                MaxWidth = 560
            },
            CloseButtonText = "关闭",
            XamlRoot = xamlRoot
        };

        await dialog.ShowAsync();
    }
}
