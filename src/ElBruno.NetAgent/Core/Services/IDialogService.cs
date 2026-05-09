namespace ElBruno.NetAgent.Core.Services
{
    /// <summary>
    /// Abstraction for opening logs/config without touching System.Diagnostics in tests.
    /// </summary>
    public interface IDialogService
    {
        void OpenLogs();
        void OpenConfig();
    }
}
