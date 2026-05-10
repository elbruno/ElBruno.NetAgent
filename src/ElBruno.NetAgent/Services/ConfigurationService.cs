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
            if (opts.LatencyThresholdMs < 0)
            {
                _logger.LogWarning("LatencyThresholdMs is negative ({Value}) - resetting to default", opts.LatencyThresholdMs);
                opts.LatencyThresholdMs = 150;
            }

            if (opts.PacketLossThresholdPercent < 0 || opts.PacketLossThresholdPercent > 100)
            {
                _logger.LogWarning("PacketLossThresholdPercent out of range ({Value}) - resetting to default", opts.PacketLossThresholdPercent);
                opts.PacketLossThresholdPercent = 5.0;
            }

            if (opts.CheckIntervalSeconds <= 0)
            {
                _logger.LogWarning("CheckIntervalSeconds must be > 0 ({Value}) - resetting to default", opts.CheckIntervalSeconds);
                opts.CheckIntervalSeconds = 30;
            }

            if (opts.MinimumChecksBeforeSwitch <= 0)
            {
                _logger.LogWarning("MinimumChecksBeforeSwitch must be > 0 ({Value}) - resetting to default", opts.MinimumChecksBeforeSwitch);
                opts.MinimumChecksBeforeSwitch = 3;
            }

            if (opts.MinimumScoreDeltaToSwitch < 0)
            {
                _logger.LogWarning("MinimumScoreDeltaToSwitch must be >= 0 ({Value}) - resetting to default", opts.MinimumScoreDeltaToSwitch);
                opts.MinimumScoreDeltaToSwitch = 10.0;
            }

            if (opts.TestEndpoints == null || opts.TestEndpoints.Length == 0)
            {
                _logger.LogWarning("TestEndpoints empty - using defaults");
                opts.TestEndpoints = new[] { "8.8.8.8", "1.1.1.1" };
            }
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
    }
}
