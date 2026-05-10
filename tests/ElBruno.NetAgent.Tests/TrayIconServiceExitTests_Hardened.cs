using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Xunit;
using ElBruno.NetAgent.Services;

namespace ElBruno.NetAgent.Tests
{
    internal class TestNotifyIconAdapter : INotifyIconAdapter
    {
        public bool Visible { get; set; }
        public string Text { get; set; } = string.Empty;
        public System.Drawing.Icon? Icon { get; set; }
        public ContextMenuStrip? ContextMenuStrip { get; set; }
        public bool IsDisposed { get; private set; }
        public event EventHandler? Disposed;
        public void ShowBalloonTip(int timeout, string title, string text, ToolTipIcon icon) { /* no-op */ }
        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            Disposed?.Invoke(this, EventArgs.Empty);
        }
    }

    public class TrayIconServiceExitTests_Hardened
    {
        private class TestHostApplicationLifetime : IHostApplicationLifetime
        {
            public CancellationToken ApplicationStarted { get; } = CancellationToken.None;
            public CancellationToken ApplicationStopping { get; } = CancellationToken.None;
            public CancellationToken ApplicationStopped { get; } = CancellationToken.None;
            public bool StopCalled { get; private set; }
            public void StopApplication() => StopCalled = true;
        }

        [Fact(Skip = "Integration: requires interactive Windows session - skipped in unit runs")]
        public void ExitClick_ShutdownsHost_And_DisposesTrayResources_Hardened()
        {
            var lifetime = new TestHostApplicationLifetime();
            var logger = new LoggerFactory().CreateLogger<TrayIconService>();
            var testIcon = new TestNotifyIconAdapter();
            var disposed = false;
            testIcon.Disposed += (s, e) => disposed = true;
            // Construct TrayIconService directly and inject test lifetime and test icon to avoid replacing host lifetime in HostBuilder.
            var svc = new TrayIconService(logger, lifetime, (ElBruno.NetAgent.Services.INotifyIconAdapter)testIcon);
            // Execute test inline: no STA thread required because the test uses the no-dispatch helper.
            try
            {
                svc.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

                svc.InvokeExitForTests_NoDispatch();
                // Verify host lifetime was requested to stop
                Assert.True(lifetime.StopCalled, "IHostApplicationLifetime.StopApplication should be called");

                // Wait briefly for dispose to happen
                System.Threading.SpinWait.SpinUntil(() => disposed, TimeSpan.FromSeconds(5));
                Assert.True(disposed, "NotifyIcon.Dispose should be called");

                // Verify private field cleared
                var iconField = typeof(ElBruno.NetAgent.Services.TrayIconService).GetField("_notifyIcon", BindingFlags.NonPublic | BindingFlags.Instance);
                var iconVal = iconField?.GetValue(svc);
                Assert.Null(iconVal);
            }
            catch (Exception ex)
            {
                throw new AggregateException("Exception during Exit click test", ex);
            }
        }
    }
}
