using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using XianyuDesktopApp.Models;

namespace XianyuDesktopApp.Services;

public sealed class WorkerHost : IWorkerHost
{
    private readonly string _baseDirectory;
    private readonly ConcurrentDictionary<string, Process> _processes = new(StringComparer.OrdinalIgnoreCase);

    public WorkerHost(string baseDirectory)
    {
        _baseDirectory = baseDirectory;
    }

    public event EventHandler<WorkerLogEvent>? LogReceived;

    public event EventHandler<(string AccountName, bool IsRunning)>? WorkerStateChanged;

    public IReadOnlyCollection<string> RunningAccounts => _processes.Keys.ToArray();

    public bool IsRunning(string accountName) => _processes.TryGetValue(accountName, out var process) && !process.HasExited;

    public async Task StartAsync(AccountProfile profile)
    {
        await StopAsync(profile.Name);
        var workerExe = ResolveWorkerExecutable();
        var startInfo = new ProcessStartInfo
        {
            FileName = workerExe.FileName,
            Arguments = workerExe.Arguments(profile),
            WorkingDirectory = workerExe.WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8,
        };
        startInfo.Environment["NON_INTERACTIVE"] = "1";
        startInfo.Environment["PYTHONIOENCODING"] = "utf-8";
        startInfo.Environment["PYTHONUTF8"] = "1";
        startInfo.Environment["PYTHONUNBUFFERED"] = "1";

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };
        process.Exited += (_, _) =>
        {
            _processes.TryRemove(profile.Name, out _);
            WorkerStateChanged?.Invoke(this, (profile.Name, false));
        };
        process.Start();
        _processes[profile.Name] = process;
        WorkerStateChanged?.Invoke(this, (profile.Name, true));
        _ = Task.Run(() => ReadOutputAsync(profile.Name, process.StandardOutput));
        _ = Task.Run(() => ReadPlainErrorAsync(profile.Name, process.StandardError));
    }

    public async Task StopAsync(string accountName)
    {
        if (!_processes.TryRemove(accountName, out var process))
        {
            return;
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }
        catch
        {
        }
        finally
        {
            WorkerStateChanged?.Invoke(this, (accountName, false));
            process.Dispose();
        }
    }

    public async Task StopAllAsync()
    {
        foreach (var accountName in _processes.Keys)
        {
            await StopAsync(accountName);
        }
    }

    private async Task ReadOutputAsync(string accountName, StreamReader reader)
    {
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                if (root.TryGetProperty("message", out _))
                {
                    LogReceived?.Invoke(this, new WorkerLogEvent
                    {
                        AccountName = accountName,
                        Level = root.TryGetProperty("level", out var level) ? level.GetString() ?? "INFO" : "INFO",
                        Message = root.GetProperty("message").GetString() ?? line,
                        RawText = line,
                        Timestamp = DateTimeOffset.TryParse(root.TryGetProperty("time", out var time) ? time.GetString() : null, out var parsed)
                            ? parsed
                            : DateTimeOffset.Now,
                    });
                    continue;
                }
            }
            catch
            {
            }

            LogReceived?.Invoke(this, new WorkerLogEvent
            {
                AccountName = accountName,
                Level = "INFO",
                Message = line,
                RawText = line,
            });
        }
    }

    private async Task ReadPlainErrorAsync(string accountName, StreamReader reader)
    {
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            LogReceived?.Invoke(this, new WorkerLogEvent
            {
                AccountName = accountName,
                Level = "ERROR",
                Message = line,
                RawText = line,
            });
        }
    }

    private (string FileName, Func<AccountProfile, string> Arguments, string WorkingDirectory) ResolveWorkerExecutable()
    {
        var publishedExe = Path.Combine(_baseDirectory, "worker", "XianyuWorker.exe");
        if (File.Exists(publishedExe))
        {
            return (
                publishedExe,
                profile => $"service --account-dir \"{profile.AccountDir}\" --account-name \"{profile.Name}\"",
                Path.GetDirectoryName(publishedExe) ?? _baseDirectory
            );
        }

        var repoRoot = ResolveRepoRoot(_baseDirectory);
        var pythonExe = "python";
        var scriptPath = Path.Combine(repoRoot, "worker_cli.py");
        return (
            pythonExe,
            profile => $"\"{scriptPath}\" service --account-dir \"{profile.AccountDir}\" --account-name \"{profile.Name}\"",
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
