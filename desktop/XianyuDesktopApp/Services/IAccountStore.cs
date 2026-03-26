using System.Collections.Generic;
using System.Threading.Tasks;
using XianyuDesktopApp.Models;

namespace XianyuDesktopApp.Services;

public interface IAccountStore
{
    Task<IReadOnlyList<AccountProfile>> ListAccountsAsync();

    Task<AccountProfile> CreateAccountAsync(string accountName);

    Task SaveAccountAsync(AccountProfile profile);

    void OpenPromptsFolder(AccountProfile profile);

    void OpenAccountFolder(AccountProfile profile);

    void OpenLogsFolder(AccountProfile profile);
}
