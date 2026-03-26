using Microsoft.UI.Xaml;
using System;
using System.IO;

namespace XianyuDesktopApp;

public partial class App : Application
{
    private MainWindow? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            _window = new MainWindow();
            _window.Activate();
        }
        catch (Exception ex)
        {
            var appDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "XianyuAutoAgent");
            Directory.CreateDirectory(appDir);
            File.WriteAllText(Path.Combine(appDir, "desktop_startup_error.log"), ex.ToString());
            throw;
        }
    }
}
