using System;
using System.Threading;
using System.Threading.Tasks;
using ElBruno.NetAgent.Core.Configuration;

namespace ElBruno.NetAgent.Services
{
    // Lightweight in-memory IConfigurationService for smoke tests. Does not touch disk.
    public class InMemoryConfigurationService : ElBruno.NetAgent.Core.Configuration.IConfigurationService
    {
        private NetAgentOptions _options = new NetAgentOptions();
        private readonly TaskCompletionSource<bool> _savedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<NetAgentOptions> GetOptionsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_options);
        }

        public Task<NetAgentOptions> ReloadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_options);
        }

        public Task SaveOptionsAsync(NetAgentOptions options, CancellationToken cancellationToken = default)
        {
            _options = options ?? new NetAgentOptions();
            _savedTcs.TrySetResult(true);
            return Task.CompletedTask;
        }

        public string GetConfigFolderPath() => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        public string GetConfigFilePath() => System.IO.Path.Combine(GetConfigFolderPath(), "config.json");
        public void OpenConfigFolder() { }
        public void OpenConfigFile() { }

        public Task<bool> AwaitSavedAsync(TimeSpan timeout)
        {
            return Task.WhenAny(_savedTcs.Task, Task.Delay(timeout)).ContinueWith(t => _savedTcs.Task.IsCompleted);
        }
    }
}
