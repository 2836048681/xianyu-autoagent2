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
    private string selectedAccountTitle = "Account workspace";

    [ObservableProperty]
    private string selectedAccountSummary = "Select an account";

    [ObservableProperty]
    private string runtimeStatusText = "Idle";

    [ObservableProperty]
    private string logCaptionText = "Runtime logs for the selected account appear here";

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
        SelectedAccountTitle = $"Account workspace · {selected.Name}";
        SelectedAccountSummary = $"{selected.Name} | {selected.AccountDir}";
        RuntimeStatusText = selected.IsRunning ? $"Account {selected.Name} is running" : $"Account {selected.Name} is stopped";
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

        if (string.IsNullOrWhiteSpace(EditableAccount.ApiKey))
        {
            await ShowInfoAsync(xamlRoot, "Cannot start", "API_KEY is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(EditableAccount.CookieString))
        {
            await ShowInfoAsync(xamlRoot, "Cannot start", "Cookies are required. Complete login first.");
            return;
        }

        try
        {
            RuntimeStatusText = $"Starting account {account.Name} ...";
            await SaveSelectedAccountAsync();
            await _workerHost.StartAsync(account);

            await Task.Delay(1500);
            if (!_workerHost.IsRunning(account.Name))
            {
                LoadLogsForSelectedAccount();
                await ShowInfoAsync(
                    xamlRoot,
                    "Start failed",
                    string.IsNullOrWhiteSpace(LogText)
                        ? "The worker process exited immediately. Check account config, API key, cookies, and logs."
                        : $"The worker process exited immediately. Recent logs:\n\n{TrimText(LogText)}");
            }
        }
        catch (Exception ex)
        {
            RuntimeStatusText = $"Account {account.Name} failed to start";
            await ShowInfoAsync(xamlRoot, "Start failed", ex.Message);
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
            ? $"New version available: {info.LatestVersion}\nPublished: {info.PublishedAt:yyyy-MM-dd HH:mm}\n\n{TrimText(info.ReleaseNotes)}"
            : $"You are already on the latest version: {info.CurrentVersion}";

        var dialog = new ContentDialog
        {
            Title = "Check updates",
            Content = new TextBlock
            {
                Text = content,
                TextWrapping = TextWrapping.WrapWholeWords,
                MaxWidth = 560
            },
            PrimaryButtonText = info.HasUpdate && !string.IsNullOrWhiteSpace(info.DownloadUrl) ? "Open download page" : "OK",
            CloseButtonText = "Close",
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
                ? $"Account {state.AccountName} is running"
                : $"Account {state.AccountName} is stopped";
        }
    }

    private async Task HandleLoginResultAsync(AccountProfile account, LoginResult result, bool wasRunning, XamlRoot? xamlRoot)
    {
        if (!result.Success)
        {
            if (xamlRoot is not null)
            {
                await ShowInfoAsync(
                    xamlRoot,
                    "Login failed",
                    string.IsNullOrWhiteSpace(result.ErrorMessage) ? "Automatic cookie capture failed." : result.ErrorMessage);
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
        LogCaptionText = $"Current account: {account.Name} | {logPath}";
        if (!System.IO.File.Exists(logPath))
        {
            LogText = string.Empty;
            return;
        }

        _logBuilder.Append(System.IO.File.ReadAllText(logPath));
        LogText = _logBuilder.ToString();
    }

    private static string TrimText(string text)
    {
        const int maxLength = 480;
        if (string.IsNullOrWhiteSpace(text))
        {
            return "No details available.";
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
            CloseButtonText = "Close",
            XamlRoot = xamlRoot
        };

        await dialog.ShowAsync();
    }
}
