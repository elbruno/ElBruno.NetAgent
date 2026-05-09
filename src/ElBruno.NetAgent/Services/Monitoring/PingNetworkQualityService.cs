using ElBruno.NetAgent.Core.Enums;
using ElBruno.NetAgent.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.NetworkInformation;

namespace ElBruno.NetAgent.Services.Monitoring;

/// <summary>
/// Ping-based implementation of INetworkQualityService.
/// Measures connection quality by pinging configured endpoints.
/// </summary>
public class PingNetworkQualityService : INetworkQualityService
{
    private readonly ILogger<PingNetworkQualityService> _logger;
    private readonly NetAgentOptions _options;
    private readonly Dictionary<string, Queue<NetworkQualitySnapshot>> _rollingHistory = new();
    private const int MaxHistorySize = 10;

    public PingNetworkQualityService(
        ILogger<PingNetworkQualityService> logger,
        IOptions<NetAgentOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task<NetworkQualitySnapshot> MeasureAsync(
        NetworkInterfaceInfo networkInterface,
        CancellationToken cancellationToken = default)
    {
        var endpointResults = new List<EndpointPingResult>();
        var ping = new Ping();

        try
        {
            foreach (var endpoint in _options.PingEndpoints)
            {
                var result = await PingEndpointAsync(ping, endpoint, cancellationToken);
                endpointResults.Add(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error measuring quality for interface {InterfaceId}", networkInterface.Id);

            // Return a degraded snapshot on error
            var degraded = CreateDegradedSnapshot(networkInterface);
            UpdateRollingHistory(networkInterface.Id, degraded);
            return degraded;
        }
        finally
        {
            ping.Dispose();
        }

        var snapshot = BuildSnapshot(networkInterface, endpointResults);
        UpdateRollingHistory(networkInterface.Id, snapshot);

        _logger.LogInformation(
            "Quality snapshot for {InterfaceName}: score={Score} level={Level} avgLatency={AvgLatency}ms loss={Loss}%",
            snapshot.InterfaceName,
            snapshot.QualityScore,
            snapshot.QualityLevel,
            snapshot.AverageLatencyMs,
            snapshot.PacketLossPercent);

        return snapshot;
    }

    private async Task<EndpointPingResult> PingEndpointAsync(
        Ping ping,
        string endpoint,
        CancellationToken cancellationToken)
    {
        try
        {
            var addresses = await System.Net.Dns.GetHostAddressesAsync(endpoint);
            var ip = addresses.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

            if (ip == null)
            {
                _logger.LogDebug("Could not resolve IP for endpoint {Endpoint}", endpoint);

                return new EndpointPingResult
                {
                    Endpoint = endpoint,
                    Success = false,
                    LatencyMs = 0,
                    Timestamp = DateTime.UtcNow
                };
            }

            var response = await ping.SendPingAsync(ip, TimeSpan.FromMilliseconds(_options.PingTimeoutMs), new byte[32],
                new PingOptions { DontFragment = true }, cancellationToken);

            return new EndpointPingResult
            {
                Endpoint = endpoint,
                Success = response.Status == IPStatus.Success,
                LatencyMs = response.Status == IPStatus.Success ? response.RoundtripTime : 0,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Ping failed for endpoint {Endpoint}", endpoint);

            return new EndpointPingResult
            {
                Endpoint = endpoint,
                Success = false,
                LatencyMs = 0,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    private NetworkQualitySnapshot BuildSnapshot(
        NetworkInterfaceInfo networkInterface,
        List<EndpointPingResult> endpointResults)
    {
        var totalEndpoints = endpointResults.Count;
        var successfulPings = endpointResults.Count(r => r.Success);
        var failedPings = totalEndpoints - successfulPings;

        var successfulLatencies = endpointResults
            .Where(r => r.Success)
            .Select(r => (double)r.LatencyMs)
            .ToList();

        var avgLatency = successfulLatencies.Any() ? successfulLatencies.Average() : 0;
        var minLatency = successfulLatencies.Any() ? successfulLatencies.Min() : 0;
        var maxLatency = successfulLatencies.Any() ? successfulLatencies.Max() : 0;
        var packetLoss = totalEndpoints > 0 ? (double)failedPings / totalEndpoints * 100 : 100;

        var qualityScore = CalculateQualityScore(avgLatency, packetLoss);
        var qualityLevel = MapQualityLevel(qualityScore);

        return new NetworkQualitySnapshot
        {
            InterfaceId = networkInterface.Id,
            InterfaceName = networkInterface.Name,
            AverageLatencyMs = Math.Round(avgLatency, 2),
            MinLatencyMs = Math.Round(minLatency, 2),
            MaxLatencyMs = Math.Round(maxLatency, 2),
            PacketLossPercent = Math.Round(packetLoss, 2),
            QualityScore = Math.Round(qualityScore, 2),
            QualityLevel = qualityLevel,
            EndpointResults = endpointResults.AsReadOnly(),
            Timestamp = DateTime.UtcNow,
            RollingHistory = GetRollingHistory(networkInterface.Id).AsReadOnly()
        };
    }

    private NetworkQualitySnapshot CreateDegradedSnapshot(NetworkInterfaceInfo networkInterface)
    {
        return new NetworkQualitySnapshot
        {
            InterfaceId = networkInterface.Id,
            InterfaceName = networkInterface.Name,
            AverageLatencyMs = 0,
            MinLatencyMs = 0,
            MaxLatencyMs = 0,
            PacketLossPercent = 100,
            QualityScore = 0,
            QualityLevel = NetworkQualityLevel.Poor,
            EndpointResults = Array.Empty<EndpointPingResult>(),
            Timestamp = DateTime.UtcNow
        };
    }

    private void UpdateRollingHistory(string interfaceId, NetworkQualitySnapshot snapshot)
    {
        if (!_rollingHistory.TryGetValue(interfaceId, out var history))
        {
            history = new Queue<NetworkQualitySnapshot>(MaxHistorySize);
            _rollingHistory[interfaceId] = history;
        }

        history.Enqueue(snapshot);

        while (history.Count > MaxHistorySize)
        {
            history.Dequeue();
        }
    }

    private List<NetworkQualitySnapshot> GetRollingHistory(string interfaceId)
    {
        if (_rollingHistory.TryGetValue(interfaceId, out var history))
        {
            return history.ToList();
        }

        return new List<NetworkQualitySnapshot>();
    }

    /// <summary>
    /// Calculates quality score using the deterministic formula from ARCHITECTURE.md.
    /// score = 100 - latencyPenalty - packetLossPenalty
    /// </summary>
    private static double CalculateQualityScore(double avgLatencyMs, double packetLossPercent)
    {
        var score = 100.0;

        // Latency penalty
        score -= GetLatencyPenalty(avgLatencyMs);

        // Packet loss penalty
        score -= GetPacketLossPenalty(packetLossPercent);

        // Clamp to 0-100
        return Math.Max(0, Math.Min(100, score));
    }

    private static double GetLatencyPenalty(double latencyMs)
    {
        if (latencyMs <= 80) return 0;
        if (latencyMs <= 150) return 10;
        if (latencyMs <= 250) return 25;
        if (latencyMs <= 500) return 50;
        return 75;
    }

    private static double GetPacketLossPenalty(double packetLossPercent)
    {
        if (packetLossPercent <= 0) return 0;
        if (packetLossPercent <= 5) return 10;
        if (packetLossPercent <= 10) return 25;
        if (packetLossPercent <= 20) return 50;
        return 80;
    }

    private static NetworkQualityLevel MapQualityLevel(double score)
    {
        if (score >= 90) return NetworkQualityLevel.Excellent;
        if (score >= 70) return NetworkQualityLevel.Good;
        if (score >= 50) return NetworkQualityLevel.Fair;
        if (score > 0) return NetworkQualityLevel.Poor;
        return NetworkQualityLevel.Unknown;
    }
}
