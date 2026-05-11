using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ElBruno.NetAgent.Core.Configuration;

namespace ElBruno.NetAgent.Services
{
    public class ConfigurationService : ElBruno.NetAgent.Core.Configuration.IConfigurationService
    {
        private readonly ILogger<ConfigurationService> _logger;
        private readonly string _folderPath;
        private readonly string _filePath;
        private ElBruno.NetAgent.Core.Configuration.NetAgentOptions? _cachedOptions;

        public ConfigurationService(ILogger<ConfigurationService> logger, string? overrideFolder = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _folderPath = overrideFolder ?? GetConfigFolderPath();
            _filePath = GetConfigFilePath();
        }

        public string GetConfigFolderPath()
        {
            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(basePath, "ElBruno.NetAgent");
        }

        public string GetConfigFilePath()
        {
            return Path.Combine(_folderPath, "config.json");
        }

        public async Task<ElBruno.NetAgent.Core.Configuration.NetAgentOptions> GetOptionsAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            if (_cachedOptions != null)
            {
                return _cachedOptions;
            }

            if (!Directory.Exists(_folderPath))
            {
                try
                {
                    Directory.CreateDirectory(_folderPath);
                    _logger.LogInformation("Created config folder at {Path}", _folderPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create config folder {Path}", _folderPath);
                }
            }

            if (!File.Exists(_filePath))
            {
                var defaults = new NetAgentOptions();
                try
                {
                    var json = JsonSerializer.Serialize(defaults, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(_filePath, json, cancellationToken).ConfigureAwait(false);
                    _logger.LogInformation("Created default config at {Path}", _filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to write default config to {Path}", _filePath);
                }

                _cachedOptions = defaults;
                return defaults;
            }

            try
            {
                var json = await File.ReadAllTextAsync(_filePath, cancellationToken).ConfigureAwait(false);
                var opts = JsonSerializer.Deserialize<NetAgentOptions>(json) ?? new NetAgentOptions();
                ValidateOptions(opts);
                _cachedOptions = opts;
                _logger.LogInformation("Loaded config from {Path}", _filePath);
                return opts;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read/parse config at {Path} - using defaults", _filePath);
                _cachedOptions = new NetAgentOptions();
                return _cachedOptions;
            }
        }

        public async Task<ElBruno.NetAgent.Core.Configuration.NetAgentOptions> ReloadAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            _cachedOptions = null;
            return await GetOptionsAsync(cancellationToken).ConfigureAwait(false);
        }

        private void ValidateOptions(NetAgentOptions opts)
        {
            // Top-level numeric validation
            if (opts.LatencyThresholdMs < 0)
            {
                _logger.LogWarning("LatencyThresholdMs is negative ({Value}) - resetting to default", opts.LatencyThresholdMs);
                opts.LatencyThresholdMs = NetAgentOptions.DefaultLatencyThresholdMs;
            }

            if (opts.PacketLossThresholdPercent < 0 || opts.PacketLossThresholdPercent > 100)
            {
                _logger.LogWarning("PacketLossThresholdPercent out of range ({Value}) - resetting to default", opts.PacketLossThresholdPercent);
                opts.PacketLossThresholdPercent = NetAgentOptions.DefaultPacketLossThresholdPercent;
            }

            if (opts.CheckIntervalSeconds < 1)
            {
                _logger.LogWarning("CheckIntervalSeconds must be >= 1 ({Value}) - resetting to default", opts.CheckIntervalSeconds);
                opts.CheckIntervalSeconds = NetAgentOptions.DefaultCheckIntervalSeconds;
            }

            if (opts.AutoModeIntervalSeconds < 1)
            {
                _logger.LogWarning("AutoModeIntervalSeconds must be >= 1 ({Value}) - resetting to default", opts.AutoModeIntervalSeconds);
                opts.AutoModeIntervalSeconds = NetAgentOptions.DefaultCheckIntervalSeconds;
            }

            if (opts.MinimumChecksBeforeSwitch < 1)
            {
                _logger.LogWarning("MinimumChecksBeforeSwitch must be >= 1 ({Value}) - resetting to default", opts.MinimumChecksBeforeSwitch);
                opts.MinimumChecksBeforeSwitch = NetAgentOptions.DefaultMinimumChecksBeforeSwitch;
            }

            if (opts.MinimumScoreDeltaToSwitch < 0)
            {
                _logger.LogWarning("MinimumScoreDeltaToSwitch must be >= 0 ({Value}) - resetting to default", opts.MinimumScoreDeltaToSwitch);
                opts.MinimumScoreDeltaToSwitch = NetAgentOptions.DefaultMinimumScoreDeltaToSwitch;
            }

            if (opts.TestEndpoints == null || opts.TestEndpoints.Length == 0)
            {
                _logger.LogWarning("TestEndpoints empty - using defaults");
                opts.TestEndpoints = new[] { "8.8.8.8", "1.1.1.1" };
            }

            // Switching rules validation and normalization
            if (opts.SwitchingRules == null)
            {
                opts.SwitchingRules = new NetAgentOptions.SwitchingRulesOptions();
            }

            var s = opts.SwitchingRules;

            if (s.AutoModeIntervalSeconds < 1)
            {
                _logger.LogWarning("SwitchingRules.AutoModeIntervalSeconds must be >= 1 ({Value}) - resetting to default", s.AutoModeIntervalSeconds);
                s.AutoModeIntervalSeconds = NetAgentOptions.DefaultCheckIntervalSeconds;
            }

            if (s.PauseAutoSwitchDurationSeconds < 0)
            {
                _logger.LogWarning("PauseAutoSwitchDurationSeconds must be >= 0 ({Value}) - resetting to 0", s.PauseAutoSwitchDurationSeconds);
                s.PauseAutoSwitchDurationSeconds = 0;
            }

            if (s.PauseAutoSwitchUntilSeconds < 0)
            {
                _logger.LogWarning("PauseAutoSwitchUntilSeconds must be >= 0 ({Value}) - resetting to 0", s.PauseAutoSwitchUntilSeconds);
                s.PauseAutoSwitchUntilSeconds = 0;
            }

            if (s.MinimumQualityScore < 0)
            {
                _logger.LogWarning("MinimumQualityScore must be >= 0 ({Value}) - resetting to 0", s.MinimumQualityScore);
                s.MinimumQualityScore = 0.0;
            }

            if (s.MinimumScoreImprovement < 0)
            {
                _logger.LogWarning("MinimumScoreImprovement must be >= 0 ({Value}) - resetting to 0", s.MinimumScoreImprovement);
                s.MinimumScoreImprovement = 0.0;
            }

            if (s.PreferredInterfacePatterns == null)
            {
                s.PreferredInterfacePatterns = new string[0];
            }

            if (s.ExcludedInterfacePatterns == null)
            {
                s.ExcludedInterfacePatterns = new string[0];
            }

            if (s.ExcludedInterfaceKinds == null)
            {
                s.ExcludedInterfaceKinds = new string[] { "Loopback", "Vpn" };
            }

            // Synchronize explicit ignore flags with ExcludedInterfaceKinds
            try
            {
                var kinds = new System.Collections.Generic.List<string>(s.ExcludedInterfaceKinds ?? System.Array.Empty<string>());

                // If UI flags were set, respect them; otherwise derive flags from kinds list
                if (!s.IgnoreLoopbackAdapters && !kinds.Contains("Loopback"))
                {
                    // nothing - leave as-is
                }
                else if (s.IgnoreLoopbackAdapters && !kinds.Contains("Loopback"))
                {
                    kinds.Add("Loopback");
                }

                if (s.IgnoreVpnAdapters && !kinds.Contains("Vpn"))
                {
                    kinds.Add("Vpn");
                }

                // Ensure flags reflect list contents
                s.IgnoreLoopbackAdapters = kinds.Contains("Loopback");
                s.IgnoreVpnAdapters = kinds.Contains("Vpn");

                s.ExcludedInterfaceKinds = kinds.Distinct(System.StringComparer.OrdinalIgnoreCase).ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error normalizing ExcludedInterfaceKinds - clearing to defaults");
                s.ExcludedInterfaceKinds = new string[] { "Loopback", "Vpn" };
                s.IgnoreLoopbackAdapters = true;
                s.IgnoreVpnAdapters = true;
            }

            // Ensure boolean defaults for other flags
            // Preserve defaults from class; explicitly ensure values are set
            // (these assignments are idempotent but make the intent clear)
            s.DryRunMode = s.DryRunMode;
            s.AutoModeEnabled = s.AutoModeEnabled;
            s.PreferUsbTethering = s.PreferUsbTethering;
            s.AllowWiFi = s.AllowWiFi;
            s.AllowEthernet = s.AllowEthernet;
            s.IgnoreVirtualAdapters = s.IgnoreVirtualAdapters;
            s.IgnoreDownOrUnknownAdapters = s.IgnoreDownOrUnknownAdapters;
        }

        public void OpenConfigFolder()
        {
            var folder = GetConfigFolderPath();
            if (!Directory.Exists(folder))
            {
                _logger.LogInformation("Config folder does not exist, creating: {Folder}", folder);
                try { Directory.CreateDirectory(folder); } catch { }
            }

            if (_cachedOptions?.DryRunMode == true)
            {
                _logger.LogInformation("DryRunMode enabled - OpenConfigFolder skipped: {Folder}", folder);
                return;
            }

            try
            {
                Process.Start("explorer.exe", folder);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to open config folder {Folder}", folder);
            }
        }

        public void OpenConfigFile()
        {
            var file = GetConfigFilePath();
            if (_cachedOptions?.DryRunMode == true)
            {
                _logger.LogInformation("DryRunMode enabled - OpenConfigFile skipped: {File}", file);
                return;
            }

            try
            {
                // Open the folder and select the file
                var args = $"/select,\"{file}\"";
                Process.Start("explorer.exe", args);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to open config file {File}", file);
            }
        }

        public async Task SaveOptionsAsync(NetAgentOptions options, CancellationToken cancellationToken = default)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            try
            {
                // Debug: log incoming values before validation
                Debug.WriteLine($"ConfigurationService.SaveOptionsAsync: before Validate - AutoModeIntervalSeconds={options.AutoModeIntervalSeconds}; SwitchingRules.AutoModeIntervalSeconds={options.SwitchingRules?.AutoModeIntervalSeconds}");

                // Validate and normalize before persisting so the on-disk representation is complete
                ValidateOptions(options);

                // Debug: log values after validation
                Debug.WriteLine($"ConfigurationService.SaveOptionsAsync: after Validate - AutoModeIntervalSeconds={options.AutoModeIntervalSeconds}; SwitchingRules.AutoModeIntervalSeconds={options.SwitchingRules?.AutoModeIntervalSeconds}");

                if (!Directory.Exists(_folderPath))
                {
                    Directory.CreateDirectory(_folderPath);
                }

                var json = JsonSerializer.Serialize(options, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_filePath, json, cancellationToken).ConfigureAwait(false);

                // Update cached instance so subsequent reads reflect saved state
                _cachedOptions = options;
                _logger.LogInformation("Saved config to {Path}", _filePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save config to {Path}", _filePath);
            }
        }
    }
}
