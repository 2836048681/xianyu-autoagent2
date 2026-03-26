using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics;
using WinRT.Interop;
using XianyuDesktopApp.Models;
using XianyuDesktopApp.Services;
using XianyuDesktopApp.ViewModels;

namespace XianyuDesktopApp;

public sealed partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainViewModel ViewModel => _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        var baseDir = AppContext.BaseDirectory;
        var services = new ServiceContainer(baseDir);
        _viewModel = new MainViewModel(services);

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        ConfigureWindow();
        Activated += MainWindow_Activated;
        Closed += MainWindow_Closed;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await _viewModel.InitializeAsync();
        if (_viewModel.Accounts.Any())
        {
            AccountsListView.SelectedIndex = 0;
        }
    }

    private void ConfigureWindow()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new SizeInt32(1520, 980));
        appWindow.Title = "闲鱼自动化助手";
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
    }

    private async void AddAccountButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "新增账号",
            Content = new TextBox
            {
                PlaceholderText = "例如：account2",
                MinWidth = 320
            },
            PrimaryButtonText = "创建",
            CloseButtonText = "取消",
            XamlRoot = Content.XamlRoot
        };
        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        if (dialog.Content is TextBox textBox)
        {
            await _viewModel.AddAccountAsync(textBox.Text);
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e) => await _viewModel.SaveSelectedAccountAsync();

    private async void StartAccountButton_Click(object sender, RoutedEventArgs e) => await _viewModel.StartSelectedAccountAsync();

    private async void StopAccountButton_Click(object sender, RoutedEventArgs e) => await _viewModel.StopSelectedAccountAsync();

    private async void StopAllButton_Click(object sender, RoutedEventArgs e) => await _viewModel.StopAllAsync();

    private async void EmbeddedLoginButton_Click(object sender, RoutedEventArgs e) => await _viewModel.RunEmbeddedLoginAsync(Content.XamlRoot);

    private async void FallbackLoginButton_Click(object sender, RoutedEventArgs e) => await _viewModel.RunFallbackLoginAsync();

    private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e) => await _viewModel.CheckForUpdatesAsync(Content.XamlRoot);

    private void OpenPromptsButton_Click(object sender, RoutedEventArgs e) => _viewModel.OpenPromptsFolder();

    private void OpenAccountFolderButton_Click(object sender, RoutedEventArgs e) => _viewModel.OpenAccountFolder();

    private void OpenLogsButton_Click(object sender, RoutedEventArgs e) => _viewModel.OpenLogsFolder();

    private void CopyLogsButton_Click(object sender, RoutedEventArgs e) => _viewModel.CopyLogsToClipboard();

    private void ClearLogsButton_Click(object sender, RoutedEventArgs e) => _viewModel.ClearLogView();

    private void AccountsListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is AccountProfile account)
        {
            _viewModel.SelectAccount(account.Name);
        }
    }

    private async void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        await _viewModel.StopAllAsync();
    }
}
