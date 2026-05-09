using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ElBruno.NetAgent.Core.Models;

namespace ElBruno.NetAgent.Core.Services
{
    /// <summary>
    /// Service that enumerates and classifies network interfaces.
    /// </summary>
    public interface INetworkInventoryService
    {
        /// <summary>
        /// Returns discovered network interfaces.
        /// </summary>
        Task<IReadOnlyList<NetworkInterfaceInfo>> GetInterfacesAsync(CancellationToken cancellationToken);
    }
}
