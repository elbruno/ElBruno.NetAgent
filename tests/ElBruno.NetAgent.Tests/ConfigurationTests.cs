using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using ElBruno.NetAgent.Services;
using ElBruno.NetAgent.Core.Configuration;

namespace ElBruno.NetAgent.Tests
{
    public class TestLogger<T> : ILogger<T>
    {
        public readonly System.Collections.Generic.List<string> Warnings = new();
        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                Warnings.Add(formatter(state, exception));
            }
        }
        private class NullScope : IDisposable { public static readonly NullScope Instance = new(); public void Dispose() {} }
    }

    public class ConfigurationTests : IDisposable
    {
        private readonly string _tempDir;
        public ConfigurationTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }
        public void Dispose()
        {
            try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true); } catch {}
        }

        [Fact]
        public async Task MissingConfig_CreatesDefaultAndReturnsDefaults()
        {
            var logger = new TestLogger<ConfigurationService>();
            var svc = new ConfigurationService(logger, _tempDir);
            var opts = await svc.GetOptionsAsync();
            Assert.Equal(150, opts.LatencyThresholdMs);
            var configPath = Path.Combine(_tempDir, "config.json");
            Assert.True(File.Exists(configPath));
            var text = await File.ReadAllTextAsync(configPath);
            var doc = JsonDocument.Parse(text);
            Assert.True(doc.RootElement.TryGetProperty("LatencyThresholdMs", out var _));
        }

        [Fact]
        public async Task InvalidValues_LogsWarningAndFallsBack()
        {
            var configPath = Path.Combine(_tempDir, "config.json");
            await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(new { LatencyThresholdMs = -10 }));
            var logger = new TestLogger<ConfigurationService>();
            var svc = new ConfigurationService(logger, _tempDir);
            var opts = await svc.GetOptionsAsync();
            Assert.Equal(150, opts.LatencyThresholdMs);
            Assert.NotEmpty(logger.Warnings);
        }

        [Fact]
        public async Task MissingFields_UseDefaultValues()
        {
            var configPath = Path.Combine(_tempDir, "config.json");
            await File.WriteAllTextAsync(configPath, "{}");
            var logger = new TestLogger<ConfigurationService>();
            var svc = new ConfigurationService(logger, _tempDir);
            var opts = await svc.GetOptionsAsync();
            Assert.Equal(150, opts.LatencyThresholdMs);
            Assert.False(opts.AutoModeEnabled);
        }
    }
}
