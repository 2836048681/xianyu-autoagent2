using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using XianyuDesktopApp.Models;

namespace XianyuDesktopApp.Services;

public interface ILoginCookieProvider
{
    Task<LoginResult> LoginEmbeddedAsync(AccountProfile profile, XamlRoot xamlRoot);

    Task<LoginResult> LoginFallbackAsync(AccountProfile profile);
}
