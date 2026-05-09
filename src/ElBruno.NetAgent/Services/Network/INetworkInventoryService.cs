namespace ElBruno.NetAgent.Services.Network;

/// <summary>
/// Service responsible for enumerating and classifying network interfaces.
/// </summary>
public interface INetworkInventoryService
{
    /// <summary>
    /// Enumerates all available network interfaces and returns classified results.
    /// </summary>
    Task<IReadOnlyList<Core.Models.NetworkInterfaceInfo>> GetInterfacesAsync(CancellationToken cancellationToken = default);
}
