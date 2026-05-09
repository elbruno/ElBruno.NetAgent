using System.IO;
using Microsoft.Extensions.Logging;

namespace ElBruno.NetAgent.Services.Configuration;

/// <summary>
/// Default implementation of IConfigurationService.
/// Loads configuration from %LOCALAPPDATA%\ElBruno.NetAgent\config.json.
/// Creates a default config file if it does not exist.
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly ConfigurationValidator _validator;
    private Core.Models.NetAgentOptions? _options;

    public string ConfigFilePath { get; }

    public Core.Models.NetAgentOptions Options => _options;

    public ConfigurationService(ILogger<ConfigurationService> logger, string? configDirectoryOverride = null)
    {
        _logger = logger;
        _validator = new ConfigurationValidator();
        var configDir = configDirectoryOverride ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        ConfigFilePath = Path.Combine(
            configDir,
            "ElBruno.NetAgent",
            "config.json");
    }

    public Core.Models.NetAgentOptions LoadConfiguration()
    {
        var configDir = Path.GetDirectoryName(ConfigFilePath)!;

        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
            _logger.LogInformation("Created configuration directory: {ConfigDir}", configDir);
        }

        if (!File.Exists(ConfigFilePath))
        {
            _options = new Core.Models.NetAgentOptions();
            _logger.LogInformation("Config file not found at {ConfigPath}. Using defaults.", ConfigFilePath);
            SaveConfigurationAsync(_options).GetAwaiter().GetResult();
            _logger.LogInformation("Default configuration written to {ConfigPath}", ConfigFilePath);
        }
        else
        {
            var json = File.ReadAllText(ConfigFilePath);
            _options = System.Text.Json.JsonSerializer.Deserialize<Core.Models.NetAgentOptions>(json)
                ?? new Core.Models.NetAgentOptions();
            _logger.LogInformation("Configuration loaded from {ConfigPath}", ConfigFilePath);
        }

        var errors = _validator.Validate(_options!);
        if (errors.Count > 0)
        {
            _logger.LogWarning("Configuration validation warnings: {Errors}", string.Join("; ", errors));
        }

        return _options!;
    }

    public async Task SaveConfigurationAsync(Core.Models.NetAgentOptions options)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(options, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        });

        await File.WriteAllTextAsync(ConfigFilePath, json);
        _logger.LogInformation("Configuration saved to {ConfigPath}", ConfigFilePath);
    }

    public IReadOnlyList<string> Validate()
    {
        return _validator.Validate(_options);
    }
}
