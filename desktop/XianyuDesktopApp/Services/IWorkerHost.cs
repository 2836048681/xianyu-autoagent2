using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using XianyuDesktopApp.Models;

namespace XianyuDesktopApp.Services;

public interface IWorkerHost
{
    event EventHandler<WorkerLogEvent>? LogReceived;

    event EventHandler<(string AccountName, bool IsRunning)>? WorkerStateChanged;

    Task StartAsync(AccountProfile profile);

    Task StopAsync(string accountName);

    Task StopAllAsync();

    bool IsRunning(string accountName);

    IReadOnlyCollection<string> RunningAccounts { get; }
}
