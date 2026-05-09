using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ElBruno.NetAgent.Services.Tray;

/// <summary>
/// Hosted service that manages the tray icon lifecycle.
/// </summary>
public class TrayHostedService : IHostedService
{
    private readonly TrayIconService _trayIconService;
    private readonly ILogger<TrayHostedService> _logger;

    public TrayHostedService(
        TrayIconService trayIconService,
        ILogger<TrayHostedService> logger)
    {
        _trayIconService = trayIconService;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Tray hosted service started");
        _trayIconService.UpdateStatus("Starting");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Tray hosted service stopping");
        _trayIconService.UpdateStatus("Stopping");
        return Task.CompletedTask;
    }
}
