using System.Security.Principal;

namespace ElBruno.NetAgent.Infrastructure.Windows;

/// <summary>
/// Detects whether the current process runs with administrator privileges.
/// Uses Windows principal — no elevation is requested.
/// </summary>
public class WindowsAdminService : IWindowsAdminService
{
    private readonly bool _isAdministrator;

    public bool IsAdministrator => _isAdministrator;

    public WindowsAdminService()
    {
        try
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            _isAdministrator = principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            // If detection fails, assume non-admin for safety
            _isAdministrator = false;
        }
    }
}
