using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using XianyuDesktopApp.Models;

namespace XianyuDesktopApp.Services;

public sealed partial class LoginWebViewDialog : ContentDialog
{
    private readonly AccountProfile _profile;
    private readonly CancellationTokenSource _pollingTokenSource = new();
    private LoginResult _result = new();

    public LoginWebViewDialog(AccountProfile profile)
    {
        InitializeComponent();
        _profile = profile;
        Closing += LoginWebViewDialog_Closing;
    }

    public async Task<LoginResult> ShowAndCaptureAsync(XamlRoot xamlRoot)
    {
        XamlRoot = xamlRoot;
        await EnsureWebViewAsync();
        LoginWebView.Source = new Uri("https://www.goofish.com/");
        _ = PollCookiesLoopAsync(_pollingTokenSource.Token);
        await ShowAsync();
        return _result;
    }

    private async Task EnsureWebViewAsync()
    {
        var env = await CoreWebView2Environment.CreateWithOptionsAsync(
            browserExecutableFolder: null,
            userDataFolder: Path.Combine(_profile.AccountDir, "webview2"),
            options: null);

        await LoginWebView.EnsureCoreWebView2Async(env);
        LoginWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        LoginWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
        LoginWebView.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted;
        LoginWebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
    }

    private void CoreWebView2_NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (!args.IsSuccess)
        {
            StatusTextBlock.Text = $"页面加载失败：{args.WebErrorStatus}";
            return;
        }

        var uri = sender.Source ?? string.Empty;
        StatusTextBlock.Text = uri.Contains("login", StringComparison.OrdinalIgnoreCase)
            ? "请完成登录，系统会自动检测 Cookie。"
            : "页面已加载，正在持续检测 Cookie...";
    }

    private async Task PollCookiesLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var cookieString = await TryExtractCookieStringAsync();
                if (!string.IsNullOrWhiteSpace(cookieString))
                {
                    StatusTextBlock.Text = "已检测到有效 Cookie，正在保存...";
                    _result = new LoginResult
                    {
                        Success = true,
                        CookieString = cookieString,
                    };
                    Hide();
                    return;
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Cookie 检测失败：{ex.Message}";
            }

            await Task.Delay(1500, cancellationToken);
        }
    }

    private async Task<string> TryExtractCookieStringAsync()
    {
        if (LoginWebView.CoreWebView2 is null)
        {
            return string.Empty;
        }

        var origins = new[]
        {
            "https://www.goofish.com/",
            "https://2.taobao.com/",
            "https://login.taobao.com/",
            "https://h5.m.goofish.com/"
        };

        foreach (var origin in origins)
        {
            var cookies = await LoginWebView.CoreWebView2.CookieManager.GetCookiesAsync(origin);
            if (cookies.Count == 0)
            {
                continue;
            }

            var cookieString = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
            if (cookieString.Contains("cna=") && (cookieString.Contains("unb=") || cookieString.Contains("cookie2=")))
            {
                return cookieString;
            }
        }

        return string.Empty;
    }

    private void LoginWebViewDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        _pollingTokenSource.Cancel();
    }
}
