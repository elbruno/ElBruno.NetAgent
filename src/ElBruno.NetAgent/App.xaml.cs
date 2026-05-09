using ElBruno.NetAgent.Core.Models;
using ElBruno.NetAgent.Services.Tray;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Configuration;
using System.Data;
using System.Windows;

namespace ElBruno.NetAgent;

/// <summary>
/// Application entry point. Uses Microsoft.Extensions.Hosting to manage the WPF lifecycle.
/// </summary>
public partial class App : Application
{
    private IHost? _host;
    private static readonly ILogger<App> _logger = LoggerFactory
        .Create(builder => builder.AddConsole())
        .CreateLogger<App>();

    /// <summary>
    /// The hosted instance used for DI and lifecycle management.
    /// </summary>
    public IHost? Host => _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                // Config will be loaded from %LOCALAPPDATA%\ElBruno.NetAgent\config.json
                // at runtime by ConfigurationService
            })
            .ConfigureLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices((context, services) =>
            {
                // Register configuration
                services.Configure<NetAgentOptions>(
                    context.Configuration.GetSection("NetAgent"));

                // Register services
                services.AddSingleton<Services.Configuration.IConfigurationService, Services.Configuration.ConfigurationService>();
                services.AddSingleton<Services.Network.INetworkInventoryService, Services.Network.NetworkInventoryService>();
                services.AddSingleton<Services.Monitoring.INetworkQualityService, Services.Monitoring.PingNetworkQualityService>();
                services.AddSingleton<Services.Decision.INetworkDecisionEngine, Services.Decision.NetworkDecisionEngine>();
                services.AddSingleton<Services.Control.INetworkController, Services.Control.WindowsNetworkController>(
                    sp => new Services.Control.WindowsNetworkController(
                        sp.GetRequiredService<ILogger<Services.Control.WindowsNetworkController>>(),
                        new Infrastructure.Windows.WindowsAdminService(),
                        sp.GetRequiredService<Services.Audit.IAuditLogService>(),
                        Core.Enums.NetworkSwitchMode.DryRun));
                services.AddSingleton<Infrastructure.Windows.IWindowsAdminService, Infrastructure.Windows.WindowsAdminService>();
                services.AddSingleton<Services.Audit.IAuditLogService, Services.Audit.InMemoryAuditLogService>();
                services.AddSingleton<TrayIconService>(sp => new TrayIconService(
                    sp.GetRequiredService<ILogger<TrayIconService>>(),
                    sp.GetRequiredService<Services.Audit.IAuditLogService>()));

                // Register hosted service for tray lifecycle
                services.AddHostedService<TrayHostedService>();
            })
            .Build();

        await _host.StartAsync();

        // Hide main window by default (tray-first app)
        MainWindow = new MainWindow
        {
            WindowState = WindowState.Minimized,
            ShowInTaskbar = false
        };

        _logger.LogInformation("App started (tray mode)");
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _logger.LogInformation("App exiting");

        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}

