namespace XianyuDesktopApp.Services;

public sealed class ServiceContainer
{
    public ServiceContainer(string baseDirectory)
    {
        AccountStore = new AccountStore(baseDirectory);
        WorkerHost = new WorkerHost(baseDirectory);
        LoginCookieProvider = new LoginCookieProvider(baseDirectory);
        ReleaseUpdateService = new ReleaseUpdateService("2836048681", "xianyu-autoagent2");
    }

    public IAccountStore AccountStore { get; }

    public IWorkerHost WorkerHost { get; }

    public ILoginCookieProvider LoginCookieProvider { get; }

    public IReleaseUpdateService ReleaseUpdateService { get; }
}
