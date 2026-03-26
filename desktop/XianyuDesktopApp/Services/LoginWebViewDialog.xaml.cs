using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using XianyuDesktopApp.Models;

namespace XianyuDesktopApp.Services;

public sealed partial class LoginWebViewDialog : ContentDialog
{
    private readonly AccountProfile _profile;
    private LoginResult _result = new();

    public LoginWebViewDialog(AccountProfile profile)
    {
        InitializeComponent();
        _profile = profile;
    }

    public async Task<LoginResult> ShowAndCaptureAsync(XamlRoot xamlRoot)
    {
        XamlRoot = xamlRoot;
        await EnsureWebViewAsync();
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
        LoginWebView.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted;
        LoginWebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
    }

    private async void CoreWebView2_NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (!args.IsSuccess)
        {
            return;
        }

        try
        {
            var cookies = await sender.CookieManager.GetCookiesAsync("https://www.goofish.com/");
            if (cookies.Count == 0)
            {
                cookies = await sender.CookieManager.GetCookiesAsync("https://2.taobao.com/");
            }

            var cookieString = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
            if (cookieString.Contains("cna=") && cookieString.Contains("unb="))
            {
                StatusTextBlock.Text = "已捕获 Cookie，正在关闭窗口...";
                _result = new LoginResult
                {
                    Success = true,
                    CookieString = cookieString,
                };
                Hide();
            }
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Cookie 检测失败：{ex.Message}";
        }
    }
}
