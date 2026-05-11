using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ElBruno.NetAgent.Core.Configuration;
using ElBruno.NetAgent.Core.Services;
using ElBruno.NetAgent.Core.Decision;
using ElBruno.NetAgent.Core.Models;

namespace ElBruno.NetAgent.Services
{
    /// <summary>
    /// Background service that runs an automatic evaluation loop and requests network switches in a safe, dry-run-first manner.
    /// </summary>
    public class AutoModeHostedService : BackgroundService
    {
        private readonly ILogger<AutoModeHostedService> _logger;
        private readonly INetworkInventoryService _inventory;
        private readonly INetworkQualityMonitor _qualityMonitor;
        private readonly IDecisionEngine _decisionEngine;
        private readonly IServiceProvider _provider;
        private readonly IOptions<NetAgentOptions> _options;

        public AutoModeHostedService(
            ILogger<AutoModeHostedService> logger,
            INetworkInventoryService inventory,
            INetworkQualityMonitor qualityMonitor,
            IDecisionEngine decisionEngine,
            IServiceProvider provider,
            IOptions<NetAgentOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _qualityMonitor = qualityMonitor ?? throw new ArgumentNullException(nameof(qualityMonitor));
            _decisionEngine = decisionEngine ?? throw new ArgumentNullException(nameof(decisionEngine));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Public hook to run a single evaluation iteration. Useful for unit testing.
        /// </summary>
        public async Task EvaluateOnceAsync(CancellationToken cancellationToken = default)
        {
            var opts = _options.Value ?? new NetAgentOptions();
            var switching = opts.SwitchingRules;

            // Determine effective flags by combining top-level and switching rules (switching rules take precedence when set)
            var effectiveAutoModeEnabled = opts.AutoModeEnabled || (switching?.AutoModeEnabled ?? false);
            if (!effectiveAutoModeEnabled)
            {
                _logger.LogDebug("AutoModeHostedService: AutoModeEnabled=false - skipping evaluation.");
                return;
            }

            var effectiveDryRun = opts.DryRunMode || (switching?.DryRunMode ?? false);

            _logger.LogInformation("AutoModeHostedService: Auto mode evaluation starting.");

            var interfaces = await _inventory.GetInterfacesAsync(cancellationToken).ConfigureAwait(false) ?? Array.Empty<NetworkInterfaceInfo>();

            foreach (var adapter in interfaces)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var report = await _qualityMonitor.EvaluateAsync(adapter, cancellationToken).ConfigureAwait(false);
                    var decision = await _decisionEngine.EvaluateAsync(report, opts, cancellationToken).ConfigureAwait(false);

                    _logger.LogInformation("AutoMode decision for {Name} ({Id}): {Action} - {Reason}", adapter.Name, adapter.Id, decision.Action, decision.Reason);

                    if (decision.Action == Core.Decision.DecisionAction.SwitchAdapter)
                    {
                        if (effectiveDryRun)
                        {
                            _logger.LogInformation("AutoMode (dry-run): would request switch to interface {Id}", adapter.Id);
                        }
                        else
                        {
                            // Resolve controller if available
                            var controller = _provider.GetService<INetworkController>();
                            if (controller == null)
                            {
                                _logger.LogWarning("AutoMode: INetworkController not registered - cannot perform switch.");
                            }
                            else
                            {
                                try
                                {
                                    _logger.LogInformation("AutoMode: requesting switch to {Id}", adapter.Id);
                                    var result = await controller.PreferInterfaceAsync(adapter.Id, cancellationToken).ConfigureAwait(false);
                                    _logger.LogInformation("AutoMode: switch result: {Result}", result);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "AutoMode: exception while requesting switch to {Id}", adapter.Id);
                                }
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AutoMode: error evaluating adapter {Id}", adapter.Id);
                }
            }

            _logger.LogInformation("AutoModeHostedService: evaluation completed.");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var intervalSeconds = Math.Max(1, _options.Value?.SwitchingRules?.AutoModeIntervalSeconds ?? _options.Value?.AutoModeIntervalSeconds ?? NetAgentOptions.DefaultCheckIntervalSeconds);
            _logger.LogInformation("AutoModeHostedService: starting with interval {Interval}s (AutoModeEnabled={AutoModeEnabled}, DryRun={DryRun})", intervalSeconds, _options.Value?.AutoModeEnabled ?? _options.Value?.SwitchingRules?.AutoModeEnabled, _options.Value?.DryRunMode ?? _options.Value?.SwitchingRules?.DryRunMode);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await EvaluateOnceAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AutoModeHostedService: unexpected error during evaluation loop");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            _logger.LogInformation("AutoModeHostedService: stopping.");
        }
    }
}
