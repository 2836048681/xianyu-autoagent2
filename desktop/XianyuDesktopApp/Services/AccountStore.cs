using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using XianyuDesktopApp.Models;

namespace XianyuDesktopApp.Services;

public sealed class AccountStore : IAccountStore
{
    private readonly string _appDataRoot;
    private readonly string _bundledPromptsDir;

    public AccountStore(string baseDirectory)
    {
        _appDataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "XianyuAutoAgent");
        _bundledPromptsDir = Path.Combine(ResolveRepoRoot(baseDirectory), "prompts");
        Directory.CreateDirectory(_appDataRoot);
        Directory.CreateDirectory(GetAccountsRoot());
    }

    public async Task<IReadOnlyList<AccountProfile>> ListAccountsAsync()
    {
        var root = GetAccountsRoot();
        Directory.CreateDirectory(root);
        var accounts = Directory.GetDirectories(root)
            .Select(LoadProfile)
            .OrderBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (accounts.Count == 0)
        {
            accounts.Add(await CreateAccountAsync("account1"));
        }

        return accounts;
    }

    public Task<AccountProfile> CreateAccountAsync(string accountName)
    {
        var normalized = NormalizeAccountName(accountName);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Account name cannot be empty.");
        }

        var accountDir = EnsureAccountDirectories(normalized);
        EnsurePromptsCopied(accountDir);
        return Task.FromResult(LoadProfile(accountDir));
    }

    public Task SaveAccountAsync(AccountProfile profile)
    {
        var envPath = Path.Combine(profile.AccountDir, ".env");
        var map = ReadEnv(envPath);
        map["API_KEY"] = profile.ApiKey;
        map["MODEL_BASE_URL"] = profile.ModelBaseUrl;
        map["MODEL_NAME"] = profile.ModelName;
        map["COOKIES_STR"] = profile.CookieString;
        WriteEnv(envPath, map);
        EnsurePromptsCopied(profile.AccountDir);
        return Task.CompletedTask;
    }

    public void OpenPromptsFolder(AccountProfile profile) => OpenFolder(profile.PromptsDir);

    public void OpenAccountFolder(AccountProfile profile) => OpenFolder(profile.AccountDir);

    public void OpenLogsFolder(AccountProfile profile) => OpenFolder(profile.LogsDir);

    private string GetAccountsRoot() => Path.Combine(_appDataRoot, "accounts");

    private string EnsureAccountDirectories(string accountName)
    {
        var accountDir = Path.Combine(GetAccountsRoot(), accountName);
        Directory.CreateDirectory(accountDir);
        Directory.CreateDirectory(Path.Combine(accountDir, "data"));
        Directory.CreateDirectory(Path.Combine(accountDir, "prompts"));
        Directory.CreateDirectory(Path.Combine(accountDir, "logs"));
        Directory.CreateDirectory(Path.Combine(accountDir, "webview2"));
        return accountDir;
    }

    private AccountProfile LoadProfile(string accountDir)
    {
        var envPath = Path.Combine(accountDir, ".env");
        EnsurePromptsCopied(accountDir);
        var env = ReadEnv(envPath);
        return new AccountProfile
        {
            Name = Path.GetFileName(accountDir),
            AccountDir = accountDir,
            PromptsDir = Path.Combine(accountDir, "prompts"),
            LogsDir = Path.Combine(accountDir, "logs"),
            ApiKey = env.TryGetValue("API_KEY", out var apiKey) ? apiKey : string.Empty,
            ModelBaseUrl = env.TryGetValue("MODEL_BASE_URL", out var baseUrl) && !string.IsNullOrWhiteSpace(baseUrl)
                ? baseUrl
                : "https://dashscope.aliyuncs.com/compatible-mode/v1",
            ModelName = env.TryGetValue("MODEL_NAME", out var modelName) && !string.IsNullOrWhiteSpace(modelName)
                ? modelName
                : "qwen-max",
            CookieString = env.TryGetValue("COOKIES_STR", out var cookie) ? cookie : string.Empty,
        };
    }

    private void EnsurePromptsCopied(string accountDir)
    {
        var promptsDir = Path.Combine(accountDir, "prompts");
        Directory.CreateDirectory(promptsDir);
        if (!Directory.Exists(_bundledPromptsDir))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(_bundledPromptsDir))
        {
            var destination = Path.Combine(promptsDir, Path.GetFileName(file));
            if (!File.Exists(destination))
            {
                File.Copy(file, destination, overwrite: false);
            }
        }
    }

    private static string ResolveRepoRoot(string baseDirectory)
    {
        var directory = new DirectoryInfo(baseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "prompts")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        return baseDirectory;
    }

    private static string NormalizeAccountName(string accountName)
    {
        var chars = accountName.Trim()
            .Select(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' ? ch : '_')
            .ToArray();
        return new string(chars);
    }

    private static Dictionary<string, string> ReadEnv(string envPath)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(envPath))
        {
            return result;
        }

        foreach (var rawLine in File.ReadAllLines(envPath))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || !line.Contains('='))
            {
                continue;
            }

            var parts = line.Split('=', 2);
            result[parts[0].Trim()] = parts[1].Trim();
        }

        return result;
    }

    private static void WriteEnv(string envPath, Dictionary<string, string> values)
    {
        var lines = values.Select(pair => $"{pair.Key}={pair.Value}");
        File.WriteAllLines(envPath, lines);
    }

    private static void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
}
