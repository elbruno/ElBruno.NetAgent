using System.Threading;
using System.Threading.Tasks;

namespace ElBruno.NetAgent.Core.Configuration
{
    public interface IConfigurationService
    {
        Task<NetAgentOptions> GetOptionsAsync(CancellationToken cancellationToken = default);
        Task<NetAgentOptions> ReloadAsync(CancellationToken cancellationToken = default);
        Task SaveOptionsAsync(NetAgentOptions options, CancellationToken cancellationToken = default);

        // Helper methods for UI/testing
        string GetConfigFolderPath();
        string GetConfigFilePath();

        void OpenConfigFolder();
        void OpenConfigFile();
    }
}
