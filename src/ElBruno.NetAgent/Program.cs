using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ElBruno.NetAgent
{
    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging => logging.AddConsole())
                .ConfigureServices((context, services) =>
                {
                    // Configuration service
                    services.AddSingleton<Core.Configuration.IConfigurationService, Services.ConfigurationService>();

                    // Provide IOptions<NetAgentOptions> synchronously at startup by reading the config.
                    services.AddSingleton(provider =>
                        Microsoft.Extensions.Options.Options.Create(
                            provider.GetRequiredService<Core.Configuration.IConfigurationService>()
                                .GetOptionsAsync(System.Threading.CancellationToken.None).GetAwaiter().GetResult()
                        )
                    );

                    // Network inventory
                    services.AddSingleton<ElBruno.NetAgent.Core.Services.INetworkInventoryService, ElBruno.NetAgent.Services.Network.NetworkInventoryService>();

                    services.AddHostedService<Services.TrayIconService>();
                })
                .Build();

            var app = new System.Windows.Application();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            app.Exit += (s, e) =>
            {
                var logger = host.Services.GetService<ILoggerFactory>()?.CreateLogger("Program");
                logger?.LogInformation("Application exiting - stopping host.");
                try
                {
                    host.StopAsync().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error stopping host during Exit.");
                }
            };

            var loggerStart = host.Services.GetService<ILoggerFactory>()?.CreateLogger("Program");
            loggerStart?.LogInformation("Starting host.");
            host.StartAsync().GetAwaiter().GetResult();

            loggerStart?.LogInformation("Application started - running in tray.");
            try
            {
                app.Run();
            }
            finally
            {
                loggerStart?.LogInformation("Application Run returned - stopping host.");
                host.StopAsync().GetAwaiter().GetResult();
                host.Dispose();
            }

            return 0;
        }
    }
}
