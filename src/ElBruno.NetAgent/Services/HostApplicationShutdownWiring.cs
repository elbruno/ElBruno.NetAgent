using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ElBruno.NetAgent.Services
{
    // HostedService that wires IHostApplicationLifetime.ApplicationStopping to WPF Application shutdown on UI thread.
    public class HostApplicationShutdownWiring : IHostedService
    {
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ILogger<HostApplicationShutdownWiring> _logger;

        public HostApplicationShutdownWiring(IHostApplicationLifetime lifetime, ILogger<HostApplicationShutdownWiring> logger)
        {
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _lifetime.ApplicationStopping.Register(() =>
                {
                    try
                    {
                        _logger.LogInformation("Host is stopping - invoking WPF Application.Shutdown on UI thread.");
                        if (System.Windows.Application.Current?.Dispatcher != null)
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                try
                                {
                                    // Close any open windows cleanly before shutting down.
                                    try
                                    {
                                        foreach (var w in System.Windows.Application.Current.Windows)
                                        {
                                            try { (w as System.Windows.Window)?.Close(); } catch { }
                                        }
                                    }
                                    catch { }

                                    try { System.Windows.Application.Current.Shutdown(); } catch (Exception ex) { _logger.LogWarning(ex, "Application.Current.Shutdown threw"); }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Error during UI shutdown handling");
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed invoking dispatcher for UI shutdown");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed wiring host shutdown to WPF shutdown");
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
