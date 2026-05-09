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
                    services.AddHostedService<Services.TrayIconService>();
                })
                .Build();

            var app = new Application();
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
