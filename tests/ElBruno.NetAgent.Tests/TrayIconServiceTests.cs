using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;
using ElBruno.NetAgent.Services;

namespace ElBruno.NetAgent.Tests
{
    [TestClass]
    public class TrayIconServiceTests
    {
        [TestMethod]
        public void CanConstructTrayIconService()
        {
            var logger = new LoggerFactory().CreateLogger<TrayIconService>();
            var svc = new TrayIconService(logger);
            Assert.IsNotNull(svc);
        }

        [TestMethod]
        public void HostRegistersTrayIconService()
        {
            using var host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHostedService<TrayIconService>();
                })
                .Build();

            var hosted = host.Services.GetServices<IHostedService>();
            Assert.IsTrue(hosted.Any(s => s.GetType() == typeof(TrayIconService)));
        }
    }
}
