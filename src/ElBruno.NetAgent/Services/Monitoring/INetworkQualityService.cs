using ElBruno.NetAgent.Core.Models;

namespace ElBruno.NetAgent.Services.Monitoring;

/// <summary>
/// Service responsible for measuring network connection quality.
/// </summary>
public interface INetworkQualityService
{
    /// <summary>
    /// Measures the quality of the given network interface by pinging configured endpoints.
    /// </summary>
    Task<NetworkQualitySnapshot> MeasureAsync(
        Core.Models.NetworkInterfaceInfo networkInterface,
        CancellationToken cancellationToken = default);
}
