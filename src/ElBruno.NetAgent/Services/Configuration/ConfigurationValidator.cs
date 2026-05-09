namespace ElBruno.NetAgent.Services.Configuration;

/// <summary>
/// Validates NetAgentOptions for required sanity checks.
/// </summary>
public class ConfigurationValidator
{
    /// <summary>
    /// Validates the given options and returns a list of error messages.
    /// </summary>
    public IReadOnlyList<string> Validate(Core.Models.NetAgentOptions options)
    {
        var errors = new List<string>();

        if (options.CheckIntervalSeconds < 2)
        {
            errors.Add("CheckIntervalSeconds must be >= 2.");
        }

        if (options.PingTimeoutMs < 250)
        {
            errors.Add("PingTimeoutMs must be >= 250.");
        }

        if (options.LatencyThresholdMs < 50)
        {
            errors.Add("LatencyThresholdMs must be >= 50.");
        }

        if (options.PacketLossThresholdPercent < 0 || options.PacketLossThresholdPercent > 100)
        {
            errors.Add("PacketLossThresholdPercent must be between 0 and 100.");
        }

        if (options.FailbackCooldownSeconds < 30)
        {
            errors.Add("FailbackCooldownSeconds must be >= 30.");
        }

        if (options.PingEndpoints == null || options.PingEndpoints.Count == 0)
        {
            errors.Add("At least one ping endpoint must be configured.");
        }

        // Phase 10: Hard safety rules — these are errors, not warnings
        if (!options.DryRunMode)
        {
            errors.Add("DryRunMode must be true. Live network execution is intentionally blocked by default.");
        }

        if (options.AutoModeEnabled)
        {
            errors.Add("AutoModeEnabled must be false. Automatic switching requires explicit opt-in.");
        }

        if (options.LiveModeAllowed)
        {
            errors.Add("LiveModeAllowed must be false. Live network execution is intentionally blocked by default.");
        }

        return errors;
    }
}
