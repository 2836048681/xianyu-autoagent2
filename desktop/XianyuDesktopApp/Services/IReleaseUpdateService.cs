using System.Threading.Tasks;
using XianyuDesktopApp.Models;

namespace XianyuDesktopApp.Services;

public interface IReleaseUpdateService
{
    Task<UpdateInfo> CheckForUpdatesAsync();
}
