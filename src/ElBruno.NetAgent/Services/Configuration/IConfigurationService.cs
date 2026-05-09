namespace ElBruno.NetAgent.Services.Configuration;

/// <summary>
/// Service that manages application configuration lifecycle.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// The current configuration options.
    /// </summary>
    Core.Models.NetAgentOptions Options { get; }

    /// <summary>
    /// Path to the configuration file on disk.
    /// </summary>
    string ConfigFilePath { get; }

    /// <summary>
    /// Loads configuration from disk, creating a default file if it does not exist.
    /// </summary>
    Core.Models.NetAgentOptions LoadConfiguration();

    /// <summary>
    /// Saves the current options back to the configuration file.
    /// </summary>
    Task SaveConfigurationAsync(Core.Models.NetAgentOptions options);

    /// <summary>
    /// Validates the current configuration and returns any errors found.
    /// </summary>
    IReadOnlyList<string> Validate();
}
