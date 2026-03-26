using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using XianyuDesktopApp.Models;

namespace XianyuDesktopApp.Services;

public sealed class LoginCookieProvider : ILoginCookieProvider
{
    private readonly string _baseDirectory;

    public LoginCookieProvider(string baseDirectory)
    {
        _baseDirectory = baseDirectory;
    }

    public async Task<LoginResult> LoginEmbeddedAsync(AccountProfile profile, XamlRoot xamlRoot)
    {
        var dialog = new LoginWebViewDialog(profile);
        return await dialog.ShowAndCaptureAsync(xamlRoot);
    }

    public async Task<LoginResult> LoginFallbackAsync(AccountProfile profile)
    {
        var workerExe = ResolveWorkerExecutable();
        var startInfo = new ProcessStartInfo
        {
            FileName = workerExe.FileName,
            Arguments = workerExe.Arguments(profile),
            WorkingDirectory = workerExe.WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8,
        };
        startInfo.Environment["NON_INTERACTIVE"] = "1";
        startInfo.Environment["PYTHONIOENCODING"] = "utf-8";
        startInfo.Environment["PYTHONUTF8"] = "1";
        var process = Process.Start(startInfo) ?? throw new InvalidOperationException("无法启动兜底登录进程。");

        string? resultLine = null;
        while (!process.StandardOutput.EndOfStream)
        {
            var line = await process.StandardOutput.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }
            resultLine = line;
        }

        await process.WaitForExitAsync();
        if (string.IsNullOrWhiteSpace(resultLine))
        {
            return new LoginResult
            {
                Success = false,
                ErrorMessage = await process.StandardError.ReadToEndAsync()
            };
        }

        using var doc = JsonDocument.Parse(resultLine);
        var root = doc.RootElement;
        return new LoginResult
        {
            Success = root.TryGetProperty("ok", out var ok) && ok.GetBoolean(),
            CookieString = root.TryGetProperty("cookie", out var cookie) ? cookie.GetString() ?? string.Empty : string.Empty,
            ErrorMessage = root.TryGetProperty("error", out var error) ? error.GetString() ?? string.Empty : string.Empty,
        };
    }

    private (string FileName, Func<AccountProfile, string> Arguments, string WorkingDirectory) ResolveWorkerExecutable()
    {
        var publishedExe = Path.Combine(_baseDirectory, "worker", "XianyuWorker.exe");
        if (File.Exists(publishedExe))
        {
            return (
                publishedExe,
                profile => $"login --account-dir \"{profile.AccountDir}\" --account-name \"{profile.Name}\"",
                Path.GetDirectoryName(publishedExe) ?? _baseDirectory
            );
        }

        var repoRoot = ResolveRepoRoot(_baseDirectory);
        var pythonExe = "python";
        var scriptPath = Path.Combine(repoRoot, "worker_cli.py");
        return (
            pythonExe,
            profile => $"\"{scriptPath}\" login --account-dir \"{profile.AccountDir}\" --account-name \"{profile.Name}\"",
            repoRoot
        );
    }

    private static string ResolveRepoRoot(string baseDirectory)
    {
        var directory = new DirectoryInfo(baseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "worker_cli.py")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        return baseDirectory;
    }
}
