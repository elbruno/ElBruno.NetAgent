using ElBruno.NetAgent.Core.Services;

namespace ElBruno.NetAgent.Services
{
    /// <summary>
    /// Safe no-op dialog service for non-UI hosts and tests.
    /// Does not open external viewers or mutate system state.
    /// </summary>
    public class NullDialogService : IDialogService
    {
        public void OpenLogs()
        {
            // no-op: safe default for test/non-UI environments
        }

        public void OpenConfig()
        {
            // no-op: safe default for test/non-UI environments
        }
    }
}
