using System.Threading;
using System.Threading.Tasks;

namespace ElBruno.NetAgent.Core.Services
{
    /// <summary>
    /// Abstraction for performing safe network control operations.
    /// Implementations should honor DryRunMode at higher layers; this interface focuses on action semantics.
    /// </summary>
    public interface INetworkController
    {
        /// <summary>
        /// Request that the system prefer the supplied interface id. Returns a short result string for logging.
        /// </summary>
        Task<string> PreferInterfaceAsync(string interfaceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Restore automatic metrics (undo manual metric changes).
        /// </summary>
        Task<string> RestoreAutomaticMetricsAsync(CancellationToken cancellationToken = default);
    }
}