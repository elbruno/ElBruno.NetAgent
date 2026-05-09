namespace ElBruno.NetAgent.Infrastructure.Windows;

/// <summary>
/// Abstraction for detecting Windows administrator elevation.
/// Does not request elevation — only detects current state.
/// </summary>
public interface IWindowsAdminService
{
    /// <summary>
    /// Returns true if the current process is running with administrator privileges.
    /// </summary>
    bool IsAdministrator { get; }
}
